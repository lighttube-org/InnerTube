using System.Web;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class ChannelLink
{
	public string Title { get; }
	public Thumbnail Icon { get; }
	public Uri Url { get; }

	public ChannelLink(JToken jToken)
	{
		Title = jToken.GetFromJsonPath<string>("title.simpleText")!;
		Icon = Utils.GetThumbnails(jToken.GetFromJsonPath<JArray>("icon.thumbnails")!).First();
		// pretty sure the following line will fail at any second
		Url = new Uri(
			HttpUtility.UrlDecode(
				HttpUtility.ParseQueryString(jToken.GetFromJsonPath<Uri>("navigationEndpoint.urlEndpoint.url")!.Query)
					.Get("q")
			)!
		);
	}

	public override string ToString() => $"{Title} ({Url})";
}