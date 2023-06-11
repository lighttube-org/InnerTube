using System.Text;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class MovieRenderer : IRenderer
{
	public string Type => "movieRenderer";

	public string Id { get; }
	public string Title { get; }
	public string DescriptionSnippet { get; }
	public IEnumerable<string> BottomMetadataItems { get; }
	public IEnumerable<string> TopMetadataItems { get; }
	public TimeSpan Duration { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }
	public IEnumerable<Badge> Badges { get; }

	public MovieRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("title") ?? new JObject());
		BottomMetadataItems =
			renderer.GetFromJsonPath<JArray>("bottomMetadataItems")?.Select(x => Utils.ReadText((JObject)x)) ??
			Array.Empty<string>();
		TopMetadataItems =
			renderer.GetFromJsonPath<JArray>("topMetadataItems")?.Select(x => Utils.ReadText((JObject)x)) ??
			Array.Empty<string>();
		DescriptionSnippet = Utils.ReadText(renderer.GetFromJsonPath<JObject>("descriptionSnippet") ??
		                                    new JObject(), true);
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		Channel = new Channel
		{
			Id = null,
			Title = renderer.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};
		Badges = renderer["badges"]?.ToObject<JArray>()?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ??
		         Array.Empty<Badge>();

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
			.AppendLine($"- Badges: {string.Join(" | ", Badges.Select(x => x.ToString()))}")
			.AppendLine($"- TopMetadataItems:\n\t{string.Join("\n\t", TopMetadataItems.Select(x => $"- {x}"))}")
			.AppendLine($"- BottomMetadataItems:\n\t{string.Join("\n\t", BottomMetadataItems.Select(x => $"- {x}"))}")
			.AppendLine(DescriptionSnippet);

		return sb.ToString();
	}
}