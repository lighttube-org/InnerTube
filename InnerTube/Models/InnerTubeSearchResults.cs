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
	public Options? SearchOptions { get; }

	public class TypoFixer
	{
		public string OriginalQuery { get; set; }
		public string CorrectedQuery { get; set; }
		public string Params { get; set; }

		public override string ToString() =>
			$"Showing results for '{CorrectedQuery}'. Search instead for '{OriginalQuery}' [{Params}]";
	}

	public class Options
	{
		public IEnumerable<Group> Groups { get; }
		public string Title { get; }

		public class Group
		{
			public string Title { get; }
			public IEnumerable<Filter> Filters { get; }

			public class Filter
			{
				public string Label { get; }
				public string? Params { get; }

				public Filter(JToken searchFilterRenderer)
				{
					Label = searchFilterRenderer["label"]!["simpleText"]!.ToString();
					Params = searchFilterRenderer["navigationEndpoint"]?["searchEndpoint"]?["params"]?.ToString();
				}
			}

			public Group(JToken searchFilterGroupRenderer)
			{
				Title = searchFilterGroupRenderer["title"]!["simpleText"]!.ToString();
				Filters = searchFilterGroupRenderer["filters"]!.ToObject<JArray>()!
					.Select(x => new Filter(x["searchFilterRenderer"]!));
			}
		}

		internal Options(JToken searchSubMenuRenderer)
		{
			Title = Utils.ReadText(searchSubMenuRenderer["title"]!.ToObject<JObject>()!);
			Groups = searchSubMenuRenderer["groups"]!.ToObject<JArray>()!.Select(x =>
				new Group(x["searchFilterGroupRenderer"]!));
		}
	}

	public InnerTubeSearchResults(JObject json)
	{
		JArray? contents = json.GetFromJsonPath<JArray>(
			"contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[0].itemSectionRenderer.contents");

		if (contents?[0].First?.Path.EndsWith("showingResultsForRenderer") ?? false)
		{
			JToken srfr = contents[0]["showingResultsForRenderer"]!;
			DidYouMean = new TypoFixer
			{
				OriginalQuery = srfr.GetFromJsonPath<string>("originalQuery.simpleText")!,
				CorrectedQuery = Utils.ReadText(srfr.GetFromJsonPath<JObject>("correctedQuery")!),
				Params = srfr.GetFromJsonPath<string>("originalQueryEndpoint.searchEndpoint.params")!
			};
			contents = JArray.FromObject(contents.Skip(1));
		}

		EstimatedResults = long.Parse(json["estimatedResults"]?.ToString() ?? "0");
		Refinements = json.GetFromJsonPath<string[]>("refinements") ?? Array.Empty<string>();
		Results = RendererManager.ParseRenderers(contents ?? new JArray()).ToList().AsReadOnly();
		Continuation =
			json.GetFromJsonPath<string>(
				"contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[1].continuationItemRenderer.continuationEndpoint.continuationCommand.token") ??
			null;

		JObject? searchSubMenuRenderer = json.GetFromJsonPath<JObject>(
			"contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.subMenu.searchSubMenuRenderer");
		if (searchSubMenuRenderer != null)
			SearchOptions = new Options(searchSubMenuRenderer);
	}
}