using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class WatchCardHeroVideoRendererData : IRendererData
{
	public string Title { get; set; }
	public string? VideoId { get; set; }
	public Thumbnail[][] HeroImages { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"Title: {Title}");
		sb.AppendLine($"VideoId: {VideoId}");
		sb.AppendLine($"HeroImages.Length: {HeroImages.Length}");
		return sb.ToString();
	}
}