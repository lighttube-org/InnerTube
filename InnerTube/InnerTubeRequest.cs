using Newtonsoft.Json;

namespace InnerTube;

internal class InnerTubeRequest
{
	private Dictionary<string, object> data = new();

	public InnerTubeRequest AddValue(string key, object value)
	{
		data[key] = value;
		return this;
	}

	private void UpdateContext(RequestClient requestClient, string language = "en", string region = "US",
		string? visitorData = null, string? referer = null)
	{
		Dictionary<string, object> clientContext = new();
		clientContext.Add("hl", language);
		clientContext.Add("gl", region);
		clientContext.Add("originalUrl", referer ?? "https://www.youtube.com");
		switch (requestClient)
		{
			case RequestClient.WEB:
				clientContext.Add("browserName", "Safari");
				clientContext.Add("browserVersion", "15.4");
				clientContext.Add("clientName", "WEB");
				clientContext.Add("clientVersion", Constants.WebClientVersion);
				clientContext.Add("deviceMake", "Apple");
				clientContext.Add("deviceModel", "");
				clientContext.Add("osName", "Macintosh");
				clientContext.Add("osVersion", "10_15_7");
				clientContext.Add("platform", "DESKTOP");
				clientContext.Add("userAgent",
					"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.4 Safari/605.1.15,gzip(gfe)");
				break;
			case RequestClient.ANDROID:
				clientContext.Add("clientName", "ANDROID");
				clientContext.Add("clientVersion", Constants.MobileClientVersion);
				clientContext.Add("osName", "Android");
				clientContext.Add("osVersion", "11");
				clientContext.Add("androidSdkVersion", 30);
				clientContext.Add("platform", "MOBILE");
				clientContext.Add("userAgent", "com.google.android.youtube/19.09.4 (Linux; U; Android 11) gzip");
				break;
			case RequestClient.IOS:
				clientContext.Add("clientName", "IOS");
				clientContext.Add("clientVersion", Constants.MobileClientVersion);
				clientContext.Add("deviceMake", "Apple");
				clientContext.Add("deviceModel", "iPhone14,5");
				clientContext.Add("osName", "iOS");
				clientContext.Add("osVersion", "15.6.0.19G71");
				clientContext.Add("platform", "MOBILE");
				break;
			case RequestClient.TV_EMBEDDED:
				clientContext.Add("clientName", "TVHTML5_SIMPLY_EMBEDDED_PLAYER");
				clientContext.Add("clientVersion", Constants.TvEmbeddedClientVersion);
				break;
		}

		if (visitorData != null)
		{
			clientContext.Add("visitorData", visitorData);
			clientContext.Add("screenPixelDensity", 1);
			clientContext.Add("clientFormFactor", "UNKNOWN_FORM_FACTOR");
			clientContext.Add("screenDensityFloat", 1.25f);
			clientContext.Add("timeZone", "Etc/UTC");
			clientContext.Add("screenWidthPoints", 1920);
			clientContext.Add("screenHeightPoints", 1080);
			clientContext.Add("utcOffsetMinutes", 0);
			clientContext.Add("userInterfaceTheme", "USER_INTERFACE_THEME_DARK");
			clientContext.Add("connectionType", "CONN_CELLULAR_4G");
			clientContext.Add("mainAppWebInfo", new Dictionary<string, object>
			{
				["graftUrl"] = referer ?? "https://www.youtube.com",
				["webDisplayMode"] = "WEB_DISPLAY_MODE_BROWSER",
				["isWebNativeShareAvailable"] = true
			});
			clientContext.Add("playerType", "UNIPLAYER");
			clientContext.Add("tvAppInfo", new Dictionary<string, object>
			{
				["livingRoomAppMode"] = "LIVING_ROOM_APP_MODE_UNSPECIFIED"
			});
			clientContext.Add("clientScreen", "WATCH_FULL_SCREEN");
		}

		Dictionary<string, object> context = new()
		{
			["client"] = clientContext
		};

		if (visitorData != null)
		{
			context.Add("user", new Dictionary<string, object>
			{
				["lockedSafetyMode"] = false
			});
			context.Add("request", new Dictionary<string, object>
			{
				["useSsl"] = true,
				["internalExperimentFlags"] = Array.Empty<string>(),
				["consistencyTokenJars"] = Array.Empty<string>()
			});
		}

		if (requestClient == RequestClient.TV_EMBEDDED)
			context.Add("thirdParty", new Dictionary<string, string>
			{
				["embedUrl"] = referer ?? "https://www.youtube.com"
			});

		AddValue("context", context);
	}

	public string GetJson(RequestClient client, string language, string region, string? visitorData, string? referer)
	{
		UpdateContext(client, language, region, visitorData, referer);
		return JsonConvert.SerializeObject(data);
	}
}