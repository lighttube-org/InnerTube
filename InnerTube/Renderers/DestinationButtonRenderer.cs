using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class DestinationButtonRenderer : IRenderer
{
	public string Type => "destinationButtonRenderer";

	public Thumbnail[] Icons { get; }
	public string Label { get; }
	public string BrowseId { get; }
	public string BrowseParams { get; }

	public DestinationButtonRenderer(JToken renderer)
	{
		Label = renderer.GetFromJsonPath<string>("label.simpleText")!;
		BrowseId = renderer.GetFromJsonPath<string>("onTap.browseEndpoint.browseId")!;
		BrowseParams = renderer.GetFromJsonPath<string>("onTap.browseEndpoint.params")!;
		Icons = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("iconImage.thumbnails")!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Label}")
			.AppendLine($"- IconCount: {Icons.Length}")
			.AppendLine($"- BrowseId: {BrowseId}")
			.AppendLine($"- BrowseParams: {BrowseParams}");

		return sb.ToString();
	}
}