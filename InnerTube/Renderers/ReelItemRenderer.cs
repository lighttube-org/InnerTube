using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ReelItemRenderer : IRenderer
{
	public string Type => "reelItemRenderer";

	public string Id { get; }
	public string Title { get; }
	public string? ViewCount { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }

	public ReelItemRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = renderer.GetFromJsonPath<string>("headline.simpleText")!;
		ViewCount = renderer["viewCountText"]?["simpleText"]?.ToString();
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- ViewCount: {ViewCount}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}");

		return sb.ToString();
	}
}