using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class PromotedVideoRenderer : IRenderer
{
	public string Type => "promotedVideoRenderer";

	public string Id { get; }
	public string Title { get; }
	public string Description { get; }
	public TimeSpan Duration { get; }
	public string ViewCount { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }
	public IEnumerable<Badge> Badges { get; }

	public PromotedVideoRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = renderer["title"]!["simpleText"]!.ToString();
		Description = renderer["description"]!["simpleText"]!.ToString();
		ViewCount = renderer["viewCountText"]!["simpleText"] != null
			? renderer["viewCountText"]!["simpleText"]!.ToString()
			: Utils.ReadRuns(renderer["viewCountText"]!["runs"]!.ToObject<JArray>()!);
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("longBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = Array.Empty<Badge>()
		};
		Badges = new[]
		{
			new Badge(renderer.GetFromJsonPath<JToken>("adBadge.metadataBadgeRenderer")!)
		};

		Duration = Utils.ParseDuration(renderer["lengthText"]?["simpleText"]?.ToString()!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[AD] [{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Duration: {Duration}")
			.AppendLine($"- ViewCount: {ViewCount}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}")
			.AppendLine($"- Badges: {string.Join(" | ", Badges.Select(x => x.ToString()))}")
			.AppendLine(Description);

		return sb.ToString();
	}
}