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

	[TestCase("ずっと真夜中でいいのに。ZUTOMAYO", TestName = "Artist sidebar #1")]
	public async Task Sidebar(string query)
	{
		SearchResponse results = await _innerTube.SearchAsync(query);
		StringBuilder sb = new();
		if (results.Contents.TwoColumnSearchResultsRenderer.SecondaryContents is null) 
			Assert.Inconclusive("/!\\ Second column does not exist");
		
		switch (results.Contents.TwoColumnSearchResultsRenderer.SecondaryContents.SecondarySearchContainerRenderer.Contents[0].RendererCase)
		{
			case RendererWrapper.RendererOneofCase.UniversalWatchCardRenderer:
			{
				UniversalWatchCardRenderer card = results.Contents.TwoColumnSearchResultsRenderer.SecondaryContents
					.SecondarySearchContainerRenderer.Contents[0].UniversalWatchCardRenderer;
				WatchCardRichHeaderRenderer header = card.Header.WatchCardRichHeaderRenderer;
				sb.AppendLine("=== HEADER");
				sb.AppendLine("Title: " + Utils.ReadRuns(header.Title));
				sb.AppendLine("Subtitle: " + Utils.ReadRuns(header.Subtitle));
				sb.AppendLine($"Avatar: ({header.Avatar.Thumbnails_.Count})" + string.Join("",
					header.Avatar.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine("- PlaceholderColor: #" + Convert.ToHexString(BitConverter.GetBytes(header.Avatar.PlaceholderColor).Take(4).ToArray()));
				sb.AppendLine("TitleBadge: " + string.Join("\n            - ", Utils.SerializeRenderer(header.TitleBadge).Trim().Split("\n")));

				
				WatchCardHeroVideoRenderer cta = card.CallToAction.WatchCardHeroVideoRenderer;
				sb.AppendLine("\n=== CALL TO ACTION");
				sb.AppendLine("Title/Accessibility: " + cta.Accessibility.AccessibilityData.Label);
				sb.AppendLine("Video ID: " + cta.NavigationEndpoint.WatchEndpoint?.VideoId ?? "<null>");
				sb.AppendLine("Hero Image:");
				sb.AppendLine($"- Left Thumbnail: ({cta.HeroImage.CollageHeroImageRenderer.LeftThumbnail.Thumbnails_.Count})" + string.Join("",
					cta.HeroImage.CollageHeroImageRenderer.LeftThumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine($"- Top Right:: ({cta.HeroImage.CollageHeroImageRenderer.TopRightThumbnail.Thumbnails_.Count})" + string.Join("",
					cta.HeroImage.CollageHeroImageRenderer.TopRightThumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine($"- Bottom Right: ({cta.HeroImage.CollageHeroImageRenderer.BottomRightThumbnail.Thumbnails_.Count})" + string.Join("",
					cta.HeroImage.CollageHeroImageRenderer.BottomRightThumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				
				
				
				Assert.Pass(sb.ToString());
				break;
			}
			default:
				Assert.Inconclusive("/!\\ Unknown RendererCase: " + results.Contents.TwoColumnSearchResultsRenderer
					.SecondaryContents.SecondarySearchContainerRenderer.Contents[0].RendererCase);
				break;
		}
	}
}