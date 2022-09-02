using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class HorizontalCardListRenderer : IRenderer
{
	public string Type => "horizontalCardListRenderer";

	public string Title { get; }
	public IEnumerable<SearchRefinementCardRenderer> Items { get; }

	public HorizontalCardListRenderer(JToken renderer)
	{
		Title = renderer.GetFromJsonPath<string>("header.richListHeaderRenderer.title.simpleText")!;
		Items = RendererManager.ParseRenderers(renderer.GetFromJsonPath<JArray>("cards")!).Cast<SearchRefinementCardRenderer>();
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}");

		foreach (IRenderer renderer in Items)
			sb.AppendLine(string.Join('\n',
				renderer.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()));

		return sb.ToString();
	}
}