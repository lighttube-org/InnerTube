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
		string? visitorData = null, string? poToken = null, string? referer = null, string? userId = null)
	{
		Dictionary<string, object> clientContext = new();
		clientContext.Add("hl", language);
		clientContext.Add("gl", region);
		clientContext.Add("originalUrl", referer ?? "https://www.youtube.com");
		switch (requestClient)
		{
			case RequestClient.WEB:
				clientContext.Add("browserName", "Safari");
				clientContext.Add("browserV	ersion", "15.4");
				clientContext.Add("clientName", "WEB");
				clientContext.Add("clientVersion", Constants.WebClientVersion);
				clientContext.Add("deviceMake", "Apple");
				clientContext.Add("deviceModel", "");
				clientContext.Add("osName", "Macintosh");
				clientContext.Add("osVersion", "10_15_7");
				clientContext.Add("platform", "DESKTOP");
				clientContext.Add("userAgent", Constants.WebUserAgent);
				break;
			case RequestClient.ANDROID:
				clientContext.Add("clientName", "ANDROID");
				clientContext.Add("clientVersion", Constants.MobileClientVersion);
				clientContext.Add("osName", "Android");
				clientContext.Add("osVersion", "11");
				clientContext.Add("androidSdkVersion", 30);
				clientContext.Add("platform", "MOBILE");
				clientContext.Add("userAgent", Constants.AndroidUserAgent);
				break;
			case RequestClient.IOS:
				clientContext.Add("clientName", "IOS");
				clientContext.Add("clientVersion", Constants.MobileClientVersion);
				clientContext.Add("deviceMake", "Apple");
				clientContext.Add("deviceModel", "iPhone16,2");
				clientContext.Add("userAgent", Constants.IosUserAgent);
				clientContext.Add("osName", "iPhone");
				clientContext.Add("osVersion", "17.5.1.21F90");
				clientContext.Add("platform", "MOBILE");
				break;
			case RequestClient.TVAPPLE:
				clientContext.Add("clientName", "TVAPPLE");
				clientContext.Add("clientVersion", Constants.TvAppleClientVersion);
				break;
			case RequestClient.MWEB_TIER_2:
				clientContext.Add("clientName", "MWEB_TIER_2");
				clientContext.Add("clientVersion", Constants.MwebTier2ClientVersion);
				break;
			case RequestClient.TV_UNPLUGGED_CAST:
				clientContext.Add("clientName", "TV_UNPLUGGED_CAST");
				clientContext.Add("clientVersion", Constants.TvUnpluggedCastClientVersion);
				break;
			case RequestClient.TV_EMBEDDED:
				clientContext.Add("clientName", "TVHTML5_SIMPLY_EMBEDDED_PLAYER");
				clientContext.Add("clientVersion", Constants.TvEmbeddedClientVersion);
				break;
			case RequestClient.MEDIA_CONNECT_FRONTEND:
				clientContext.Add("clientName", "MEDIA_CONNECT_FRONTEND");
				clientContext.Add("clientVersion", Constants.MediaConnectFrontendClientVersion);
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

		if (poToken != null)
			AddValue("serviceIntegrityDimensions", new Dictionary<string, string>
			{
				["poToken"] = poToken	
			});

		Dictionary<string, object> context = new()
		{
			["client"] = clientContext
		};

		if (userId != null)
			context.Add("user", new Dictionary<string, object>
			{
				["lockedSafetyMode"] = false,
				["onBehalfOfUser"] = userId
			});
		else
			context.Add("user", new Dictionary<string, object>
			{
				["lockedSafetyMode"] = false
			});

		if (visitorData != null)
		{
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

	public string GetJson(RequestClient client, string language, string region, string? referer, string? visitorData, string? poToken, string? userId)
	{
		UpdateContext(client, language, region, visitorData, poToken, referer, userId);
		return JsonConvert.SerializeObject(data);
	}
}