using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeSearchResults
{
	public TypoFixer? DidYouMean { get; }
	public IReadOnlyList<IRenderer> Results { get; }
	public string? Continuation { get; }
	public string[] Refinements { get; }
	public long EstimatedResults { get; }

	public class TypoFixer
	{
		public string OriginalQuery { get; set; }
		public string CorrectedQuery { get; set; }
		public string Params { get; set; }

		public override string ToString() => $"Showing results for '{CorrectedQuery}'. Search instead for '{OriginalQuery}' [{Params}]";
	}

	public InnerTubeSearchResults(JObject json)
	{
		JArray? contents = json.GetFromJsonPath<JArray>("contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[0].itemSectionRenderer.contents");

		if (contents?[0].First?.Path.EndsWith("showingResultsForRenderer") ?? false)
		{
			JToken srfr = contents[0]["showingResultsForRenderer"]!;
			DidYouMean = new TypoFixer
			{
				OriginalQuery = srfr.GetFromJsonPath<string>("originalQuery.simpleText")!,
				CorrectedQuery = Utils.ReadRuns(srfr.GetFromJsonPath<JArray>("correctedQuery.runs")!),
				Params = srfr.GetFromJsonPath<string>("originalQueryEndpoint.searchEndpoint.params")!
			};
			contents = JArray.FromObject(contents.Skip(1));
		}

		EstimatedResults = long.Parse(json["estimatedResults"]?.ToString() ?? "0");
		Refinements = json.GetFromJsonPath<string[]>("refinements") ?? Array.Empty<string>();
		Results = Utils.ParseRenderers(contents ?? new JArray()).ToList().AsReadOnly();
		Continuation =
			json.GetFromJsonPath<string>(
				"contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[1].continuationItemRenderer.continuationEndpoint.continuationCommand.token") ??
			null;
	}
}

