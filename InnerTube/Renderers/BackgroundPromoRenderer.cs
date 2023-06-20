using System.Text;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class BackgroundPromoRenderer : IRenderer
{
	public string Type => "backgroundPromoRenderer";

	public string Title { get; }
	public string BodyText { get; }
	public string? Icon { get; }

	public BackgroundPromoRenderer(JToken renderer)
	{
		BodyText = Utils.ReadText(renderer.GetFromJsonPath<JObject>("bodyText")!);
		Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("title")!);
		Icon = renderer.GetFromJsonPath<string>("icon.iconType");
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Icon: {Icon}")
			.AppendLine($"- {BodyText}")
			.ToString();
}