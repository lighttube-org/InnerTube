using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ItemSectionRenderer : IRenderer
{
	public string Type { get; }

	public IEnumerable<IRenderer> Contents;

	public ItemSectionRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();

		Contents = RendererManager.ParseRenderers(renderer["contents"]!.ToObject<JArray>()!);
	}

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{Type}]");

		foreach (IRenderer renderer in Contents)
			sb.AppendLine("->\t" + string.Join("\n\t",
				(renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		return sb.ToString();
	}
}