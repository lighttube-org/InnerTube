using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class PlaylistRenderer : IRenderer
{
	public string Type => "playlistRenderer";

	public string Id { get; }
	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Dictionary<string, IEnumerable<Thumbnail>> VideoThumbnails { get; }
	public Channel Channel { get; }
	public int VideoCount { get; }
	public IEnumerable<ChildVideoRenderer> Videos { get; }

	public PlaylistRenderer(JToken renderer)
	{
		Id = renderer["playlistId"]!.ToString();
		Title = renderer["title"]!["simpleText"]!.ToString();
		VideoCount = int.Parse(renderer["videoCount"]!.ToString());

		Thumbnails =
			Utils.GetThumbnails(
				(renderer["thumbnailRenderer"] as JObject)
					?.Properties()
					.FirstOrDefault()
					?.Value
					?.GetFromJsonPath<JArray>("thumbnail.thumbnails"));
		VideoThumbnails = new Dictionary<string, IEnumerable<Thumbnail>>();
		foreach (JArray thumbnails in renderer["thumbnails"]!.ToObject<JArray>()!.Select(x =>
			         x["thumbnails"]!.ToObject<JArray>()!))
		{
			string videoId = thumbnails.First()["url"]!.ToString().Split("/vi/")[1].Split("/")[0];
			Thumbnail[] thumbs = Utils.GetThumbnails(thumbnails);
			VideoThumbnails.Add(videoId, thumbs);
		}

		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("longBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};

		Videos = RendererManager.ParseRenderers(renderer["videos"]?.ToObject<JArray>()).Cast<ChildVideoRenderer>();
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- VideoCount: {VideoCount}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine(
				$"- VideoThumbnails count: {VideoThumbnails.Keys.Count} - {string.Join(", ", VideoThumbnails.Select(x => x.Key + ": " + x.Value.Count()))}")
			.AppendLine($"- Channel: {Channel}")
			.AppendLine("- Videos:");

		foreach (ChildVideoRenderer renderer in Videos)
			sb.AppendLine("\t " + renderer);

		return sb.ToString();
	}
}