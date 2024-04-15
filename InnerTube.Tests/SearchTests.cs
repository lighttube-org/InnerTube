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

	[TestCase(
		"EtEEEgh6dXRvbWF5bxrYA1NCU0NBUmhWUTJOa0xVZFBkbXc1UkdSNVVGWklVWGg1TlRoaVQzZUNBUXRCZEhaeloxOTZiMmQ0YjRJQkMxUjRWV3hvWTJwSVdtVXdnZ0VMUWxaMmRsVkhVREJOUm5lQ0FRczBVV1ZRY25ZeU5GUkNWWUlCQ3paUFF6a3liM2h6TkdkQmdnRUxaR05QZDJvdFVVVmZXa1dDQVJwU1JFVk5WMWhXU1dwbk9HNW5NRlp1WlRWVGJHdDVPVU5RVVlJQklsQk1TV1J5WWpOWlgxUTJkMk4yUkhNeGMzUkJNazFCZW1GWlRHSlpWMFZ1ZGtLQ0FRdGxOVXhoUzNoS1ZtVldTWUlCQzNWbmNIbDNaVE0wWHpNd2dnRUxSMHBKTkVkMk4wNWliVVdDQVF0UWRXUjFlRGxTUzB3NVNZSUJDekkxT0hGVlFVazNjbU5yZ2dFTFJXeHVlRnAwYVVKRWRuT0NBUXRKT0RoUWNrVXRTMVZRYTRJQkMxZGtiR3c1VURscFkwcFZnZ0VMV2xWM1lYVmtkemhvZERDQ0FRdEhRVUl5TmtkblNqaFdPSUlCQzFsbmJVWkpWazlTTVMxSnNnRUdDZ1FJSFJBQzZnRUVDQUVRSXclM0QlM0SSAmkvc2VhcmNoP29xPXp1dG9tYXlvJmdzX2w9eW91dHViZS4xMi4uLjAuMC4wLjE5NDYuMC4wLjAuMC4wLjAuMC4wLi4wLjAuLi4uMC4uLjFhYy4uNjQueW91dHViZS4uMC4wLjAuLi4uMC4YgeDoGCILc2VhcmNoLWZlZWQ%3D",
		TestName = "i hope this continuation token also never expires")]
	public async Task ContinueSearchAsync(string continuation)
	{
		SearchResponse results = await _innerTube.ContinueSearchAsync(continuation);
		StringBuilder sb = new();
		sb.AppendLine("\n== RESULTS");
		foreach (RendererWrapper? renderer in results.OnResponseReceivedCommands.AppendContinuationItemsAction
			         .ContinuationItems.SelectMany(x => x.ItemSectionRenderer?.Contents ?? []))
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
				sb.AppendLine("Video ID: " + (cta.NavigationEndpoint.WatchEndpoint?.VideoId ?? "<null>"));
				sb.AppendLine("Hero Image:");
				sb.AppendLine($"- Left Thumbnail: ({cta.HeroImage.CollageHeroImageRenderer.LeftThumbnail.Thumbnails_.Count})" + string.Join("",
					cta.HeroImage.CollageHeroImageRenderer.LeftThumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine($"- Top Right:: ({cta.HeroImage.CollageHeroImageRenderer.TopRightThumbnail.Thumbnails_.Count})" + string.Join("",
					cta.HeroImage.CollageHeroImageRenderer.TopRightThumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine($"- Bottom Right: ({cta.HeroImage.CollageHeroImageRenderer.BottomRightThumbnail.Thumbnails_.Count})" + string.Join("",
					cta.HeroImage.CollageHeroImageRenderer.BottomRightThumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));

				sb.AppendLine("\n=== SECTIONS");
				foreach (RendererWrapper section in card.Sections)
				{
					for (int i = 0; i < section.WatchCardSectionSequenceRenderer.Lists.Count; i++)
					{
						RendererWrapper list = section.WatchCardSectionSequenceRenderer.Lists[i];
						string? title = null;
						if (section.WatchCardSectionSequenceRenderer.ListTitles.Count > i) 
							title = Utils.ReadRuns(section.WatchCardSectionSequenceRenderer.ListTitles[i]);
						sb.AppendLine("Title: " + (title ?? "<no title>"));
						
						switch (list.RendererCase)
						{
							case RendererWrapper.RendererOneofCase.VerticalWatchCardListRenderer:
							{
								VerticalWatchCardListRenderer l = list.VerticalWatchCardListRenderer;
								foreach (RendererWrapper renderer in l.Items)
								{
									sb.AppendLine(
										$"[{renderer.WatchCardCompactVideoRenderer.NavigationEndpoint.WatchEndpoint.VideoId}] " +
										Utils.ReadRuns(renderer.WatchCardCompactVideoRenderer.Title));
									sb.Append(Utils.ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle));
									sb.Append("       ");
									sb.AppendLine(Utils.ReadRuns(renderer.WatchCardCompactVideoRenderer.LengthText));
								}
								break;
							}
							case RendererWrapper.RendererOneofCase.HorizontalCardListRenderer:
							{
								HorizontalCardListRenderer l = list.HorizontalCardListRenderer;
								foreach (RendererWrapper renderer in l.Cards)
								{
									sb.AppendLine($"[{renderer.SearchRefinementCardRenderer.Thumbnail.Thumbnails_[0].Url}]");
									sb.AppendLine(Utils.ReadRuns(renderer.SearchRefinementCardRenderer.Query));
								}
								break;
							}
							default:
								sb.AppendLine("Unknown RendererCase: " + list.RendererCase);
								break;
						}
					}
					sb.AppendLine();
				}
				
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