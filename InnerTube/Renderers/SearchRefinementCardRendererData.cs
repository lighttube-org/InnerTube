using System.Text;

namespace InnerTube.Renderers;

public class SearchRefinementCardRendererData : IRendererData
{
	public string Thumbnail { get; set; }
	public string Title { get; set; }
	public string? PlaylistId { get; set; }
	public string? SearchQuery { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{Thumbnail}] {Title}");
		sb.AppendLine("PlaylistId: " + PlaylistId);
		sb.AppendLine("SearchQuery: " + SearchQuery);
		return sb.ToString();
	}
}