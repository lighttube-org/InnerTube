using System.Text;

namespace InnerTube.Renderers;

public class ContainerRendererData : IRendererData
{
	public RendererContainer[] Items { get; set; }
	public string Style { get; set; }
	public string Title { get; set; }
	public string? Subtitle { get; set; }
	public string? Destination { get; set; }
	public int? ShownItemCount { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"Style: {Style}");
		sb.AppendLine($"Title: {Title}");
		sb.AppendLine($"Subtitle: {Subtitle ?? "<null>"}");
		sb.AppendLine($"Destination: {Destination ?? "<null>"}");
		sb.AppendLine($"ShownItemCount: {ShownItemCount.ToString() ?? "<null>"}");
		foreach (RendererContainer renderer in Items)
		{
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		}
		return sb.ToString();
	}
}