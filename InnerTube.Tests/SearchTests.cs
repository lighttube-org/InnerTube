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

	[TestCase("big buck bunny", null, Description = "Just a normal search")]
	[TestCase("big bcuk bunny", null, Description = "Search with a typo")]
	[TestCase("big bcuk bunny", "exact", Description = "Force to search with the typo")]
	[TestCase("technoblade skyblock", "playlist", Description = "Used to get playlistRenderer")]
	[TestCase("lofi radio", null, Description = "Used to get live videos")]
	[TestCase("technoblade", null, Description = "didYouMeanRenderer & channelRenderer")]
	[TestCase("O'zbekcha Kuylar 2020, Vol. 2", null, Description = "epic broken playlist")]
	[TestCase("cars 2", "movie", Description = "movieRenderer")]
	[TestCase("", "exact", Description = "backgroundPromoRenderer")]
	[TestCase("vpn", null, Description = "adSlotRenderer")]
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
		sb.AppendLine("\n== RESULTS");
		foreach (RendererWrapper? renderer in results.Contents.TwoColumnSearchResultsRenderer.PrimaryContents
			         .ResultsContainer.Results[0].ItemSectionRenderer.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t", Utils.SerializeRenderer(renderer).Split("\n")));
		Assert.Pass(sb.ToString());
	}
}