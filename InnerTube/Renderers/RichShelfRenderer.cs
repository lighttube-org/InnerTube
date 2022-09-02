using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class RichShelfRenderer : IRenderer
{
	public string Type => "richShelfRenderer";

	public string Title { get; }
	public string Icon { get; }
	public IEnumerable<IRenderer> Contents { get; }

	public RichShelfRenderer(JToken renderer)
	{
		Title = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("title.runs")!);
		Icon = renderer.GetFromJsonPath<string>("icon.iconType")!;
		Contents = RendererManager.ParseRenderers(renderer.GetFromJsonPath<JArray>("contents")!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Icon: {Icon}");

		foreach (IRenderer renderer in Contents)
			sb.AppendLine(string.Join('\n',
				renderer.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()));

		return sb.ToString();
	}
}