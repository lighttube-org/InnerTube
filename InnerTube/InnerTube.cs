using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using InnerTube.Exceptions;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTube
{
	internal readonly HttpClient HttpClient = new();
	internal readonly MemoryCache PlayerCache = new("innertube.players");
	internal readonly InnerTubeAuthorization? Authorization;

	public InnerTube(InnerTubeAuthorization authorization = null)
	{
		Authorization = authorization;
	}

	private async Task<JObject> MakeRequest(RequestClient client, string endpoint, InnerTubeRequest postData,
		string language, string region, bool authorized = false)
	{
		HttpRequestMessage hrm = new(HttpMethod.Post,
			@$"https://www.youtube.com/youtubei/v1/{endpoint}?prettyPrint=false{(authorized && Authorization?.Type == AuthorizationType.REFRESH_TOKEN ? "" : "&key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8")}");

		byte[] buffer = Encoding.UTF8.GetBytes(postData.GetJson(client, language, region));
		ByteArrayContent byteContent = new(buffer);
		byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		hrm.Content = byteContent;

		if (authorized && Authorization is not null)
		{
			hrm.Headers.Add("Cookie", Authorization.GenerateCookieHeader());
			hrm.Headers.Add("Authorization", Authorization.GenerateAuthHeader());
		}

		hrm.Headers.Add("X-Youtube-Client-Name", ((int)client).ToString());
		hrm.Headers.Add("X-Youtube-Client-Version", client switch
		{
			RequestClient.WEB => "2.20220809.02.00",
			RequestClient.ANDROID => "17.31.4",
			RequestClient.IOS => "17.31.4",
			var _ => ""
		});
		hrm.Headers.Add("Origin", "https://www.youtube.com");

		HttpResponseMessage ytPlayerRequest = await HttpClient.SendAsync(hrm);
		return JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());
	}

	/// <summary>
	/// Gets the player data of a video
	/// </summary>
	/// <param name="videoId">ID of the video</param>
	/// <param name="contentCheckOk">Set to true if you want to skip the content warnings (suicide, self-harm etc.)</param>
	/// <param name="includeHls">Set to true if you need HLS streams. Note that HLS streams are always sent for live videos and for non-live videos setting this to true will not return formats larger than 1080p</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	public async Task<InnerTubePlayer> GetPlayerAsync(string videoId, bool contentCheckOk, bool includeHls,
		string language = "en", string region = "US")
	{
		string cacheId = $"{videoId}_{(includeHls ? "hls" : "dash")}({language}_{region})";

		if (PlayerCache.Contains(cacheId))
			return (InnerTubePlayer)PlayerCache.Get(cacheId)!;

		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId)
			.AddValue("contentCheckOk", contentCheckOk)
			.AddValue("racyCheckOk", contentCheckOk);

		JObject playerResponse = await MakeRequest(includeHls ? RequestClient.IOS : RequestClient.ANDROID, "player",
			postData,
			language, region, true);
		string playabilityStatus = playerResponse.GetFromJsonPath<string>("playabilityStatus.status")!;
		if (playabilityStatus != "OK")
		{
			throw new PlayerException(playabilityStatus, 
			playerResponse.GetFromJsonPath<string>("playabilityStatus.reason")!,
			playerResponse.GetFromJsonPath<string>("playabilityStatus.reasonTitle")!);
		}
		InnerTubePlayer player = new(playerResponse);
		PlayerCache.Set(cacheId, player, DateTimeOffset.Now.AddSeconds(player.ExpiresInSeconds).AddSeconds(-player.Details.Length.TotalSeconds));
		return player;
	}
}