using System.Text;

namespace InnerTube.Renderers;

public class RecognitionShelfRendererData : IRendererData
{
	public string Title { get; set; }
	public string Subtitle { get; set; }
	public string[] Avatars { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine("Title: " + Title);
		sb.AppendLine("Subtitle: " + Subtitle);
		sb.AppendLine($"Avatars: ({Avatars.Length})");
		foreach (string avatar in Avatars) sb.AppendLine("- " + avatar);
		return sb.ToString();
	}
}