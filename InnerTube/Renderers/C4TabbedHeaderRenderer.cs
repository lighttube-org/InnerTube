using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class C4TabbedHeaderRenderer : IRenderer
{
	public string Type => "c4TabbedHeaderRenderer";

	public string Id { get; }
	public Thumbnail[] Avatars { get; }
	public Thumbnail[] Banner { get; }
	public Badge[] Badges { get; }
	public IEnumerable<ChannelLink> PrimaryLinks { get; }
	public IEnumerable<ChannelLink> SecondaryLinks { get; }
	public string SubscriberCountText { get; }
	public string Title { get; }

	public C4TabbedHeaderRenderer(JToken renderer)
	{
		Id = renderer.GetFromJsonPath<string>("channelId")!;
		Avatars = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("avatar.thumbnails")!);
		Banner = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("banner.thumbnails")!);
		JObject? badgeObject = renderer.GetFromJsonPath<JObject>("badges[0].metadataBadgeRenderer");
		Badges = badgeObject is not null ? new[] { new Badge(badgeObject) } : Array.Empty<Badge>();
		PrimaryLinks =
			renderer.GetFromJsonPath<JArray>("headerLinks.channelHeaderLinksRenderer.primaryLinks")!.Select(x =>
				new ChannelLink(x));
		SecondaryLinks =
			renderer.GetFromJsonPath<JArray>("headerLinks.channelHeaderLinksRenderer.secondaryLinks")!.Select(x =>
				new ChannelLink(x));
		SubscriberCountText = renderer.GetFromJsonPath<string>("subscriberCountText.simpleText")!;
		Title = renderer.GetFromJsonPath<string>("title")!;
	}

	public override string ToString() =>
		new StringBuilder().AppendLine($"[{Id}] {Title}")
			.AppendLine($"AvatarCount: {Avatars.Length}")
			.AppendLine($"BannerCount: {Banner.Length}")
			.AppendLine($"Badges: {string.Join(" | ", Badges.Select(x => x.ToString()))}")
			.AppendLine($"PrimaryLinks:\n\t{string.Join("\n\t", PrimaryLinks.Select(x => x.ToString()))}")
			.AppendLine($"SecondaryLinks:\n\t{string.Join("\n\t", SecondaryLinks.Select(x => x.ToString()))}")
			.AppendLine($"SubscriberCountText: {SubscriberCountText}")
			.ToString();
}