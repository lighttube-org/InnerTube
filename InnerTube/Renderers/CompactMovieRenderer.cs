using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class CompactMovieRenderer : IRenderer
{
	public string Type => "compactMovieRenderer";

	public string Id { get; }
	public string Title { get; }
	public IEnumerable<string> TopMetadataItems { get; }
	public TimeSpan Duration { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }

	public CompactMovieRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		TopMetadataItems =
			renderer.GetFromJsonPath<JArray>("topMetadataItems")?.Select(x => Utils.ReadText((JObject)x)) ??
			Array.Empty<string>();
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("shortBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("shortBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = Array.Empty<Badge>()
		};

		Duration = Utils.ParseDuration(renderer["lengthText"]?["simpleText"]?.ToString()!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Duration: {Duration}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}")
			.AppendLine($"- TopMetadataItems:\n\t{string.Join("\n\t", TopMetadataItems.Select(x => $"- {x}"))}");

		return sb.ToString();
	}
}