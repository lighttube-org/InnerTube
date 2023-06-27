using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ShelfRenderer : IRenderer
{
	public string Type => "shelfRenderer";

	public string Title { get; }
	public string? Subtitle { get; }
	public string? Destination { get; }
	public int CollapsedItemCount { get; }
	public ShelfDirection Direction { get; }
	public IEnumerable<IRenderer> Items { get; }

	public ShelfRenderer(JToken renderer)
	{
		Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("title")!);
		Subtitle = renderer.GetFromJsonPath<JObject>("subtitle")?["simpleText"]?.ToString(); // dont ask why
		if (renderer.GetFromJsonPath<JArray?>("title.runs") != null)
		{
			Destination =
				renderer.GetFromJsonPath<string>(
					"title.runs[0].navigationEndpoint.commandMetadata.webCommandMetadata.url");
		}

		CollapsedItemCount = renderer.GetFromJsonPath<int>("content.verticalListRenderer.collapsedItemCount")!;
		Direction = renderer.GetFromJsonPath<JArray>("content.verticalListRenderer.items") != null
			? ShelfDirection.Vertical
			: renderer.GetFromJsonPath<JArray>("content.horizontalListRenderer.items") != null
				? ShelfDirection.Horizontal
				: renderer.GetFromJsonPath<JArray>("content.gridRenderer.items") != null
					? ShelfDirection.Grid
					: ShelfDirection.None;
		Items = RendererManager.ParseRenderers(Direction switch
		{
			ShelfDirection.Horizontal => renderer.GetFromJsonPath<JArray>("content.horizontalListRenderer.items")!,
			ShelfDirection.Vertical => renderer.GetFromJsonPath<JArray>("content.verticalListRenderer.items")!,
			ShelfDirection.Grid => renderer.GetFromJsonPath<JArray>("content.gridRenderer.items")!,
			//TODO this happens in FEexplore
			var _ => new JArray()
		});
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Subtitle: {Subtitle}")
			.AppendLine($"- Destination: {Destination}")
			.AppendLine($"- CollapsedItemCount: {CollapsedItemCount}");

		foreach (IRenderer renderer in Items)
			sb.AppendLine(string.Join('\n',
				renderer.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()));

		return sb.ToString();
	}
}