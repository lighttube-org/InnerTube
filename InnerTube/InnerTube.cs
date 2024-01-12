using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Google.Protobuf;
using InnerTube.Exceptions;
using InnerTube.Protobuf.Requests;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace InnerTube;

/// <summary>
/// The InnerTube client.
/// </summary>
public class InnerTube
{
	internal readonly HttpClient HttpClient = new();
	internal readonly MemoryCache PlayerCache;
	internal readonly string ApiKey;
	internal readonly InnerTubeAuthorization? Authorization;

	/// <summary>
	/// Initializes a new instance of InnerTube client.
	/// </summary>
	/// <param name="config">Configuration parameters</param>
	public InnerTube(InnerTubeConfiguration? config = null)
	{
		config ??= new InnerTubeConfiguration();
		ApiKey = config.ApiKey;
		Authorization = config.Authorization;
		PlayerCache = new MemoryCache(new MemoryCacheOptions
		{
			ExpirationScanFrequency = config.CacheExpirationPollingInterval,
			SizeLimit = config.CacheSize
		});
	}

	private async Task<byte[]> MakeRequest(RequestClient client, string endpoint, InnerTubeRequest postData,
		string language, string region, bool authorized = false)
	{
		string url = $"https://youtubei.googleapis.com/youtubei/v1/{endpoint}";
		url += "?alt=proto";
		if (!authorized || Authorization?.Type != AuthorizationType.REFRESH_TOKEN)
			url += $"&key={ApiKey}";


		HttpRequestMessage hrm = new(HttpMethod.Post, url);

		byte[] buffer = Encoding.UTF8.GetBytes(postData.GetJson(client, language, region));
		ByteArrayContent byteContent = new(buffer);
		byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		hrm.Content = byteContent;

		if (authorized && Authorization is not null)
		{
			if (Authorization.Type == AuthorizationType.SAPISID)
				hrm.Headers.Add("Cookie", Authorization.GenerateCookieHeader());
			hrm.Headers.Add("Authorization", Authorization.GenerateAuthHeader());
		}

		hrm.Headers.Add("X-Youtube-Client-Name", ((int)client).ToString());
		hrm.Headers.Add("X-Youtube-Client-Version", client switch
		{
			RequestClient.WEB => "2.20220809.02.00",
			RequestClient.ANDROID => "19.09.4",
			RequestClient.IOS => "19.09.4",
			var _ => ""
		});
		//hrm.Headers.Add("Origin", "https://www.youtube.com");
		if (client == RequestClient.ANDROID)
			hrm.Headers.Add("User-Agent", "com.google.android.youtube/19.09.4 (Linux; U; Android 11) gzip");

		HttpResponseMessage ytPlayerRequest = await HttpClient.SendAsync(hrm);
		if (!ytPlayerRequest.IsSuccessStatusCode)
			throw new RequestException(ytPlayerRequest.StatusCode, await ytPlayerRequest.Content.ReadAsStringAsync());
		return await ytPlayerRequest.Content.ReadAsByteArrayAsync();
	}

	/// <summary>
	/// Gets the player data of a video
	/// </summary>
	/// <param name="videoId">ID of the video</param>
	/// <param name="contentCheckOk">Set to true if you want to skip the content warnings (suicide, self-harm etc.)</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	public async Task<PlayerResponse> GetPlayerAsync(string videoId, bool contentCheckOk = false,
		string language = "en", string region = "US")
	{
		/*
		string cacheId = $"{videoId}_{(includeHls ? "hls" : "dash")}({language}_{region})";

		if (PlayerCache.TryGetValue(cacheId, out InnerTubePlayer cachedPlayer)) return cachedPlayer;

		Task[] tasks =
		{
			GetPlayerObjectAsync(videoId, contentCheckOk, language, region, RequestClient.WEB),
			GetPlayerObjectAsync(videoId, contentCheckOk, language, region,
				includeHls ? RequestClient.IOS : RequestClient.ANDROID)
		};

		Task.WaitAll(tasks);

		JObject[] responses = tasks.Select(x => ((Task<JObject>)x).Result).ToArray();

		string playabilityStatus = responses[1].GetFromJsonPath<string>("playabilityStatus.status")!;
		if (playabilityStatus != "OK")
			throw new PlayerException(playabilityStatus,
				responses[1].GetFromJsonPath<string>("playabilityStatus.reason")!,
				responses[1].GetFromJsonPath<string>("playabilityStatus.reasonTitle") ??
				Utils.ReadText(
					responses[1].GetFromJsonPath<JObject>(
						"playabilityStatus.errorScreen.playerErrorMessageRenderer.subreason")));

		InnerTubePlaPlayerResyer player = new(responses[1], responses[0]);
		PlayerCache.Set(cacheId, player, new MemoryCacheEntryOptions
		{
			Size = 1,
			SlidingExpiration = TimeSpan.FromSeconds(Math.Max(600, player.Details.Length.TotalSeconds)),
			AbsoluteExpirationRelativeToNow =
				TimeSpan.FromSeconds(Math.Max(3600, player.ExpiresInSeconds - player.Details.Length.TotalSeconds))
		});
		return player;
		*/
		Task<PlayerResponse>[] tasks =
		{
			GetPlayerObjectAsync(videoId, contentCheckOk, language, region, RequestClient.WEB),
			GetPlayerObjectAsync(videoId, contentCheckOk, language, region, RequestClient.ANDROID),
			GetPlayerObjectAsync(videoId, contentCheckOk, language, region, RequestClient.IOS)
		};
		Task.WaitAll(tasks.Cast<Task>().ToArray());
		PlayerResponse[] players = tasks.Select(x => x.Result).ToArray();

		players[0].StreamingData = players[1].StreamingData;
		if (!players[0].StreamingData.HasHlsManifestUrl && players[2].StreamingData.HasHlsManifestUrl)
			players[0].StreamingData.HlsManifestUrl = players[2].StreamingData.HlsManifestUrl;
		
		return players[0];
	}

	// instead of trying hours to find a protobuf compilation to
	// have endscreen, cards, storyboards and non 403'ing video
	// data at the same time i decided to just do the request
	// twice, one WEB and one ANDROID. if someone finds a protobuf
	// string that returns all those on the android client pls pr <3
	private async Task<PlayerResponse> GetPlayerObjectAsync(string videoId, bool contentCheckOk, string language,
		string region, RequestClient client)
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId)
			.AddValue("contentCheckOk", contentCheckOk)
			.AddValue("racyCheckOk", contentCheckOk);

		if (client == RequestClient.ANDROID)
			postData.AddValue("params", "CgIQBg");

		return PlayerResponse.Parser.ParseFrom(await MakeRequest(client, "player", postData,
			language, region, true));
	}
}