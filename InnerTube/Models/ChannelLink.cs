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
		Icon = Utils.GetThumbnails(jToken.GetFromJsonPath<JArray>("icon.thumbnails")!)[0];
		// ~~pretty sure the following line will fail at any second~~
		// no it won't
		Url = new Uri(
			Utils.UnwrapRedirectUrl(jToken.GetFromJsonPath<string>("navigationEndpoint.urlEndpoint.url")!)
		);
	}

	public override string ToString() => $"{Title} ({Url})";
}