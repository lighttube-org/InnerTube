using System.Text;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class CommunityPostImageRendererData : IRendererData
{
	public Thumbnail[][] Images { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine("Images.Length: " + Images.Length);
		foreach (Thumbnail[] image in Images) 
			sb.AppendLine("-> " + image.Last().Url);
		return sb.ToString();
	}
}