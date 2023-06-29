using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeSearchResults
{
	public IReadOnlyList<IRenderer> Results { get; }
	public string? Continuation { get; }
	public string[] Refinements { get; }
	public long EstimatedResults { get; }

	public InnerTubeSearchResults(JObject json)
	{
		JArray? contents = json.GetFromJsonPath<JArray>(
			"contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[0].itemSectionRenderer.contents");

		EstimatedResults = long.Parse(json["estimatedResults"]?.ToString() ?? "0");
		Refinements = json.GetFromJsonPath<string[]>("refinements") ?? Array.Empty<string>();
		Results = RendererManager.ParseRenderers(contents ?? new JArray()).ToList().AsReadOnly();
		Continuation =
			json.GetFromJsonPath<string>(
				"contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[1].continuationItemRenderer.continuationEndpoint.continuationCommand.token") ??
			null;
	}
}