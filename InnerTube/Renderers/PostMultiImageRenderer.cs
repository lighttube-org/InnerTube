using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class PostMultiImageRenderer : IRenderer
{
	public string Type { get; }

	public IEnumerable<BackstageImageRenderer> Images { get; }

	public PostMultiImageRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();

		Images = RendererManager.ParseRenderers(renderer["images"]!.ToObject<JArray>()!).Cast<BackstageImageRenderer>();
	}

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{Type}] {Images.Count()} images");

		foreach (BackstageImageRenderer image in Images)
			sb.AppendLine(image.ToString());

		return sb.ToString();
	}
}