using InnerTube.Exceptions;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubePlaylist
{
	public string Id { get; }
	public IEnumerable<string> Alerts { get; }
	public IEnumerable<PlaylistVideoRenderer> Videos { get; }
	public string? Continuation { get; }
	public PlaylistSidebar Sidebar { get; }

	public InnerTubePlaylist(JObject browseResponse)
	{
		JArray? alertsArray = browseResponse.GetFromJsonPath<JArray>("alerts");
		Alerts = alertsArray is not null
			? alertsArray.Select(x =>
			{
				JToken current = x.First!;
				for (int i = 0; i < 3; i++)
				{
					try
					{
						if (current["text"] is not null)
							return Utils.ReadText(current["text"]!.ToObject<JObject>()!);
					}
					catch
					{
					}

					current = current.First!;
				}

				return "";
			}).ToArray()
			: Array.Empty<string>();
		if (!browseResponse.ContainsKey("header") && !browseResponse.ContainsKey("sidebar"))
		{
			if (Alerts.Any())
				throw new NotFoundException(Alerts.First());
			throw new InnerTubeException("Response is missing important fields (header, sidebar)");
		}

		Id = browseResponse.GetFromJsonPath<string>("header.playlistHeaderRenderer.playlistId")!;

		IRenderer[] renderers = RendererManager.ParseRenderers(browseResponse.GetFromJsonPath<JArray>(
				"contents.twoColumnBrowseResultsRenderer.tabs[0].tabRenderer.content.sectionListRenderer.contents[0].itemSectionRenderer.contents[0].playlistVideoListRenderer.contents")
			).ToArray();
		Videos = renderers.Where(x => x is PlaylistVideoRenderer).Cast<PlaylistVideoRenderer>();
		Continuation = ((ContinuationItemRenderer?)renderers.FirstOrDefault(x => x is ContinuationItemRenderer))?.Token;
		Sidebar = new PlaylistSidebar(browseResponse.GetFromJsonPath<JObject>("sidebar.playlistSidebarRenderer")!);
	}
}