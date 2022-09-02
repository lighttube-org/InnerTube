using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class DestinationShelfRenderer : IRenderer
{
	public string Type => "destinationShelfRenderer";

	public IEnumerable<DestinationButtonRenderer> DestinationButtons { get; }

	public DestinationShelfRenderer(JToken renderer)
	{
		DestinationButtons = RendererManager
			.ParseRenderers(renderer.GetFromJsonPath<JArray>("destinationButtons")!)
			.Cast<DestinationButtonRenderer>();
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}]");

		foreach (DestinationButtonRenderer renderer in DestinationButtons)
			sb.AppendLine(string.Join('\n',
				renderer.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()));

		return sb.ToString();
	}
}