using System.Text;
using InnerTube.Renderers;

namespace InnerTube.Tests;

public class SearchTests
{
	private InnerTube _innerTube;

	private string[] _skip = {
		"videoRenderer",
		"channelRenderer",
		"playlistRenderer",
		"shelfRenderer",
		"horizontalCardListRenderer"
	};

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[TestCase("big buck bunny", null, Description = "Just a normal search")]
	[TestCase("big bcuk bunny", null, Description = "Search with a typo")]
	[TestCase("big bcuk bunny", "QgIIAQ%3D%3D", Description = "Force to search with the typo")]
	[TestCase("technoblade skyblock", null, Description = "Used to get playlistRenderer & channelRenderer")]
	[TestCase("lofi radio", null, Description = "Used to get live videos")]
	public async Task Search(string query, string param)
	{
		InnerTubeSearchResults results = await _innerTube.SearchAsync(query, param);
		StringBuilder sb = new();

		sb.AppendLine("EstimatedResults: " + results.EstimatedResults)
			.AppendLine("Continuation: " + string.Join("", results.Continuation?.Take(20) ?? "NONE") + "...")
			.AppendLine("TypoFixer: " + results.DidYouMean)
			.AppendLine("Refinements: \n" + string.Join('\n', results.Refinements.Select(x => $"- {x}")))
			.AppendLine("")
			.AppendLine("== RESULTS ==");

		foreach (IRenderer renderer in results.Results)
		{
			if (!_skip.Contains(renderer.Type))
				sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));
			else
				//sb.AppendLine($"->\t[{renderer.Type}]");
				continue;
		}

		Assert.Pass(sb.ToString());
	}

	[Test]
	public async Task SearchFilters()
	{
		InnerTubeSearchResults results = await _innerTube.SearchAsync(":)");
		StringBuilder sb = new();

		sb.AppendLine(results.SearchOptions.Title);
		foreach (InnerTubeSearchResults.Options.Group category in results.SearchOptions.Groups)
		{
			sb.AppendLine($"- {category.Title}");
			foreach (InnerTubeSearchResults.Options.Group.Filter filter in category.Filters)
				sb.AppendLine($"  [{filter.Params ?? "YOU ARE HERE"}] {filter.Label}");
		}

		Assert.Pass(sb.ToString());
	}
	
	[TestCase("EpMFEg9zYXVsIGdvb2RtYW4gM2QahANTQlNDQVF0blJHcE5XblpaVjFWa2I0SUJDM28zVDNJME56VkNRVFJGZ2dFTGFtVk5PWGxTU25kTGJEaUNBUXQ1TUhOWVZHSnhVSEJZUVlJQkMwOXlNVlJrYW1zMmFUUnpnZ0VMZEMxVFlXRlBTMmR0WW11Q0FRdE5kVXRWUzFwTGNHZFdiNElCQ3pkUE0yRkhOR3BzVVZCQmdnRUxVR04yUm1sclVsOW9NbFdDQVF0cmFGZGlOVVJ2WjFCblZZSUJDM281WDA5WU1WZFdXRmhWZ2dFTFRIbEZjV280YlVNM2FWR0NBUXN0WmpaaVlUUnhWbFpTUVlJQkMycFhSVzh4TWxGRVlra3dnZ0VMYUVGRmVWaFFXRWh2T0d1Q0FRdHFVMUpJZWxSWlZFWnBiNElCQzNoSFIycFNaV3B3Y1U4NGdnRUxTbVpxVmtsUmNFcFFlRUdDQVF0VWRYZHRhRTQ1ZGpkZlVZSUJDMFk1ZW5CNE16QnZPSGRac2dFR0NnUUlGUkFDkgL3AS9zZWFyY2g_b3E9c2F1bCBnb29kbWFuIDNkJmdzX2w9eW91dHViZS4zLi4waTQ3MWk0MzNrMWwyajBpNDcxazFqMGk1MTJpNDMzazFqMGk1MTJrMWowaTUxMmk0MzNrMWowaTUxMmk0MzNpMTMxazFqMGk1MTJrMWw3LjIyNzQuNDcwNS4wLjUwMDcuMTYuMTMuMC4zLjMuMC4zNDUuMjMxMS4wajlqMWoyLjEzLjAuLi4uMC4uLjFhYy4xLjY0LnlvdXR1YmUuLjEuMTQuMjE1Ny4wLi4waTQzM2kxMzFrMWowaTNrMS42MTAuTEs4aHZ1cXB0R3cYgeDoGCILc2VhcmNoLWZlZWQ%3D", Description = "A continuation key that i hope wont expire")]
	public async Task Continue(string continuation)
	{
		InnerTubeContinuationResponse results = await _innerTube.ContinueSearchAsync(continuation);
		StringBuilder sb = new();

		sb.AppendLine("Continuation: " + results.Continuation?.Substring(0, 20));
		
		foreach (IRenderer renderer in results.Contents)
		{
			if (!_skip.Contains(renderer.Type))
				sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));
			else
				sb.AppendLine($"->\t[{renderer.Type}]");
		}

		Assert.Pass(sb.ToString());
	}
}