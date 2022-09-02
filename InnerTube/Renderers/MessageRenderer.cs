using System.Text;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class MessageRenderer : IRenderer
{
	public string Type => "messageRenderer";
	
	public string Message { get; }

	public MessageRenderer(JToken renderer)
	{
		Message = Utils.ReadText(renderer.GetFromJsonPath<JObject>("text")!);
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}]")
			.AppendLine($"- {Message}")
			.ToString();
}
