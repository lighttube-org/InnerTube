using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class PlaylistSidebar
{
	public string Title { get; }
	public Thumbnail[] Thumbnails { get; }
	public string VideoCountText { get; }
	public string ViewCountText { get; }
	public string LastUpdated { get; }
	public string Description { get; }
	public Channel Channel { get; }
	public IEnumerable<Badge> Badges { get; }

	public PlaylistSidebar(JObject renderer)
	{
		JObject primary = renderer.GetFromJsonPath<JObject>("items[0].playlistSidebarPrimaryInfoRenderer")!;
		Title = Utils.ReadText(primary.GetFromJsonPath<JObject>("title")!);
		Thumbnails =
			Utils.GetThumbnails(
				primary.GetFromJsonPath<JArray>(
					"thumbnailRenderer.playlistVideoThumbnailRenderer.thumbnail.thumbnails")!);
		VideoCountText = Utils.ReadText(primary.GetFromJsonPath<JObject>("stats[0]")!);
		ViewCountText = primary.GetFromJsonPath<string>("stats[1].simpleText")!;
		LastUpdated = Utils.ReadText(primary.GetFromJsonPath<JObject>("stats[2]")!);
		Description = primary.GetFromJsonPath<string>("description.simpleText")!;
		Badges = primary["badges"]?.ToObject<JArray>()?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ??
		         Array.Empty<Badge>();

		JObject secondary = renderer.GetFromJsonPath<JObject>("items[1].playlistSidebarSecondaryInfoRenderer")!;
		Channel = new Channel
		{
			Id = secondary.GetFromJsonPath<string>(
				"videoOwner.videoOwnerRenderer.navigationEndpoint.browseEndpoint.browseId"),
			Title = Utils.ReadText(secondary.GetFromJsonPath<JObject>("videoOwner.videoOwnerRenderer.title")!),
			Avatar = Utils
				.GetThumbnails(
					secondary.GetFromJsonPath<JArray>("videoOwner.videoOwnerRenderer.thumbnail.thumbnails") ??
					new JArray()).FirstOrDefault()?.Url,
			Subscribers = null,
			Badges = Array.Empty<Badge>()
		};
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine(Title)
			.AppendLine($"{VideoCountText} - {ViewCountText} - {LastUpdated} - {Thumbnails.Length} thumbnails")
			.AppendLine(string.Join(" | ", Badges.Select(x => x.ToString())))
			.AppendLine(Description)
			.AppendLine(Channel.ToString())
			.ToString();
}