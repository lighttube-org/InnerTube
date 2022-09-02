using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class TabRenderer : IRenderer
{
	public string Type => "tabRenderer";
	
	public string TabId { get; }
	public bool Selected { get; }
	public IRenderer Content { get; }

	public TabRenderer(JToken renderer)
	{
		Content = RendererManager.ParseRenderer(renderer["content"]!.First!.First!, renderer["content"]!.First!.Path.Split(".").Last())!;
		TabId = renderer.GetFromJsonPath<string>("tabIdentifier")!;
		Selected = renderer.GetFromJsonPath<bool>("selected")!;
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}] {TabId} ({(Selected ? "Selected" : "Not Selected")})")
			.AppendLine(string.Join('\n',
				Content.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()))
			.ToString();
}