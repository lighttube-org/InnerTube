using System.Text;
using Google.Protobuf.WellKnownTypes;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class PlaylistRendererData : IRendererData
{
	public string PlaylistId { get; set; }
	public Thumbnail[] Thumbnails { get; set; }
	public string Title { get; set; }
	public string VideoCountText { get; set; }
	public Thumbnail[][]? SidebarThumbnails { get; set; }
	public Channel? Author { get; set; }
	public RendererContainer[]? ChildVideos { get; set; }
	public string? FirstVideoId { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{PlaylistId}] {Title}");
		sb.AppendLine("Thumbnails.Length: " + Thumbnails.Length);
		sb.AppendLine("Author: " + (Author?.ToString() ?? "<null>"));
		sb.AppendLine("VideoCountText: " + VideoCountText);
		sb.AppendLine("SidebarThumbnails: " + string.Join(", ", SidebarThumbnails?.Select(x => x.Length.ToString()) ?? ["none"]));
		sb.AppendLine("FirstVideoId: " + FirstVideoId);
		foreach (RendererContainer renderer in ChildVideos ?? [])
		{
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		}
		return sb.ToString();
	}
}