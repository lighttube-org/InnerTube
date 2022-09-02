using System.Text;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class RadioRenderer : IRenderer
{
	public string Type => "radioRenderer";
	
	public string Id { get; }
	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }
	public string FirstVideoId { get; }

	public RadioRenderer(JToken renderer)
	{
		Id = renderer["playlistId"]!.ToString();
		Title = Utils.ReadText(renderer["title"]!.ToObject<JObject>()!);
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails")!);
		
		Channel = new Channel
		{
			Id = null,
			Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("longBylineText")!),
			Avatar = null,
			Subscribers = null,
			Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};

		FirstVideoId = renderer.GetFromJsonPath<string>("navigationEndpoint.watchEndpoint.videoId")!;
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}")
			.AppendLine($"- FirstVideoId: {FirstVideoId}")
			.ToString();
}