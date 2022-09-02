using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class SectionListRenderer : IRenderer
{
	public string Type => "sectionListRenderer";
	
	public string TargetId { get; }
	public IEnumerable<IRenderer> Contents { get; }

	public SectionListRenderer(JToken renderer)
	{
		TargetId = renderer.GetFromJsonPath<string>("targetId")!;
		Contents = RendererManager.ParseRenderers(renderer.GetFromJsonPath<JArray>("contents")!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {TargetId}");

		foreach (IRenderer renderer in Contents)
			sb.AppendLine(string.Join('\n',
				renderer.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()));

		return sb.ToString();
	}
}