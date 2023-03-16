using System.Web;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class EndScreenItem
{
	public EndScreenItemType Type { get; set; }
	public string Target { get; set; }
	public string Title { get; set; }
	public Thumbnail[] Image { get; set; }
	public string Metadata { get; set; }
	public string Style { get; set; }
	public long StartMs { get; set; }
	public long EndMs { get; set; }
	public float AspectRatio { get; set; }
	public float Left { get; set; }
	public float Top { get; set; }
	public float Width { get; set; }

	public EndScreenItem(JToken json)
	{
		Title = Utils.ReadText(json["title"]!.ToObject<JObject>()!);
		Image = Utils.GetThumbnails(json.GetFromJsonPath<JArray>("image.thumbnails")!);
		Metadata = Utils.ReadText(json["metadata"]!.ToObject<JObject>()!);
		Style = json["style"]!.ToString();
		StartMs = long.Parse(json["startMs"]!.ToString());
		EndMs = long.Parse(json["endMs"]!.ToString());
		AspectRatio = json["aspectRatio"]!.ToObject<float>();
		Left = json["left"]!.ToObject<float>();
		Top = json["top"]!.ToObject<float>();
		Width = json["width"]!.ToObject<float>();

		switch (json["style"]!.ToString())
		{
			case "CHANNEL":
				Type = EndScreenItemType.Channel;
				Target = json.GetFromJsonPath<string>("endpoint.browseEndpoint.browseId")!;
				break;
			case "VIDEO":
				Type = EndScreenItemType.Video;
				Target = json.GetFromJsonPath<string>("endpoint.watchEndpoint.videoId")!;
				break;
			case "WEBSITE":
				Type = EndScreenItemType.Link;
				Target = Utils.UnwrapRedirectUrl(
					json.GetFromJsonPath<string>("endpoint.urlEndpoint.url") ?? ""
				);
				break;
			case "PLAYLIST":
				Type = EndScreenItemType.Playlist;
				Target = $"{json.GetFromJsonPath<string>("endpoint.watchEndpoint.videoId")!}&list={json.GetFromJsonPath<string>("endpoint.watchEndpoint.playlistId")!}";
				break;
		}
	}
}