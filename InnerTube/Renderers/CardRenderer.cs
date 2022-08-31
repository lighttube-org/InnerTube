using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class CardRenderer : IRenderer
{
	public string Type { get; }

	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }

	public CardRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();
		Title = Utils.ReadRuns(renderer["query"]!["runs"]!.ToObject<JArray>()!);
		Thumbnails = Utils.GetThumbnails(renderer["thumbnail"]!["thumbnails"]!.ToObject<JArray>()!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Thumbnail: {Thumbnails.First().Url}");

		return sb.ToString();
	}
}