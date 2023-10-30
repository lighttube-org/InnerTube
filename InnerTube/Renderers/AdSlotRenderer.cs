using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class AdSlotRenderer : IRenderer
{
	public string Type => "adSlotRenderer";

	public IRenderer? Content { get; }

	public AdSlotRenderer(JToken renderer)
	{
		JProperty? obj = renderer
			.GetFromJsonPath<JObject>("fulfillmentContent.fulfilledLayout.inFeedAdLayoutRenderer.renderingContent")
			?.First?.ToObject<JProperty>();
		Content = obj != null ? RendererManager.ParseRenderer(obj.Value.ToObject<JObject>(), obj.Name)! : null;
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}]")
			.AppendLine(string.Join('\n',
				Content?.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()))
			.ToString();
}