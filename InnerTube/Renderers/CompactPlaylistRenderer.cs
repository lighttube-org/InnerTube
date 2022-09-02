using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class CompactPlaylistRenderer : IRenderer
{
	public string Type => "compactPlaylistRenderer";

	public string Id { get; }
	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }
	public string VideoCountText { get; }
	public string FirstVideoId { get; }

	public CompactPlaylistRenderer(JToken renderer)
	{
		Id = renderer["playlistId"]!.ToString();
		Title = renderer["title"]!["simpleText"]!.ToString();
		VideoCountText = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("videoCountText.runs")!, false);

		Thumbnails =
			Utils.GetThumbnails(
				(renderer.GetFromJsonPath<JArray>(
					 "thumbnailRenderer.playlistVideoThumbnailRenderer.thumbnail.thumbnails") ??
				 renderer.GetFromJsonPath<JArray>(
					 "thumbnailRenderer.playlistCustomThumbnailRenderer.thumbnail.thumbnails"))!);

		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("longBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};

		FirstVideoId = renderer.GetFromJsonPath<string>("navigationEndpoint.watchEndpoint.videoId")!;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- VideoCountText: {VideoCountText}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}")
			.AppendLine($"- FirstVideoId: {FirstVideoId}");

		return sb.ToString();
	}
}