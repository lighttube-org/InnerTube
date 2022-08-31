using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class BackstageImageRenderer : IRenderer
{
	public string Type { get; }

	public IEnumerable<Thumbnail> Images { get; }

	public BackstageImageRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();

		Images = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("image.thumbnails")!);
	}

	public override string ToString() => $"[{Type}] {Images.Last()}";
}