using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;

namespace InnerTube;

public class InnerTubeSearchResults
{
	public RendererContainer[] Results { get; }
	public ShowingResultsFor QueryCorrecter { get; }
	public RendererContainer[] Chips { get; }
	public string? Continuation { get; }
	public string[] Refinements { get; }
	public long EstimatedResults { get; }

	public InnerTubeSearchResults(SearchResponse response)
	{
		RendererWrapper[] renderers = response.Contents
			.TwoColumnSearchResultsRenderer.PrimaryContents.ResultsContainer.Results
			.SelectMany(x => x.ItemSectionRenderer?.Contents ?? []).ToArray();
		Results = Utils.ConvertRenderers(renderers.Where(x =>
			x.RendererCase is not RendererWrapper.RendererOneofCase.DidYouMeanRenderer
				and not RendererWrapper.RendererOneofCase.ShowingResultsForRenderer));
		QueryCorrecter = ShowingResultsFor.GetFromRendererWrapper(renderers.FirstOrDefault());
		Chips = Utils.ConvertRenderers(response.Header.SearchHeaderRenderer.ChipBar?.ChipCloudRenderer.Chips);
		Continuation = response.Contents.TwoColumnSearchResultsRenderer.PrimaryContents
			.ResultsContainer.Results
			.LastOrDefault(x => x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)
			?.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token;
		Refinements = response.Refinements.ToArray();
		EstimatedResults = response.EstimatedResults;
	}
}
