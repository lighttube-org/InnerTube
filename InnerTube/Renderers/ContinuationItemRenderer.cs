using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ContinuationItemRenderer : IRenderer
{
	public string Type => "continuationItemRenderer";

	public string Title { get; }
	public string Token { get; }

	public ContinuationItemRenderer(JToken renderer)
	{
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		Token = renderer.GetFromJsonPath<string>("continuationEndpoint.continuationCommand.token")!;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Token: {string.Join("", Token.Take(20))}...");

		return sb.ToString();
	}
}