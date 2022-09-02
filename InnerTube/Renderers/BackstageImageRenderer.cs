using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class BackstageImageRenderer : IRenderer
{
	public string Type => "backstageImageRenderer";

	public IEnumerable<Thumbnail> Images { get; }

	public BackstageImageRenderer(JToken renderer)
	{
		Images = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("image.thumbnails")!);
	}

	public override string ToString() => $"[{Type}] {Images.Last()}";
}