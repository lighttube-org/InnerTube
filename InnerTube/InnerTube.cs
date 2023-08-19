using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Google.Protobuf;
using InnerTube.Exceptions;
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

		RendererManager.LoadRenderers();
	}

	private async Task<JObject> MakeRequest(RequestClient client, string endpoint, InnerTubeRequest postData,
		string language, string region, bool authorized = false)
	{
		HttpRequestMessage hrm = new(HttpMethod.Post,
			@$"https://www.youtube.com/youtubei/v1/{endpoint}?prettyPrint=false{(authorized && Authorization?.Type == AuthorizationType.REFRESH_TOKEN ? "" : $"&key={ApiKey}")}");

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
			RequestClient.ANDROID => "17.31.4",
			RequestClient.IOS => "17.31.4",
			var _ => ""
		});
		hrm.Headers.Add("Origin", "https://www.youtube.com");
		if (client == RequestClient.ANDROID)
			hrm.Headers.Add("User-Agent", "com.google.android.youtube/17.31.35 (Linux; U; Android 11) gzip");

		HttpResponseMessage ytPlayerRequest = await HttpClient.SendAsync(hrm);
		if (!ytPlayerRequest.IsSuccessStatusCode)
			throw new RequestException(ytPlayerRequest.StatusCode, await ytPlayerRequest.Content.ReadAsStringAsync());
		return JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());
	}

	/// <summary>
	/// Gets the player data of a video
	/// </summary>
	/// <param name="videoId">ID of the video</param>
	/// <param name="contentCheckOk">Set to true if you want to skip the content warnings (suicide, self-harm etc.)</param>
	/// <param name="includeHls">
	/// Set to true if you need HLS streams. Note that HLS streams are always sent for live videos and
	/// for non-live videos setting this to true will not return formats larger than 1080p <br></br>
	/// If this is set to true, Formats will be empty
	/// </param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	public async Task<InnerTubePlayer> GetPlayerAsync(string videoId, bool contentCheckOk = false,
		bool includeHls = false,
		string language = "en", string region = "US")
	{
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

		InnerTubePlayer player = new(responses[1], responses[0]);
		PlayerCache.Set(cacheId, player, new MemoryCacheEntryOptions
		{
			Size = 1,
			SlidingExpiration = TimeSpan.FromSeconds(Math.Max(600, player.Details.Length.TotalSeconds)),
			AbsoluteExpirationRelativeToNow =
				TimeSpan.FromSeconds(Math.Max(3600, player.ExpiresInSeconds - player.Details.Length.TotalSeconds))
		});
		return player;
	}

	// instead of trying hours to find a protobuf compilation to
	// have endscreen, cards, storyboards and non 403'ing video
	// data at the same time i decided to just do the request
	// twice, one WEB and one ANDROID. if someone finds a protobuf
	// string that returns all those on the android client pls pr <3
	private async Task<JObject> GetPlayerObjectAsync(string videoId, bool contentCheckOk, string language,
		string region, RequestClient client)
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId)
			.AddValue("contentCheckOk", contentCheckOk)
			.AddValue("racyCheckOk", contentCheckOk);

		if (client == RequestClient.ANDROID)
			postData.AddValue("params", "CgIQBg");

		return await MakeRequest(client, "player", postData,
			language, region, true);
	}

	/// <summary>
	/// Search using a query
	/// </summary>
	/// <param name="query">Query of what to search</param>
	/// <param name="param">Filter params.</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of results</returns>
	public async Task<InnerTubeSearchResults> SearchAsync(string query, SearchParams? param,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("query", query);

		if (param != null)
			postData.AddValue("params", Convert.ToBase64String(param.ToByteArray()));

		JObject searchResponse = await MakeRequest(RequestClient.WEB, "search", postData,
			language, region);
		return new InnerTubeSearchResults(searchResponse);
	}

	/// <summary>
	/// Continue an old search query using its continuation token
	/// </summary>
	/// <param name="continuation">Continuation token received from an older response</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of continuation results</returns>
	public async Task<InnerTubeContinuationResponse> ContinueSearchAsync(string continuation,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", continuation);

		JObject searchResponse = await MakeRequest(RequestClient.WEB, "search", postData,
			language, region);
		return InnerTubeContinuationResponse.GetFromSearchResponse(searchResponse);
	}

	/// <summary>
	/// Gets a list of search autocomplete using the given query
	/// </summary>
	/// <param name="query">Query to get autocompletes in</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of complete strings</returns>
	public async Task<InnerTubeSearchAutocomplete> GetSearchAutocompleteAsync(string query,
		string language = "en", string region = "US")
	{
		// TODO: this is inefficient
		// do some funny things with the mobile app to see how we should *actually* do this
		HttpResponseMessage response = await HttpClient.GetAsync(
			$"https://suggestqueries-clients6.youtube.com/complete/search?client=youtube&hl={language}&gl={region.ToLower()}&ds=yt&q={HttpUtility.UrlEncode(query)}");

		return new InnerTubeSearchAutocomplete(await response.Content.ReadAsStringAsync());
	}

	/// <summary>
	/// Gets more information about a video, including recommended videos and a comment continuation token to be used with
	/// GetVideoCommentsAsync
	/// </summary>
	/// <param name="videoId">ID of the video</param>
	/// <param name="playlistId">ID of a playlist that contains this video. Must start with either PL or OLAK</param>
	/// <param name="playlistIndex">Index of the video for the playlist this video is in. Requires <paramref name="playlistId"/> to be set.</param>
	/// <param name="playlistParams">Params for the playlist this video is in. Requires <paramref name="playlistId"/> to be set.</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>Video info, a key for the comments &amp; a list of recommended videos</returns>
	public async Task<InnerTubeNextResponse> GetVideoAsync(string videoId, string? playlistId = null,
		int? playlistIndex = null, string? playlistParams = null, string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId);
		if (playlistId != null)
			postData.AddValue("playlistId", playlistId);
		if (playlistId != null && playlistIndex != null)
			postData.AddValue("playlistIndex", playlistIndex);
		if (playlistParams != null)
			postData.AddValue("params", playlistParams);

		JObject nextResponse = await MakeRequest(RequestClient.WEB, "next", postData, language, region);
		return new InnerTubeNextResponse(nextResponse);
	}

	/// <summary>
	/// Gets the comments of a video from a comment continuation token that can be received from GetVideoAsync
	/// </summary>
	/// <param name="commentsContinuation">Continuation token received from GetVideoAsync</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of comments for the video that belongs to the specified key</returns>
	public async Task<InnerTubeContinuationResponse> GetVideoCommentsAsync(string commentsContinuation,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", commentsContinuation);

		JObject nextResponse = await MakeRequest(RequestClient.WEB, "next", postData, language, region);
		return InnerTubeContinuationResponse.GetFromComments(nextResponse);
	}

	/// <summary>
	/// Get information about a channel
	/// </summary>
	/// <param name="channelId">ID of a channel, starts with UC</param>
	/// <param name="tab">
	/// Tab of the requested channel. ChannelTabs.Community will return the same response as ChannelTabs.Home
	/// if the channel does not have community enabled
	/// </param>
	/// <param name="searchQuery">Query to search in this channel. Only used if tab is ChannelTabs.Search</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>Information about a channel</returns>
	public async Task<InnerTubeChannelResponse> GetChannelAsync(string channelId, ChannelTabs tab = ChannelTabs.Home,
		string? searchQuery = null,
		string language = "en", string region = "US")
	{
		// channel ID is a vanity url/handle
		if (!channelId.StartsWith("UC"))
		{
			channelId = await GetChannelIdFromVanity(
				channelId.StartsWith("@")
					? $"https://youtube.com/{channelId}"
					: $"https://youtube.com/c/{channelId}") ?? channelId;
		}

		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("browseId", channelId);
		if (tab == ChannelTabs.Search && searchQuery is not null)
			postData
				.AddValue("params", tab.GetParams())
				.AddValue("query", searchQuery);
		else if (tab != ChannelTabs.Search)
			postData
				.AddValue("params", tab.GetParams());

		JObject browseResponse = await MakeRequest(RequestClient.WEB, "browse", postData, language, region);

		return new InnerTubeChannelResponse(browseResponse);
	}

	/// <summary>
	/// Get a channel ID from its vanity URL or @handle
	/// </summary>
	/// <param name="vanityUrl">
	/// The vanity URL or the handle of the channel
	/// <br/>
	/// If this is a handle, make sure it follows the format @url
	/// <br/>
	/// If this is a vanity URL, only pass in the part after the /c/ 
	/// </param>
	/// <returns>Channel ID of the given vanity URL, or null if given ID is not valid</returns>
	public async Task<string?> GetChannelIdFromVanity(string vanityUrl)
	{
		if (vanityUrl.StartsWith("@"))
			vanityUrl = "https://youtube.com/" + vanityUrl;
		else if (!vanityUrl.StartsWith("http"))
			vanityUrl = "https://youtube.com/c/" + vanityUrl;

		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("url", vanityUrl);

		JObject browseResponse =
			await MakeRequest(RequestClient.ANDROID, "navigation/resolve_url", postData, "en", "US");

		return browseResponse.GetFromJsonPath<string>("endpoint.browseEndpoint.browseId");
	}

	/// <summary>
	/// Paginate through a channel
	/// </summary>
	/// <param name="continuation">Continuation token from an older GetSearchAsync call</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>Information about a channel</returns>
	public async Task<InnerTubeContinuationResponse> ContinueChannelAsync(string continuation,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", continuation);

		JObject browseResponse = await MakeRequest(RequestClient.WEB, "browse", postData, language, region);

		return InnerTubeContinuationResponse.GetFromBrowse(browseResponse);
	}

	/// <summary>
	/// Get videos from a playlist
	/// </summary>
	/// <param name="playlistId">ID of the playlist. Must start with either VL, PL or OLAK</param>
	/// <param name="includeUnavailable">Set to true if you want to received [Deleted video]s and such</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>Information and videos of a playlist</returns>
	public async Task<InnerTubePlaylist> GetPlaylistAsync(string playlistId, bool includeUnavailable = false,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("browseId",
				playlistId.StartsWith("VL") ? playlistId :
				playlistId.StartsWith("OL") ? playlistId : "VL" + playlistId);
		if (includeUnavailable)
			postData.AddValue("params", "wgYCCAA%3D");

		JObject browseResponse = await MakeRequest(RequestClient.WEB, "browse", postData, language, region);

		return new InnerTubePlaylist(browseResponse);
	}

	/// <summary>
	/// Get videos from a playlist
	/// </summary>
	/// <param name="playlistId">ID of the playlist. Must start with either VL, PL or OLAK</param>
	/// <param name="skipAmount">Amount of items to skip. Usually page multiplied by 100</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>More videos from a playlist</returns>
	public async Task<InnerTubeContinuationResponse> ContinuePlaylistAsync(string playlistId, int skipAmount,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", Utils.PackPlaylistContinuation(
				playlistId.StartsWith("VL") ? playlistId :
				playlistId.StartsWith("OL") ? playlistId : 
				"VL" + playlistId, skipAmount));

		JObject browseResponse = await MakeRequest(RequestClient.WEB, "browse", postData, language, region);

		return InnerTubeContinuationResponse.GetFromBrowse(browseResponse);
	}

	/// <summary>
	/// Get raw renderers from a browseId. Do not use unless a method for what you're trying to do does not exist.
	/// </summary>
	/// <param name="browseId">A browseId you can gather from the InnerTube API.</param>
	/// <param name="browseParams">Parameters for this browseId</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns></returns>
	public async Task<InnerTubeExploreResponse> BrowseAsync(string browseId, string? browseParams = null,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("browseId", browseId);

		if (browseParams is not null)
			postData.AddValue("params", browseParams);

		JObject browseResponse = await MakeRequest(RequestClient.WEB, "browse", postData, language, region);

		return new InnerTubeExploreResponse(browseResponse, browseId);
	}

	/// <summary>
	/// Get more renderers from a continuation key received from BrowseAsync.
	/// </summary>
	/// <param name="continuation">Continuation token from an older BrowseAsync call</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>More renderers from the given continuation key</returns>
	public async Task<InnerTubeContinuationResponse> ContinueBrowseAsync(string continuation,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", continuation);

		JObject browseResponse = await MakeRequest(RequestClient.WEB, "browse", postData, language, region);

		return InnerTubeContinuationResponse.GetFromBrowse(browseResponse);
	}

	/// <summary>
	/// Get a list of all valid languages &amp; regions
	/// </summary>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of all valid languages &amp; regions</returns>
	public async Task<InnerTubeLocals> GetLocalsAsync(string language = "en", string region = "US")
	{
		JObject localsResponse = await MakeRequest(RequestClient.WEB, "account/account_menu", new InnerTubeRequest(),
			language, region);
		return new InnerTubeLocals(localsResponse);
	}
}
