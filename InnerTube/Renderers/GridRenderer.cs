using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class GridRenderer : IRenderer
{
	public string Type { get; }

	public IEnumerable<IRenderer> Items;

	public GridRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();

		Items = Utils.ParseRenderers(renderer["items"]!.ToObject<JArray>()!);
	}

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{Type}]");

		foreach (IRenderer renderer in Items)
			sb.AppendLine("->\t" + string.Join("\n\t",
				(renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		return sb.ToString();
	}
}