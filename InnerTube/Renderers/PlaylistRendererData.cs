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

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{PlaylistId}] {Title}");
		sb.AppendLine("Thumbnails.Length: " + Thumbnails.Length);
		sb.AppendLine("Author: " + (Author?.ToString() ?? "<null>"));
		sb.AppendLine("VideoCountText: " + VideoCountText);
		sb.AppendLine("SidebarThumbnails: " + string.Join(", ", SidebarThumbnails?.Select(x => x.Length.ToString()) ?? ["none"]));
		return sb.ToString();
	}
}