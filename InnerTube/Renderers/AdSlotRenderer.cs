using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class AdSlotRenderer : IRenderer
{
	public string Type => "adSlotRenderer";

	public IRenderer? Content { get; }

	public AdSlotRenderer(JToken renderer)
	{
		JObject? obj = renderer
			.GetFromJsonPath<JObject>("fulfillmentContent.fulfilledLayout.inFeedAdLayoutRenderer.renderingContent")
			?.First?.ToObject<JObject>();
		Content = obj != null ? RendererManager.ParseRenderer(obj.First!, obj.Path.Split(".").Last())! : null;
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}]")
			.AppendLine(string.Join('\n',
				Content?.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()))
			.ToString();
}