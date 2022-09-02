using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class CompactVideoRenderer : IRenderer
{
	public string Type => "compactVideoRenderer";

	public string Id { get; }
	public string Title { get; }
	public TimeSpan Duration { get; }
	public string? Published { get; }
	public string ViewCount { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }

	public CompactVideoRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		Published = renderer["publishedTimeText"]?["simpleText"]!.ToString();
		ViewCount = renderer["viewCountText"]!["simpleText"] != null
			? renderer["viewCountText"]!["simpleText"]!.ToString()
			: Utils.ReadRuns(renderer["viewCountText"]!["runs"]!.ToObject<JArray>()!);
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("longBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>(
				                             "channelThumbnailSupportedRenderers.channelThumbnailWithLinkRenderer.thumbnail.thumbnails") ??
			                             new JArray()).LastOrDefault()?.Url,
			Subscribers = null,
			Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};

		Duration = Utils.ParseDuration(renderer["lengthText"]?["simpleText"]?.ToString()!);
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
			.AppendLine($"- Channel: {Channel}");

		return sb.ToString();
	}
}