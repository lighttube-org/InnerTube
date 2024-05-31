using System.Text;

namespace InnerTube.Renderers;

public class ChipRendererData : IRendererData
{
	public string Title { get; set; }
	public string? ContinuationToken { get; set; }
	public string? Params { get; set; }
	public bool IsSelected { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine("Title: " + Title);
		sb.AppendLine("ContinuationToken: " + (ContinuationToken != null ? ContinuationToken[..10] + "..." : "<null>"));
		sb.AppendLine("Params: " + (Params != null ? Params.Take(10) + "..." : "<null>"));
		sb.AppendLine("IsSelected: " + IsSelected);
		return sb.ToString();
	}
}