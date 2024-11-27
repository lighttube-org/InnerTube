using InnerTube.Renderers;

namespace InnerTube.Models;

public class ContinuationResponse
{
	public string? ContinuationToken { get; set; }
	public RendererContainer[] Results { get; set; }
}