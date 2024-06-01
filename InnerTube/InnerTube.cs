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
	internal readonly SignatureSolver SignatureSolver = new();
	internal string? VisitorData;
	private readonly Regex visitorDataRegex = VisitorDataGeneratedRegex();

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
		if (VisitorData is null) 
			await UpdateVisitorData();
		string url = $"https://youtubei.googleapis.com/youtubei/v1/{endpoint}";
		url += "?alt=proto";
		if (!authorized || Authorization?.Type != AuthorizationType.REFRESH_TOKEN)
			url += $"&key={ApiKey}";


		HttpRequestMessage hrm = new(HttpMethod.Post, url);

		byte[] buffer =
			Encoding.UTF8.GetBytes(postData.GetJson(client, language, region, authorized ? VisitorData : null, referer));
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
			RequestClient.TV_EMBEDDED => Constants.TvEmbeddedClientVersion,
			_ => ""
		});
		//hrm.Headers.Add("Origin", "https://www.youtube.com");
		if (client == RequestClient.ANDROID)
			hrm.Headers.Add("User-Agent", "com.google.android.youtube/19.09.4 (Linux; U; Android 11) gzip");

		HttpResponseMessage ytPlayerRequest = await HttpClient.SendAsync(hrm);
		if (!ytPlayerRequest.IsSuccessStatusCode)
			throw new RequestException(ytPlayerRequest.StatusCode, await ytPlayerRequest.Content.ReadAsStringAsync());
		return await ytPlayerRequest.Content.ReadAsByteArrayAsync();
	}

	private async Task UpdateVisitorData()
	{
		HttpRequestMessage req = new(HttpMethod.Get, "https://www.youtube.com");
		req.Headers.Add("Cookie", "CONSENT=PENDING+742");
		HttpResponseMessage res = await HttpClient.SendAsync(req);
		string html = await res.Content.ReadAsStringAsync();
		VisitorData = visitorDataRegex.Match(html).Groups[1].Value;
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
		bool fallbackToUnserializedResponse = false, string language = "en", string region = "US")
	{
		if (!SignatureSolver.Initialized)
			await SignatureSolver.LoadLatestJs(videoId);
		string cacheId = $"{videoId}_({language}_{region})";

		if (PlayerCache.TryGetValue(cacheId, out PlayerResponse? cachedPlayer)) return cachedPlayer!;

		// if authorized, use the WEB client to make sure all age-gated videos play.
		// if not, use TV_EMBEDDED to bypass embeddable videos 
		Task<PlayerResponse> webResponse =
			GetPlayerObjectAsync(videoId, contentCheckOk, SignatureSolver.SignatureTimestamp, language, region,
				RequestClient.WEB);
		Task<PlayerResponse>? tvEmbeddedResponse = Authorization == null
			? GetPlayerObjectAsync(videoId, contentCheckOk, SignatureSolver.SignatureTimestamp, language, region,
				RequestClient.TV_EMBEDDED)
			: null;
		Task<PlayerResponse> iosResponse =
			GetPlayerObjectAsync(videoId, contentCheckOk, SignatureSolver.SignatureTimestamp, language, region,
				RequestClient.IOS);

		PlayerResponse webPlayer = await webResponse;
		PlayerResponse? tvEmbeddedPlayer = tvEmbeddedResponse != null ? await tvEmbeddedResponse : null;
		PlayerResponse? iosPlayer = await iosResponse;

		if (Authorization == null)
		{
			PlayerResponse a = webPlayer;
			webPlayer = tvEmbeddedPlayer!;
			tvEmbeddedPlayer = a;
			webPlayer.PlayabilityStatus = tvEmbeddedPlayer.PlayabilityStatus;
			webPlayer.Microformat = tvEmbeddedPlayer.Microformat;
		}

		if (webPlayer.PlayabilityStatus.Status == PlayabilityStatus.Types.Status.LiveStreamOffline)
		{
			PlayerResponse? unserializedResponse = webPlayer.PlayabilityStatus.ErrorScreen?.YpcTrailerRenderer?.UnserializedPlayerResponse;
			if (unserializedResponse == null || !fallbackToUnserializedResponse)
	            throw new PlayerException(webPlayer.PlayabilityStatus.Status,
		            webPlayer.PlayabilityStatus.Reason, webPlayer.PlayabilityStatus.Subreason);
			cacheId = ""; // dont cache
			// keep microformat & videodetails alive
			webPlayer.Captions = unserializedResponse.Captions;
			webPlayer.StreamingData = unserializedResponse.StreamingData;
			webPlayer.PlayabilityStatus = unserializedResponse.PlayabilityStatus;
			webPlayer.Storyboards = unserializedResponse.Storyboards;
			webPlayer.Endscreen = unserializedResponse.Endscreen;
			iosPlayer = iosPlayer.PlayabilityStatus.ErrorScreen?.YpcTrailerRenderer?.UnserializedPlayerResponse;
		}
		if (webPlayer.PlayabilityStatus.Status != PlayabilityStatus.Types.Status.Ok)
			throw new PlayerException(webPlayer.PlayabilityStatus.Status, webPlayer.PlayabilityStatus.Reason,
				webPlayer.PlayabilityStatus.Subreason);

		if (webPlayer.StreamingData != null && iosPlayer?.StreamingData != null &&
		    !webPlayer.StreamingData.HasHlsManifestUrl && iosPlayer.StreamingData.HasHlsManifestUrl)
			webPlayer.StreamingData.HlsManifestUrl = iosPlayer.StreamingData.HlsManifestUrl;

		if (webPlayer.StreamingData != null)
		{
			foreach (Format format in webPlayer.StreamingData.Formats) 
				SignatureSolver.DescrambleUrl(format);
			foreach (Format format in webPlayer.StreamingData.AdaptiveFormats) 
				SignatureSolver.DescrambleUrl(format);
		}

		// Only WEB players have microformat
		if (webPlayer.Microformat == null && iosPlayer?.Microformat != null)
			webPlayer.Microformat = iosPlayer.Microformat;

		if (cacheId != "")
			PlayerCache.Set(cacheId, webPlayer, new MemoryCacheEntryOptions
			{
				Size = 1,
				SlidingExpiration = TimeSpan.FromSeconds(Math.Max(600, webPlayer.VideoDetails.LengthSeconds)),
				AbsoluteExpirationRelativeToNow =
					TimeSpan.FromSeconds(Math.Max(3600,
						webPlayer.StreamingData!.ExpiresInSeconds - webPlayer.VideoDetails.LengthSeconds))
			});
		if (webPlayer.Microformat == null) throw new Exception("no microformat?");
		return webPlayer;
	}

	private async Task<PlayerResponse> GetPlayerObjectAsync(string videoId, bool contentCheckOk, int? signatureTimestamp, string language,
		string region, RequestClient client)
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId)
			.AddValue("contentCheckOk", contentCheckOk)
			.AddValue("racyCheckOk", contentCheckOk);

		if (client is RequestClient.TV_EMBEDDED or RequestClient.WEB && signatureTimestamp != null)
		{
			postData.AddValue("playbackContext", new Dictionary<string, object>
			{
				["contentPlaybackContext"] = new Dictionary<string, object>
				{
					["html5Preference"] = "HTML5_PREF_WANTS",
					["lactMilliseconds"] = "2132",
					["referer"] = "https://www.youtube.com/watch?v=" + videoId,
					["signatureTimestamp"] = signatureTimestamp,
					["autoCaptionsDefaultOn"] = false,
					["autoplay"] = true,
					["mdxContext"] = new Dictionary<string, string>(),
					["playerWidthPixels"] = 770,
					["playerHeightPixels"] = 433
				}
			});	
		}

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