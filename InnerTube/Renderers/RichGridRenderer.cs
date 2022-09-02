using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class RichGridRenderer : IRenderer
{
	public string Type => "richGridRenderer";

	public IEnumerable<IRenderer> Contents { get; }

	public RichGridRenderer(JToken renderer)
	{
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