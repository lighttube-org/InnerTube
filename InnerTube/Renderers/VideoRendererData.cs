using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class VideoRendererData : IRendererData
{
	public string Id { get; set; }
	public string Title { get; set; }
	public Thumbnail[] Thumbnails { get; set; }
	public Channel? Author { get; set; }
	public TimeSpan Duration { get; set; }
	public string? PublishedText { get; set; }
	public string? ViewCountText { get; set; }
	public MetadataBadgeRenderer[] Badges { get; set; }
	public string? Description { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{Id}] {Title}");
		sb.AppendLine("Thumbnails.Length: " + Thumbnails.Length);
		sb.AppendLine("Author: " + Author);
		sb.AppendLine("Duration: " + Duration);
		sb.AppendLine("PublishedText: " + PublishedText);
		sb.AppendLine("ViewCountText: " + ViewCountText);
		sb.AppendLine("Badges.Length: " + Badges.Length);
		sb.AppendLine("Description: " + Description);
		return sb.ToString();
	}
}