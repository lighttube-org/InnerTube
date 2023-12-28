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
			RequestClient.ANDROID => "19.09.4",
			RequestClient.IOS => "19.09.4",
			var _ => ""
		});
		hrm.Headers.Add("Origin", "https://www.youtube.com");
		if (client == RequestClient.ANDROID)
			hrm.Headers.Add("User-Agent", "com.google.android.youtube/19.09.4 (Linux; U; Android 11) gzip");

		HttpResponseMessage ytPlayerRequest = await HttpClient.SendAsync(hrm);
		if (!ytPlayerRequest.IsSuccessStatusCode)
			throw new RequestException(ytPlayerRequest.StatusCode, await ytPlayerRequest.Content.ReadAsStringAsync());
		return JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());
	}
}