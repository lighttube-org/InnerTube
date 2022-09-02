using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class SearchRefinementCardRenderer : IRenderer
{
	public string Type => "searchRefinementCardRenderer";

	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }

	public SearchRefinementCardRenderer(JToken renderer)
	{
		Title = Utils.ReadText(renderer["query"]!.ToObject<JObject>()!);
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