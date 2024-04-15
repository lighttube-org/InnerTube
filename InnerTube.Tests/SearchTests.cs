using System.Text;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;

namespace InnerTube.Tests;

public class SearchTests
{
	private InnerTube _innerTube;

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[TestCase("big buck bunny", null, TestName = "Just a normal search")]
	[TestCase("big bcuk bunny", null, TestName = "Search with a typo")]
	[TestCase("big bcuk bunny", "exact", TestName = "Force to search with the typo")]
	[TestCase("technoblade skyblock", "playlist", TestName = "Used to get playlistRenderer")]
	[TestCase("lofi radio", null, TestName = "Used to get live videos")]
	[TestCase("technoblade", null, TestName = "didYouMeanRenderer & channelRenderer")]
	[TestCase("O'zbekcha Kuylar 2020, Vol. 2", null, TestName = "epic broken playlist")]
	[TestCase("cars 2", "movie", TestName = "movieRenderer")]
	[TestCase("", "exact", TestName = "backgroundPromoRenderer")]
	[TestCase("vpn", null, TestName = "adSlotRenderer")]
	public async Task Search(string query, string paramArgs)
	{
		SearchParams? param = paramArgs switch
		{
			"playlist" => new SearchParams { Filters = new SearchFilters { Type = SearchFilters.Types.ItemType.Playlist } },
			"movie" => new SearchParams { Filters = new SearchFilters { Type = SearchFilters.Types.ItemType.Movie } },
			"exact" => new SearchParams { QueryFlags = new QueryFlags { ExactSearch = true } },
			_ => null
		};
		SearchResponse results = await _innerTube.SearchAsync(query, param);
		StringBuilder sb = new();
		sb.AppendLine("\n== METADATA");
		sb.AppendLine($"EstimatedResults: {results.EstimatedResults}");
		sb.AppendLine($"Chips: {string.Join(", ", results.Header.SearchHeaderRenderer.ChipBar?.ChipCloudRenderer?.Chips?.Select(x => Utils.ReadRuns(x.ChipCloudChipRenderer.Text)) ?? [])}");
		sb.AppendLine("Refinements:");
		foreach (string resultsRefinement in results.Refinements) 
			sb.AppendLine("- " + resultsRefinement);
		
		sb.AppendLine("\n== RESULTS");
		foreach (RendererWrapper? renderer in results.Contents.TwoColumnSearchResultsRenderer.PrimaryContents
			         .ResultsContainer.Results.SelectMany(x => x.ItemSectionRenderer?.Contents ?? []))
			sb.AppendLine("->\t" + string.Join("\n\t", Utils.SerializeRenderer(renderer).Split("\n")));
		Assert.Pass(sb.ToString());
	}
}