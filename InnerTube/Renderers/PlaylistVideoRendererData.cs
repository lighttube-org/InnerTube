using System.Text;

namespace InnerTube.Renderers;

public class PlaylistVideoRendererData : VideoRendererData
{
	public string VideoIndexText { get; set; }	

	public override string ToString()
	{
		StringBuilder sb = new(base.ToString());
		sb.AppendLine("VideoIndexText: " + VideoIndexText);
		return sb.ToString();
	}
}