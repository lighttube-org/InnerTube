using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class PlaylistPanelVideoRenderer : IRenderer
{
	public string Type => "playlistPanelVideoRenderer";

	public string Id { get; }
	public string PlaylistParams { get; }
	public string Title { get; }
	public string IndexText { get; }
	public bool IsSelected { get; }
	public TimeSpan Duration { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }

	public PlaylistPanelVideoRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		PlaylistParams = renderer.GetFromJsonPath<string>("navigationEndpoint.watchEndpoint.params")!;
		Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("title") ?? new JObject());
		IndexText = renderer.GetFromJsonPath<string>("indexText.simpleText")!;
		IsSelected = renderer.GetFromJsonPath<bool>("selected");
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
				"lengthText.simpleText")!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {IndexText} - {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- PlaylistParams: {PlaylistParams}")
			.AppendLine($"- Duration: {Duration}")
			.AppendLine($"- IsSelected: {IsSelected}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}");

		return sb.ToString();
	}
}