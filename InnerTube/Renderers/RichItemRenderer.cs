using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class RichItemRenderer : IRenderer
{
	public string Type { get; }

	public IRenderer Content;

	public RichItemRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();

		Content = Utils.ParseRenderer(renderer["content"]!.First!.First!, renderer["content"]!.First!.Path.Split(".").Last())!;
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}]")
			.AppendLine(string.Join('\n',
				Content.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()))
			.ToString();
}