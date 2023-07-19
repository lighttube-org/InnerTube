using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ShowingResultsForRenderer : IRenderer
{
	public string Type => "showingResultsFor";
	
	public string ShowingResultsFor;
	public string SearchInsteadFor;
	public string OriginalQuery;
	public string CorrectedQuery;

	public ShowingResultsForRenderer(JToken renderer)
	{
		ShowingResultsFor = Utils.ReadText(renderer.GetFromJsonPath<JObject>("showingResultsFor")!);
		SearchInsteadFor = Utils.ReadText(renderer.GetFromJsonPath<JObject>("searchInsteadFor")!);
		OriginalQuery = Utils.ReadText(renderer.GetFromJsonPath<JObject>("originalQuery")!);
		CorrectedQuery = Utils.ReadText(renderer.GetFromJsonPath<JObject>("correctedQuery")!);
	}

	public override string ToString() => $"[{Type}] {ShowingResultsFor} \"{CorrectedQuery}\"\n{SearchInsteadFor} \"{OriginalQuery}\"";
}