using InnerTube.Renderers;

namespace InnerTube.Models;

public class SearchContinuationResponse : ContinuationResponse
{
	public RendererContainer[]? Chips { get; set; }
}