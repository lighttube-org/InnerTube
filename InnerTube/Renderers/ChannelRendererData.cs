using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class ChannelRendererData : IRendererData
{
	public string ChannelId { get; set; }
	public string Title { get; set; }
	public string? Handle { get; set; }
	public Thumbnail[] Avatar { get; set; }
	public string? VideoCountText { get; set; }
	public string? SubscriberCountText { get; set; }
	public Badge[] Badges { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{ChannelId}] {Title} ({Avatar.Length} avatars)");
		if (Handle != null)
			sb.AppendLine($"Handle: {Handle}");
		sb.AppendLine($"VideoCountText: {VideoCountText}");
		sb.AppendLine($"SubscriberCountText: {SubscriberCountText}");
		return sb.ToString();
	}
}