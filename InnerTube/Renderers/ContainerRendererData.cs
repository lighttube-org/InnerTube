using System.Text;

namespace InnerTube.Renderers;

public class ContainerRendererData : IRendererData
{
	public RendererContainer[] Items { get; set; }
	public string Style { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"Style: {Style}");
		foreach (RendererContainer renderer in Items)
		{
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		}
		return sb.ToString();
	}
}