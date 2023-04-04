using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ChannelRenderer : IRenderer
{
	public string Type => "channelRenderer";

	public string Id { get; }
	public string Title { get; }
	public IEnumerable<Thumbnail> Avatars { get; }
	public string? CustomUrl { get; }
	public string Description { get; }
	public string UserHandle { get; }
	public string SubscriberCountText { get; }
	public IReadOnlyList<Badge> Badges { get; }

	public ChannelRenderer(JToken renderer)
	{
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		Id = renderer.GetFromJsonPath<string>("channelId")!;
		CustomUrl = renderer.GetFromJsonPath<string>("navigationEndpoint.browseEndpoint.canonicalBaseUrl");
		Avatars = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails")!);
		Description = Utils.ReadText(renderer.GetFromJsonPath<JObject>("descriptionSnippet") ?? new JObject(), true);
		SubscriberCountText = Utils.ReadText(renderer.GetFromJsonPath<JObject>("videoCountText")!);
		UserHandle = renderer.GetFromJsonPath<string>("subscriberCountText.simpleText")!;
		Badges = (renderer.GetFromJsonPath<JArray>("ownerBadges")?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>())
			.ToList().AsReadOnly();
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Avatars.Length: {Avatars.Count()}")
			.AppendLine($"- CustomUrl: {CustomUrl ?? "no custom ID"}")
			.AppendLine($"- UserHandle: {UserHandle}")
			.AppendLine($"- SubscriberCountText: {SubscriberCountText}")
			.AppendLine($"- Badges:\n{string.Join('\n', Badges.Select(x => "\t- " + x))}")
			.AppendLine(Description);

		return sb.ToString();
	}
}