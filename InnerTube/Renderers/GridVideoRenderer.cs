using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class GridVideoRenderer : IRenderer
{
	public string Type => "gridVideoRenderer";

	public string Id { get; }
	public string Title { get; }
	public TimeSpan Duration { get; }
	public string? Published { get; }
	public string ViewCount { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }
	public IEnumerable<Badge> Badges { get; }

	public GridVideoRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = renderer.GetFromJsonPath<string>("title.simpleText") ??
		        Utils.ReadText(renderer.GetFromJsonPath<JObject>("title.runs") ?? new JObject());
		Published = renderer["publishedTimeText"]?["simpleText"]!.ToString();
		ViewCount = Utils.ReadText(renderer["viewCountText"]?.ToObject<JObject>()!);
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>(
				"shortBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("shortBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};
		Badges = renderer["badges"]?.ToObject<JArray>()?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ??
		         Array.Empty<Badge>();

		Duration = Utils.ParseDuration(
			renderer.GetFromJsonPath<string>(
				"thumbnailOverlays[0].thumbnailOverlayTimeStatusRenderer.text.simpleText")!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Duration: {Duration}")
			.AppendLine($"- Published: {Published ?? "Live now"}")
			.AppendLine($"- ViewCount: {ViewCount}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}")
			.AppendLine($"- Badges: {string.Join(" | ", Badges.Select(x => x.ToString()))}");

		return sb.ToString();
	}
}