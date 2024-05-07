using System.Web;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;

namespace InnerTube.Models;

public class InnerTubePlaylist
{
	public string Id { get; }
	public IEnumerable<string> Alerts { get; }
	public IEnumerable<RendererContainer> Contents { get; }
	public string? Continuation { get; }
	public PlaylistSidebar Sidebar { get; }

	public InnerTubePlaylist(BrowseResponse browse)
	{
		Id = HttpUtility.ParseQueryString(
			browse.Metadata?.PlaylistMetadataRenderer?.AndroidAppindexingLink?.Split('?')[1] ?? "")["list"] ?? "";
		Alerts = browse.Alerts.Select(x => Utils.ReadRuns(x.AlertWithButtonRenderer?.Text));
		IEnumerable<RendererWrapper> renderers = browse.Contents.TwoColumnBrowseResultsRenderer.Tabs[0]
			                                         .TabRenderer.Content?
			                                         .ResultsContainer.Results[0].ItemSectionRenderer
			                                         .Contents[0].PlaylistVideoListRenderer?.Contents ??
		                                         browse.Contents.TwoColumnBrowseResultsRenderer.Tabs[0]
			                                         .TabRenderer.Content?
			                                         .ResultsContainer.Results[0].ItemSectionRenderer
			                                         .Contents ??
		                                         [];
		RendererContainer[] items = Utils.ConvertRenderers(renderers);
		Contents = items.Where(x => x.Type != "continuation");
		Continuation = (items.LastOrDefault(x => x.Type == "continuation")?.Data as ContinuationRendererData)
			?.ContinuationToken;
		Sidebar = new PlaylistSidebar(browse.Header.PlaylistHeaderRenderer);
	}
}