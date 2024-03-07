using Newtonsoft.Json;

namespace InnerTube;

internal class InnerTubeRequest
{
	private Dictionary<string, object> data = new();

	public InnerTubeRequest AddValue(string key, object value)
	{
		if (data.ContainsKey(key)) data[key] = value;
		else data.Add(key, value);
		return this;
	}

	private void UpdateContext(RequestClient requestClient, string language = "en", string region = "US")
	{
		Dictionary<string, object> clientContext = new();
		clientContext.Add("hl", language);
		clientContext.Add("gl", region);
		switch (requestClient)
		{
			case RequestClient.WEB:
				clientContext.Add("browserName", "Safari");
				clientContext.Add("browserVersion", "15.4");
				clientContext.Add("clientName", "WEB");
				clientContext.Add("clientVersion", "2.20240304.00.00");
				clientContext.Add("deviceMake", "Apple");
				clientContext.Add("osName", "Macintosh");
				clientContext.Add("osVersion", "10_15_7");
				clientContext.Add("platform", "DESKTOP");
				clientContext.Add("originalUrl", "https://www.youtube.com");
				clientContext.Add("userAgent",
					"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.4 Safari/605.1.15,gzip(gfe)");
				break;
			case RequestClient.ANDROID:
				clientContext.Add("clientName", "ANDROID");
				clientContext.Add("clientVersion", "19.09.4");
				clientContext.Add("osName", "Android");
				clientContext.Add("osVersion", "11");
				clientContext.Add("androidSdkVersion", 30);
				clientContext.Add("platform", "MOBILE");
				clientContext.Add("userAgent", "com.google.android.youtube/19.09.4 (Linux; U; Android 11) gzip");
				break;
			case RequestClient.IOS:
				clientContext.Add("clientName", "IOS");
				clientContext.Add("clientVersion", "19.09.4");
				clientContext.Add("deviceMake", "Apple");
				clientContext.Add("deviceModel", "iPhone14,5");
				clientContext.Add("osName", "iOS");
				clientContext.Add("osVersion", "15.6.0.19G71");
				clientContext.Add("platform", "MOBILE");
				break;
		}

		AddValue("context", new Dictionary<string, object>
		{
			["client"] = clientContext
		});
	}

	public string GetJson(RequestClient client, string language, string region)
	{
		UpdateContext(client, language, region);
		return JsonConvert.SerializeObject(data);
	}
}