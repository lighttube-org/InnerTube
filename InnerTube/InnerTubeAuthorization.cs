using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Serilog;

namespace InnerTube;

/// <summary>
/// Authorization parameters for InnerTube
/// </summary>
public class InnerTubeAuthorization
{
	/// <summary>
	/// The way this authorization works
	/// </summary>
	public AuthorizationType Type;
	/// <summary>
	/// Secrets that an authorization implementation uses
	/// </summary>
	private Dictionary<string, object> Secrets = new();

	internal InnerTubeAuthorization()
	{ }

	/// <summary>
	/// Authorize with cookies. See https://github.com/lighttube-org/InnerTube/wiki/Authorization#using-cookies
	/// </summary>
	/// <param name="sid">The <code>SID</code> cookie from your browser session</param>
	/// <param name="hsid">The <code>HSID</code> cookie from your browser session</param>
	/// <param name="ssid">The <code>SSID</code> cookie from your browser session</param>
	/// <param name="apisid">The <code>APISID</code> cookie from your browser session</param>
	/// <param name="sapisid">The <code>SAPISID</code> cookie from your browser session</param>
	/// <returns></returns>
	[Obsolete("Cookie authorization does not work with protobuf. Use refresh tokens instead")]
	public static InnerTubeAuthorization SapisidAuthorization(
		string sid,
		string hsid,
		string ssid,
		string apisid,
		string sapisid) =>
		new()
		{
			Type = AuthorizationType.SAPISID,
			Secrets = new Dictionary<string, object>
			{
				["SID"] = sid,
				["HSID"] = hsid,
				["SSID"] = ssid,
				["APISID"] = apisid,
				["SAPISID"] = sapisid
			}
		};

	/// <summary>
	/// Authorize with an OAuth2 refresh token. See: https://github.com/kuylar/InnerTube/wiki/Authorization#using-a-refresh-token
	/// </summary>
	/// <param name="refreshToken">The refresh token from the OAuth response</param>
	/// <returns></returns>
	public static InnerTubeAuthorization RefreshTokenAuthorization(string refreshToken) =>
		new()
		{
			Type = AuthorizationType.REFRESH_TOKEN,
			Secrets = new Dictionary<string, object>
			{
				["refreshToken"] = refreshToken
			}
		};

	internal string GenerateCookieHeader()
	{
		switch (Type)
		{
			case AuthorizationType.SAPISID:
				return string.Join(", ", Secrets.Select(x => $"{x.Key}={x.Value}"));
			case AuthorizationType.REFRESH_TOKEN:
			default:
				return "";
		}
	}

	internal string GenerateAuthHeader()
	{
		switch (Type)
		{
			case AuthorizationType.SAPISID:
				long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				string hash = GenerateSha1Hash($"{timestamp} {Secrets["SAPISID"]} https://www.youtube.com");
				return $"SAPISIDHASH {timestamp}_{hash}";
			case AuthorizationType.REFRESH_TOKEN:
				RefreshAccessToken().Wait();
				return (string)Secrets["accessToken"];
			default:
				return "";
		}
	}

	private async Task RefreshAccessToken()
	{
		if (Secrets.ContainsKey("tokenExpiry"))
		{
			DateTimeOffset dto = (DateTimeOffset)Secrets["tokenExpiry"];
			if (DateTimeOffset.Compare(dto, DateTimeOffset.Now.AddHours(1)) > 0)
			{
				Log.Debug("[AUTHORIZATION] Access token still valid, continuing");
				return;
			}
		}

		Log.Debug("[AUTHORIZATION] Refreshing access token");
		string requestBody =
			$"{{\"client_id\":\"861556708454-d6dlm3lh05idd8npek18k6be8ba3oc68.apps.googleusercontent.com\",\"client_secret\":\"SboVhoG9s0rNafixCSGGKXAT\",\"grant_type\":\"refresh_token\",\"refresh_token\":\"{Secrets["refreshToken"]}\" }}";
		HttpClient client = new();
		HttpResponseMessage httpResponseMessage = await client.PostAsync("https://www.youtube.com/o/oauth2/token",
			new StringContent(requestBody, Encoding.UTF8, "application/json"));
		JObject res = JObject.Parse(await httpResponseMessage.Content.ReadAsStringAsync());

		if (Secrets.ContainsKey("accessToken"))
			Secrets["accessToken"] = $"{res["token_type"]} {res["access_token"]}";
		else
			Secrets.Add("accessToken", $"{res["token_type"]} {res["access_token"]}");

		if (Secrets.ContainsKey("tokenExpiry"))
			Secrets["tokenExpiry"] = DateTimeOffset.Now.AddSeconds(res["expires_in"]?.ToObject<int>() ?? 0);
		else
			Secrets.Add("tokenExpiry", DateTimeOffset.Now.AddSeconds(res["expires_in"]?.ToObject<int>() ?? 0));
	}

	private string GenerateSha1Hash(string input)
	{
		byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
		StringBuilder sb = new(hash.Length * 2);
		foreach (byte b in hash) sb.Append(b.ToString("X2"));
		return sb.ToString();
	}
}

/// <summary>
/// Authorization type
/// </summary>
public enum AuthorizationType
{
	/// <summary>
	/// Cookie based authorization
	/// </summary>
	SAPISID,
	/// <summary>
	/// OAuth2 based authorization
	/// </summary>
	REFRESH_TOKEN
}