using System.Net.Http.Headers;
using System.Text;
using System.Web;
using InnerTube.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTube
{
	internal readonly HttpClient HttpClient = new();
	internal readonly MemoryCache PlayerCache;
	internal readonly string ApiKey;
	internal readonly InnerTubeAuthorization? Authorization;

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
	/// for non-live videos setting this to true will not return formats larger than 1080p
	/// </param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	public async Task<InnerTubePlayer> GetPlayerAsync(string videoId, bool contentCheckOk, bool includeHls,
		string language = "en", string region = "US")
	{
		string cacheId = $"{videoId}_{(includeHls ? "hls" : "dash")}({language}_{region})";

		if (PlayerCache.TryGetValue(cacheId, out InnerTubePlayer cachedPlayer)) return cachedPlayer;

		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId)
			.AddValue("contentCheckOk", contentCheckOk)
			.AddValue("racyCheckOk", contentCheckOk);

		JObject playerResponse = await MakeRequest(includeHls ? RequestClient.IOS : RequestClient.ANDROID, "player",
			postData,
			language, region, true);
		string playabilityStatus = playerResponse.GetFromJsonPath<string>("playabilityStatus.status")!;
		if (playabilityStatus != "OK")
			throw new PlayerException(playabilityStatus,
				playerResponse.GetFromJsonPath<string>("playabilityStatus.reason")!,
				playerResponse.GetFromJsonPath<string>("playabilityStatus.reasonTitle")!);

		InnerTubePlayer player = new(playerResponse);
		PlayerCache.Set(cacheId, player, new MemoryCacheEntryOptions
		{
			Size = 1,
			SlidingExpiration = TimeSpan.FromSeconds(Math.Max(600, player.Details.Length.TotalSeconds)),
			AbsoluteExpirationRelativeToNow =
				TimeSpan.FromSeconds(player.ExpiresInSeconds - player.Details.Length.TotalSeconds)
		});
		return player;
	}

	/// <summary>
	/// Search using a query
	/// </summary>
	/// <param name="query">Query of what to search</param>
	/// <param name="filterParams">Filter params. Get this from InnerTubeSearchResults.SearchOptions</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of results</returns>
	public async Task<InnerTubeSearchResults> SearchAsync(string query, string? filterParams = null,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("query", query);

		if (filterParams != null)
			postData.AddValue("contentCheckOk", filterParams);

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
	/// <param name="playlistIndex"></param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>Video info, a key for the comments & a list of recommended videos</returns>
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
	/// Get information about a channel
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
	/// <param name="continuation">Continuation token from an older GetPlaylistAsync call</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>More videos from a playlist</returns>
	public async Task<InnerTubeContinuationResponse> ContinuePlaylistAsync(string continuation,
		string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", continuation);

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
	/// Get a list of all valid languages & regions
	/// </summary>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of all valid languages & regions</returns>
	public async Task<InnerTubeLocals> GetLocalsAsync(string language = "en", string region = "US")
	{
		JObject localsResponse = await MakeRequest(RequestClient.WEB, "account/account_menu", new InnerTubeRequest(),
			language, region);
		return new InnerTubeLocals(localsResponse);
	}
}