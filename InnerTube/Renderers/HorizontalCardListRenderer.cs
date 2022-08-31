using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class HorizontalCardListRenderer : IRenderer
{
	public string Type { get; }

	public string Title { get; }
	public IEnumerable<CardRenderer> Items { get; }

	public HorizontalCardListRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();
		Title = renderer.GetFromJsonPath<string>("header.richListHeaderRenderer.title.simpleText")!;
		Items = Utils.ParseRenderers(renderer.GetFromJsonPath<JArray>("cards")!).Cast<CardRenderer>();
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