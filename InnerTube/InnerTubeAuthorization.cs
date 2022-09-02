using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Serilog;

namespace InnerTube;

public class InnerTubeAuthorization
{
	public AuthorizationType Type;
	private Dictionary<string, object> Secrets = new();

	internal InnerTubeAuthorization()
	{ }

	public static InnerTubeAuthorization SapisidAuthorization(string sapisid, string psid) =>
		new()
		{
			Type = AuthorizationType.SAPISID,
			Secrets = new Dictionary<string, object>
			{
				["SAPISID"] = sapisid,
				["PSID"] = psid
			}
		};

	public static InnerTubeAuthorization RefreshTokenAuthorization(string refreshToken) =>
		new()
		{
			Type = AuthorizationType.REFRESH_TOKEN,
			Secrets = new Dictionary<string, object>
			{
				["refreshToken"] = refreshToken
			}
		};

	public string GenerateCookieHeader()
	{
		switch (Type)
		{
			case AuthorizationType.SAPISID:
				return
					$"SAPISID={Secrets["SAPISID"]}; __Secure-3PAPISID={Secrets["SAPISID"]}; __Secure-3PSID={Secrets["PSID"]};";
			case AuthorizationType.REFRESH_TOKEN:
			default:
				return "";
		}
	}

	public string GenerateAuthHeader()
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
		using SHA1Managed sha1 = new();
		byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
		StringBuilder sb = new(hash.Length * 2);
		foreach (byte b in hash) sb.Append(b.ToString("X2"));
		return sb.ToString();
	}
}

public enum AuthorizationType
{
	SAPISID,
	REFRESH_TOKEN
}