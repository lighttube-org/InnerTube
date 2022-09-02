using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubePlaylistInfo
{
	public string PlaylistId { get; }
	public string Title { get; }
	public int TotalVideos { get; }
	public int CurrentIndex { get; }
	public int LocalCurrentIndex { get; }
	public Channel Channel { get; }
	public bool IsCourse { get; }
	public bool IsInfinite { get; }
	public IEnumerable<PlaylistPanelVideoRenderer> Videos { get; }
	
	public InnerTubePlaylistInfo(JObject playlist)
	{
		PlaylistId = playlist.GetFromJsonPath<string>("playlistId")!;
		Title = playlist.GetFromJsonPath<string>("title")!;
		TotalVideos = playlist.GetFromJsonPath<int>("totalVideos")!;
		CurrentIndex = playlist.GetFromJsonPath<int>("currentIndex")!;
		LocalCurrentIndex = playlist.GetFromJsonPath<int>("localCurrentIndex")!;
		Channel = new Channel
		{
			Id = playlist.GetFromJsonPath<string>("longBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = playlist.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = Array.Empty<Badge>()
		};
		IsCourse = playlist.GetFromJsonPath<bool>("isCourse")!;
		IsInfinite = playlist.GetFromJsonPath<bool>("isInfinite")!;
		Videos = RendererManager.ParseRenderers(playlist.GetFromJsonPath<JArray>("contents")!)
			.Cast<PlaylistPanelVideoRenderer>();
	}
}