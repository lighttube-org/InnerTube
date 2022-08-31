using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeExploreResponse
{
	public IRenderer Contents { get; }
	public IRenderer Header { get; }
	public string BrowseId { get; }

	public InnerTubeExploreResponse(JObject browseResponse, string browseId)
	{
		BrowseId = browseId;
		Contents = RendererManager.ParseRenderer(
			browseResponse.GetFromJsonPath<JObject>("contents.twoColumnBrowseResultsRenderer.tabs[0].tabRenderer"),
			"tabRenderer")!;
		JToken headerElement = browseResponse["header"]!.First!;
		Header = RendererManager.ParseRenderer(headerElement.First, headerElement.Path.Split(".").Last())!;
	}
}