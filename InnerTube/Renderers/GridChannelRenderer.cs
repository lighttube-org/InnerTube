using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class GridChannelRenderer : IRenderer
{
	public string Type => "gridChannelRenderer";

	public string Id { get; }
	public string Title { get; }
	public string CustomUrl { get; }
	public IEnumerable<Thumbnail> Avatars { get; }
	public string VideoCountText { get; }
	public string SubscriberCountText { get; }
	public IReadOnlyList<Badge> Badges { get; }

	public GridChannelRenderer(JToken renderer)
	{
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		Id = renderer.GetFromJsonPath<string>("channelId")!;
		CustomUrl = renderer.GetFromJsonPath<string>("navigationEndpoint.browseEndpoint.canonicalBaseUrl")!;
		Avatars = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails")!);
		VideoCountText = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("videoCountText.runs")!);
		SubscriberCountText = renderer.GetFromJsonPath<string>("subscriberCountText.simpleText")!;
		Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")?.Select(x => new Badge(x["metadataBadgeRenderer"]!))
			.ToList().AsReadOnly() ?? new List<Badge>().AsReadOnly();
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Avatars.Length: {Avatars.Count()}")
			.AppendLine($"- CustomUrl: {CustomUrl ?? "no custom ID"}")
			.AppendLine($"- VideoCountText: {VideoCountText}")
			.AppendLine($"- SubscriberCountText: {SubscriberCountText}")
			.AppendLine($"- Badges:\n{string.Join('\n', Badges.Select(x => "\t- " + x))}");

		return sb.ToString();
	}
}