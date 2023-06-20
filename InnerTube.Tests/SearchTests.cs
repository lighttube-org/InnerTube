using System.Text;
using System.Web;
using InnerTube.Renderers;

// ReSharper disable StringLiteralTypo

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
	[TestCase("technoblade skyblock", null, Description = "Used to get playlistRenderer & channelRenderer")]
	[TestCase("lofi radio", null, Description = "Used to get live videos")]
	[TestCase("EvCZ9W2xAMQ", null, Description = "Premiere video")]
	[TestCase("technoblade", null, Description = "didYouMeanRenderer")]
	[TestCase("O'zbekcha Kuylar 2020, Vol. 2", null, Description = "epic broken playlist")]
	[TestCase("cars 2", "movie", Description = "movieRenderer")]
	public async Task Search(string query, string paramArgs)
	{
		SearchParams? param = paramArgs switch
		{
			"movie" => new SearchParams { Filters = new SearchFilters { Type = SearchFilters.Types.ItemType.Movie } },
			"exact" => new SearchParams { QueryFlags = new QueryFlags { ExactSearch = true } },
			_ => null
		};
		InnerTubeSearchResults results = await _innerTube.SearchAsync(query, param);
		StringBuilder sb = new();

		sb.AppendLine("EstimatedResults: " + results.EstimatedResults)
			.AppendLine("Continuation: " + string.Join("", results.Continuation?.Take(20) ?? "NONE") + "...")
			.AppendLine("TypoFixer: " + results.DidYouMean)
			.AppendLine("Refinements: \n" + string.Join('\n', results.Refinements.Select(x => $"- {x}")))
			.AppendLine("")
			.AppendLine("== RESULTS ==");

		foreach (IRenderer renderer in results.Results)
			sb.AppendLine("->\t" + string.Join("\n\t",
				(renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		Assert.Pass(sb.ToString());
	}

	[TestCase(
		"EpMFEg9zYXVsIGdvb2RtYW4gM2QahANTQlNDQVF0blJHcE5XblpaVjFWa2I0SUJDM28zVDNJME56VkNRVFJGZ2dFTGFtVk5PWGxTU25kTGJEaUNBUXQ1TUhOWVZHSnhVSEJZUVlJQkMwOXlNVlJrYW1zMmFUUnpnZ0VMZEMxVFlXRlBTMmR0WW11Q0FRdE5kVXRWUzFwTGNHZFdiNElCQ3pkUE0yRkhOR3BzVVZCQmdnRUxVR04yUm1sclVsOW9NbFdDQVF0cmFGZGlOVVJ2WjFCblZZSUJDM281WDA5WU1WZFdXRmhWZ2dFTFRIbEZjV280YlVNM2FWR0NBUXN0WmpaaVlUUnhWbFpTUVlJQkMycFhSVzh4TWxGRVlra3dnZ0VMYUVGRmVWaFFXRWh2T0d1Q0FRdHFVMUpJZWxSWlZFWnBiNElCQzNoSFIycFNaV3B3Y1U4NGdnRUxTbVpxVmtsUmNFcFFlRUdDQVF0VWRYZHRhRTQ1ZGpkZlVZSUJDMFk1ZW5CNE16QnZPSGRac2dFR0NnUUlGUkFDkgL3AS9zZWFyY2g_b3E9c2F1bCBnb29kbWFuIDNkJmdzX2w9eW91dHViZS4zLi4waTQ3MWk0MzNrMWwyajBpNDcxazFqMGk1MTJpNDMzazFqMGk1MTJrMWowaTUxMmk0MzNrMWowaTUxMmk0MzNpMTMxazFqMGk1MTJrMWw3LjIyNzQuNDcwNS4wLjUwMDcuMTYuMTMuMC4zLjMuMC4zNDUuMjMxMS4wajlqMWoyLjEzLjAuLi4uMC4uLjFhYy4xLjY0LnlvdXR1YmUuLjEuMTQuMjE1Ny4wLi4waTQzM2kxMzFrMWowaTNrMS42MTAuTEs4aHZ1cXB0R3cYgeDoGCILc2VhcmNoLWZlZWQ%3D",
		Description = "A continuation key that i hope wont expire")]
	public async Task Continue(string continuation)
	{
		InnerTubeContinuationResponse results = await _innerTube.ContinueSearchAsync(continuation);
		StringBuilder sb = new();

		sb.AppendLine("Continuation: " + results.Continuation?.Substring(0, 20));

		foreach (IRenderer renderer in results.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t",
				(renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		Assert.Pass(sb.ToString());
	}

	[TestCase("big buck bun")]
	[TestCase("qwer :)")]
	[TestCase("asdf (asdf")]
	public async Task Autocomplete(string query)
	{
		InnerTubeSearchAutocomplete innerTubeSearchAutocomplete = await _innerTube.GetSearchAutocompleteAsync(query);

		StringBuilder sb = new();
		sb.AppendLine("Query: " + innerTubeSearchAutocomplete.Query)
			.AppendLine(new string('=', innerTubeSearchAutocomplete.Query.Length + 7));
		foreach (string autocomplete in innerTubeSearchAutocomplete.Autocomplete)
			sb.AppendLine(autocomplete);
		Assert.Pass(sb.ToString());
	}
}