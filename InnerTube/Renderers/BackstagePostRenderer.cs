using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class BackstagePostRenderer : IRenderer
{
	public string Type => "backstagePostRenderer";

	public string Id { get; set; }
	public Channel Author { get; }
	public IRenderer? Attachment { get; }
	public string Content { get; }
	public string Published { get; }
	public string LikeCount { get; }

	public BackstagePostRenderer(JToken renderer)
	{
		Id = renderer.GetFromJsonPath<string>("postId")!;
		Author = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("authorEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("authorText.runs[0].text")!,
			Avatar = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("authorThumbnail.thumbnails") ?? new JArray())
				.LastOrDefault()?.Url,
			Subscribers = null,
			Badges = Array.Empty<Badge>()
		};
		Content = Utils.ReadText(renderer.GetFromJsonPath<JObject>("contentText")!);
		JToken? attachmentObject = renderer.GetFromJsonPath<JObject>("backstageAttachment")?.First;
		Attachment =
			RendererManager.ParseRenderer(attachmentObject?.First, attachmentObject?.Path.Split(".").Last() ?? "");
		Published = Utils.ReadText(renderer.GetFromJsonPath<JObject>("publishedTimeText")!);
		LikeCount = renderer.GetFromJsonPath<string>("voteCount.simpleText")!;
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}] ({Id})")
			.AppendLine($"Author: {Author}")
			.AppendLine($"Published: {Published}")
			.AppendLine($"Content: {Content}")
			.AppendLine($"Attachment: {Attachment?.ToString() ?? "<no attachment>"}")
			.AppendLine($"LikeCount: {LikeCount}")
			.ToString();
}