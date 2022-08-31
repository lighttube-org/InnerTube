using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ShelfRenderer : IRenderer
{
	public string Type { get; }

	public string Title { get; }
	public int CollapsedItemCount { get; }
	public ShelfDirection Direction { get; }
	public IEnumerable<IRenderer> Items { get; }


	public ShelfRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		CollapsedItemCount = renderer.GetFromJsonPath<int>("content.verticalListRenderer.collapsedItemCount")!;
		Direction = renderer.GetFromJsonPath<JArray>("content.verticalListRenderer.items") != null
			? ShelfDirection.Vertical
			: renderer.GetFromJsonPath<JArray>("content.horizontalListRenderer.items") != null
				? ShelfDirection.Horizontal
				: ShelfDirection.None;
		Items = Utils.ParseRenderers(Direction switch
		{
			ShelfDirection.Horizontal => renderer.GetFromJsonPath<JArray>("content.horizontalListRenderer.items")!,
			ShelfDirection.Vertical => renderer.GetFromJsonPath<JArray>("content.verticalListRenderer.items")!,
			//TODO this happens in FEexplore
			var _ => new JArray()
		});
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- CollapsedItemCount: {CollapsedItemCount}");

		foreach (IRenderer renderer in Items)
			sb.AppendLine(string.Join('\n',
				renderer.ToString()?.Split('\n').Select(x => $"\t{x}") ?? Array.Empty<string>()));

		return sb.ToString();
	}
}