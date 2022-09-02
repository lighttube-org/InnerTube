using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ReelShelfRenderer : IRenderer
{
	public string Type => "reelShelfRenderer";

	public string Title { get; }
	public IEnumerable<ReelItemRenderer> Items { get; }

	public ReelShelfRenderer(JToken renderer)
	{
		Title = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("title.runs")!);
		Items = RendererManager.ParseRenderers(renderer.GetFromJsonPath<JArray>("items")!).Cast<ReelItemRenderer>();
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}");

		foreach (ReelItemRenderer renderer in Items)
			sb.AppendLine(string.Join('\n',
				renderer.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()));

		return sb.ToString();
	}
}