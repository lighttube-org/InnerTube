using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class PlaylistVideoRenderer : IRenderer
{
	public string Type => "playlistVideoRenderer";

	public string Id { get; }
	public string Title { get; }
	public int Index { get; }
	public bool IsPlayable { get; }
	public TimeSpan Duration { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }

	public PlaylistVideoRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("title.runs") ?? new JArray());
		Index = int.Parse(renderer.GetFromJsonPath<string>("index.simpleText")!.Replace(",", "").Replace(".", ""));
		IsPlayable = renderer.GetFromJsonPath<bool>("isPlayable");
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>(
				"shortBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("shortBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = Array.Empty<Badge>()
		};

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
			.AppendLine($"- Index: {Index}")
			.AppendLine($"- IsPlayable: {IsPlayable}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}");

		return sb.ToString();
	}
}