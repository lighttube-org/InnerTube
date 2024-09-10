using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Google.Protobuf;
using InnerTube.Exceptions;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;
using Microsoft.Extensions.Caching.Memory;

namespace InnerTube;

/// <summary>
/// The InnerTube client.
/// </summary>
public partial class InnerTube
{
	internal readonly HttpClient HttpClient = new();
	internal readonly MemoryCache PlayerCache;
	internal readonly string ApiKey;
	internal readonly InnerTubeAuthorization? Authorization;
	internal Dictionary<RequestClient, string> VisitorDatas = [];
	internal Dictionary<RequestClient, string> PoTokens = [];
	private readonly Regex visitorDataRegex = VisitorDataGeneratedRegex();
	internal SignatureSolver SignatureSolver = new();

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
		string language, string region, bool authorized = false, string? referer = null)
	{
		string url = $"https://youtubei.googleapis.com/youtubei/v1/{endpoint}";
		url += "?alt=proto";
		if (!authorized || Authorization?.Type != AuthorizationType.REFRESH_TOKEN)
			url += $"&key={ApiKey}";

		HttpRequestMessage hrm = new(HttpMethod.Post, url);

		byte[] buffer = Encoding.UTF8.GetBytes(postData.GetJson(client, language, region,
			authorized ? VisitorDatas?.GetValueOrDefault(client) : null,
			authorized ? PoTokens?.GetValueOrDefault(client) : null, referer));
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
			RequestClient.WEB => Constants.WebClientVersion,
			RequestClient.ANDROID => Constants.MobileClientVersion,
			RequestClient.IOS => Constants.MobileClientVersion,
			RequestClient.TVAPPLE => Constants.TvAppleClientVersion,
			RequestClient.MWEB_TIER_2 => Constants.MwebTier2ClientVersion,
			RequestClient.TV_UNPLUGGED_CAST => Constants.TvUnpluggedCastClientVersion,
			RequestClient.TV_EMBEDDED => Constants.TvEmbeddedClientVersion,
			RequestClient.MEDIA_CONNECT_FRONTEND => Constants.MediaConnectFrontendClientVersion,
			_ => ""
		});
		//hrm.Headers.Add("Origin", "https://www.youtube.com");
		switch (client)
		{
			case RequestClient.WEB:			
				hrm.Headers.TryAddWithoutValidation("User-Agent", Constants.WebUserAgent);
				break;
			case RequestClient.ANDROID:			
				hrm.Headers.Add("User-Agent", Constants.AndroidUserAgent);
				break;
			case RequestClient.IOS:
				hrm.Headers.Add("User-Agent", Constants.IosUserAgent);
				break;
		}

		HttpResponseMessage ytPlayerRequest = await HttpClient.SendAsync(hrm);
		if (!ytPlayerRequest.IsSuccessStatusCode)
			throw new RequestException(ytPlayerRequest.StatusCode, client, await ytPlayerRequest.Content.ReadAsStringAsync());
		return await ytPlayerRequest.Content.ReadAsByteArrayAsync();
	}

	public async Task<string> GenerateVisitorData()
	{
		HttpRequestMessage req = new(HttpMethod.Get, "https://www.youtube.com");
		req.Headers.Add("Cookie", "CONSENT=PENDING+742");
		HttpResponseMessage res = await HttpClient.SendAsync(req);
		string html = await res.Content.ReadAsStringAsync();
		return visitorDataRegex.Match(html).Groups[1].Value;
	}

	public void ProvideSecrets(RequestClient client, string visitorData, string? poToken = null)
	{
		VisitorDatas[client] = visitorData;
		if (poToken != null)
			PoTokens[client] = poToken;
	}

	/// <summary>
	/// Gets the player data of a video
	/// </summary>
	/// <param name="videoId">ID of the video</param>
	/// <param name="contentCheckOk">Set to true if you want to skip the content warnings (suicide, self-harm etc.)</param>
	/// <param name="fallbackToUnserializedResponse">For future streams/premieres with trailers, the response will have
	/// another video response inside the main response. Set this to true to use that response, if its available</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	public async Task<PlayerResponse> GetPlayerAsync(string videoId, bool contentCheckOk = false,
		bool fallbackToUnserializedResponse = false, RequestClient client = RequestClient.WEB, string language = "en", string region = "US")
	{
		if (!SignatureSolver.Initialized)
			await SignatureSolver.LoadLatestJs(videoId);
		string cacheId = $"{videoId}_({language})";

		if (PlayerCache.TryGetValue(cacheId, out PlayerResponse? cachedPlayer)) return cachedPlayer!;

		PlayerResponse player = await GetPlayerObjectAsync(videoId, contentCheckOk, language, region, client);
		PlayerResponse? microformatPlayer = client != RequestClient.WEB
			? await GetPlayerObjectAsync(videoId, contentCheckOk, language, region, RequestClient.WEB)
			: null;

		foreach (Format format in player.StreamingData?.Formats ?? [])
			SignatureSolver.DescrambleUrl(format);
		foreach (Format format in player.StreamingData?.AdaptiveFormats ?? [])
			SignatureSolver.DescrambleUrl(format);
		foreach (Format format in player.StreamingData?.HlsFormats ?? [])
			SignatureSolver.DescrambleUrl(format);
		
		if (player.PlayabilityStatus.Status == PlayabilityStatus.Types.Status.LiveStreamOffline)
		{
			YpcTrailerRenderer? ypc = player.PlayabilityStatus.ErrorScreen?.YpcTrailerRenderer;
			PlayerResponse? fallbackResponse = ypc?.UnserializedPlayerResponse ?? ypc?.PlayerResponse;
			
			if (fallbackResponse == null || !fallbackToUnserializedResponse)
	            throw new PlayerException(player.PlayabilityStatus.Status,
		            player.PlayabilityStatus.Reason, player.PlayabilityStatus.Subreason);
			cacheId = ""; // dont cache
			// keep microformat & videodetails alive
			player.Captions = fallbackResponse.Captions;
			player.StreamingData = fallbackResponse.StreamingData;
			player.PlayabilityStatus = fallbackResponse.PlayabilityStatus;
			player.Storyboards = fallbackResponse.Storyboards;
			player.Endscreen = fallbackResponse.Endscreen;
		}
		if (player.PlayabilityStatus.Status != PlayabilityStatus.Types.Status.Ok)
			throw new PlayerException(player.PlayabilityStatus.Status, player.PlayabilityStatus.Reason,
				player.PlayabilityStatus.Subreason);
		
		try
		{
			if (!string.IsNullOrEmpty(player.StreamingData?.HlsManifestUrl))
			{
				string hls = await HttpClient.GetStringAsync(player.StreamingData.HlsManifestUrl);
				string[] lines = hls.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (!line.StartsWith("#EXT-X-STREAM-INF")) continue;
					if (line.StartsWith("http")) continue;

					Dictionary<string,string> info = Utils.ParseHlsStreamInfo(line);
					i++;
					string url = lines[i];

					Dictionary<string, string> urlInfo = Utils.ParseHlsUrlInfo(url);
					
					player.StreamingData?.HlsFormats.Add(new Format
					{
						Itag = int.Parse(urlInfo["itag"]),
						Url = url,
						Mime = $"video/mp2t; codecs={info["CODECS"]}",
						Bitrate = int.Parse(info["BANDWIDTH"]),
						Width = int.Parse(info["RESOLUTION"].Split('x')[0]),
						Height = int.Parse(info["RESOLUTION"].Split('x')[1]),
						LastModified =
							Math.Max(
								ulong.Parse(urlInfo["sgoap"].Split(';').FirstOrDefault(x => x.StartsWith("lmt="))?.Split('=')[1] ?? "0"),
								ulong.Parse(urlInfo["sgovp"].Split(';').FirstOrDefault(x => x.StartsWith("lmt="))?.Split('=')[1] ?? "0")
							),
						Quality = "hlsmuxed_" + urlInfo["itag"],
						Fps = int.Parse(info["FRAME-RATE"]),
						QualityLabel = $"{info["RESOLUTION"].Split('x')[1]}p (HLS)",
					});
				}
			}
		}
		catch (Exception)
		{
			// ignore errors, since theres a high change of the hls endpoint
			// returning a 429 if used in an environment with heavy traffic
		}

		if (microformatPlayer != null)
			player.Microformat = microformatPlayer.Microformat;

		if (cacheId != "")
			PlayerCache.Set(cacheId, player, new MemoryCacheEntryOptions
			{
				Size = 1,
				SlidingExpiration = TimeSpan.FromSeconds(Math.Max(600, player.VideoDetails.LengthSeconds)),
				AbsoluteExpirationRelativeToNow =
					TimeSpan.FromSeconds(Math.Max(3600,
						player.StreamingData!.ExpiresInSeconds - player.VideoDetails.LengthSeconds))
			});
		return player;
	}

	private async Task<PlayerResponse> GetPlayerObjectAsync(string videoId, bool contentCheckOk, string language,
		string region, RequestClient client)
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId)
			.AddValue("contentCheckOk", contentCheckOk)
			.AddValue("racyCheckOk", contentCheckOk);

		return PlayerResponse.Parser.ParseFrom(await MakeRequest(client, "player", postData,
			language, region, true, "https://www.youtube.com/watch?v=" + videoId));
	}

	public async Task<NextResponse> GetNextAsync(string videoId, bool contentCheckOk = false,
		bool captionsRequested = false, string? playlistId = null, int? playlistIndex = null,
		string? playlistParams = null, string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId)
			.AddValue("contentCheckOk", contentCheckOk)
			.AddValue("racyCheckOk", contentCheckOk)
			.AddValue("captionsRequested", captionsRequested);
		if (playlistId != null) postData.AddValue("playlistId", playlistId);
		if (playlistIndex != null) postData.AddValue("playlistIndex", playlistIndex);
		if (playlistParams != null) postData.AddValue("params", playlistParams);
		NextResponse next =
			NextResponse.Parser.ParseFrom(await MakeRequest(RequestClient.WEB, "next", postData, language, region));
		if (next.Contents.TwoColumnWatchNextResults.Results.ResultsContainer.Results.Count == 0)
		{
			throw new InnerTubeException("Empty response, video is either deleted or private");
		}

		RendererWrapper parent = next.Contents.TwoColumnWatchNextResults.Results.ResultsContainer.Results[0];
		if (parent.RendererCase ==
		    RendererWrapper.RendererOneofCase.ItemSectionRenderer)
		{
			if (parent.ItemSectionRenderer.Contents[0].RendererCase ==
			    RendererWrapper.RendererOneofCase.BackgroundPromoRenderer)
			{
				throw new InnerTubeException(Utils.ReadRuns(parent.ItemSectionRenderer.Contents[0]
					.BackgroundPromoRenderer.Text));
			}
		}

		return next;
	}

	public async Task<NextResponse> ContinueNextAsync(string continuation, string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", continuation);
		NextResponse next =
			NextResponse.Parser.ParseFrom(await MakeRequest(RequestClient.WEB, "next", postData, language, region));
		if (next.Contents != null)
		{
			RendererWrapper parent = next.Contents.TwoColumnWatchNextResults.Results.ResultsContainer.Results[0];
			if (parent.RendererCase != RendererWrapper.RendererOneofCase.ItemSectionRenderer) return next;
			if (parent.ItemSectionRenderer.Contents[0].RendererCase ==
			    RendererWrapper.RendererOneofCase.BackgroundPromoRenderer)
			{
				throw new InnerTubeException(
					Utils.ReadRuns(parent.ItemSectionRenderer.Contents[0].BackgroundPromoRenderer.Text));
			}
		}

		if (next.OnResponseReceivedEndpoints.Count == 0)
			throw new InnerTubeException("No data returned from YouTube");

		return next;
	}

	public async Task<SearchResponse> SearchAsync(string query, SearchParams? param = null, string language = "en",
		string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("query", query);
		
		if (param != null)
			postData.AddValue("params", Convert.ToBase64String(param.ToByteArray()));
		return SearchResponse.Parser.ParseFrom(await MakeRequest(RequestClient.WEB, "search", postData, language, region));
	}

	public async Task<SearchResponse> ContinueSearchAsync(string continuation, string language = "en",
		string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", continuation);
		return SearchResponse.Parser.ParseFrom(await MakeRequest(RequestClient.WEB, "search", postData, language, region));
	}

	public async Task<BrowseResponse> BrowseAsync(string browseId, string? param = null, string? query = null, string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("browseId", browseId);
		if (param != null)
			postData.AddValue("params", param);
		if (query != null)
			postData.AddValue("query", query);
		return BrowseResponse.Parser.ParseFrom(await MakeRequest(RequestClient.WEB, "browse", postData, language, region));
	}

	public async Task<BrowseResponse> ContinueBrowseAsync(string continuation, string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", continuation);
		return BrowseResponse.Parser.ParseFrom(await MakeRequest(RequestClient.WEB, "browse", postData, language, region));
	}

	public async Task<ResolveUrlResponse> ResolveUrl(string url)
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("url", url);
		return ResolveUrlResponse.Parser.ParseFrom(await MakeRequest(RequestClient.WEB, "navigation/resolve_url",
			postData, "en", "US"));
	}

    [GeneratedRegex("\"visitorData\":\"(.+?)\",")]
    private static partial Regex VisitorDataGeneratedRegex();
}