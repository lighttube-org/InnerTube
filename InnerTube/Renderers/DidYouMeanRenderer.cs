using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class DidYouMeanRenderer : IRenderer
{
	public string Type => "didYouMeanRenderer";
	
	public string CorrectedQuery;
	public string DidYouMean;
	public string SearchQuery;

	public DidYouMeanRenderer(JToken renderer)
	{
		DidYouMean = Utils.ReadText(renderer.GetFromJsonPath<JObject>("didYouMean")!);
		CorrectedQuery = Utils.ReadText(renderer.GetFromJsonPath<JObject>("correctedQuery")!);
		SearchQuery = renderer.GetFromJsonPath<string>("correctedQueryEndpoint.searchEndpoint.query")!;
	}

	public override string ToString() => $"[{Type}] {DidYouMean} {CorrectedQuery} [{SearchQuery}]";
}