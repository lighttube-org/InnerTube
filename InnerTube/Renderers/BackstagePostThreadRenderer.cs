using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class BackstagePostThreadRenderer : IRenderer
{
	public string Type { get; }

	public BackstagePostRenderer Post { get; }

	public BackstagePostThreadRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();

		Post = (BackstagePostRenderer)Utils.ParseRenderer(
			renderer["post"]!["backstagePostRenderer"]!.ToObject<JObject>()!, "backstagePostRenderer")!;
	}

	public override string ToString() =>
		$"[{Type}]\n\t{string.Join('\n', Post.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>())}";
}