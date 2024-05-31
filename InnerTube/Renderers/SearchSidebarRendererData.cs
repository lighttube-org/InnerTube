using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class SearchSidebarRendererData : IRendererData
{
	public string Title { get; set; }
	public string Subtitle { get; set; }
	public Thumbnail[] Avatar { get; set; }
	public Badge? TitleBadge { get; set; }
	public RendererContainer? CallToAction { get; set; }
	public RendererContainer[] Sections { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"Title: {Title}");
		sb.AppendLine($"Subtitle: {Subtitle}");
		sb.AppendLine($"Avatar.Length: {Avatar.Length}");
		sb.AppendLine($"TitleBadge: {(TitleBadge != null ? string.Join("\n\t", TitleBadge.ToString().Split("\n")) : "<null>")}");
		sb.AppendLine($"CallToAction: {(CallToAction != null ? string.Join("\n\t", CallToAction.Data.ToString().Split("\n")) : "<null>")}");
		sb.AppendLine($"Sections: ({Sections.Length})");
		foreach (RendererContainer renderer in Sections)
		{
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		}

		
		return sb.ToString();
	}
}