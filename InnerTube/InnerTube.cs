using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Web;
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
		if (!ytPlayerRequest.IsSuccessStatusCode)
			throw new InnerTubeException($"Unexpected status code: [{(int)ytPlayerRequest.StatusCode}] {ytPlayerRequest.StatusCode}");
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
		PlayerCache.Set(cacheId, player,
			DateTimeOffset.Now.AddSeconds(player.ExpiresInSeconds).AddSeconds(-player.Details.Length.TotalSeconds));
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
	/// Gets more information about a video, including recommended videos and a comment continuation token to be used with GetVideoComments
	/// </summary>
	/// <param name="videoId">ID of the video</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>Video info, a key for the comments & a list of recommended videos</returns>
	public async Task<InnerTubeNextResponse> GetVideoNext(string videoId, string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("videoId", videoId);

		JObject nextResponse = await MakeRequest(RequestClient.WEB, "next", postData, language, region);
		return new InnerTubeNextResponse(nextResponse);
	}

	/// <summary>
	/// Gets the comments of a video from a comment continuation token that can be received from GetVideoNext 
	/// </summary>
	/// <param name="commentsContinuation">Continuation token received from GetVideoNext</param>
	/// <param name="language">Language of the content</param>
	/// <param name="region">Region of the content</param>
	/// <returns>List of comments for the video that belongs to the specified key</returns>
	public async Task<InnerTubeContinuationResponse> GetVideoComments(string commentsContinuation, string language = "en", string region = "US")
	{
		InnerTubeRequest postData = new InnerTubeRequest()
			.AddValue("continuation", commentsContinuation);

		JObject nextResponse = await MakeRequest(RequestClient.WEB, "next", postData, language, region);
		return InnerTubeContinuationResponse.GetFromComments(nextResponse);
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