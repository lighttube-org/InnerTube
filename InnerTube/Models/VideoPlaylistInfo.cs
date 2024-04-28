using InnerTube.Protobuf;
using InnerTube.Renderers;

namespace InnerTube.Models;

public class VideoPlaylistInfo(Playlist playlist)
{
	public string PlaylistId { get; } = playlist.PlaylistId;
	public string Title { get; } = playlist.Title;
	public int TotalVideos { get; } = playlist.TotalVideos;
	public int CurrentIndex { get; } = playlist.CurrentIndex;
	public int LocalCurrentIndex { get; } = playlist.LocalCurrentIndex;
	public Channel Channel { get; } = Channel.From(playlist.LongBylineText);
	public bool IsCourse { get; } = playlist.IsCourse;
	public bool IsInfinite { get; } = playlist.IsInfinite;
	public IEnumerable<RendererContainer> Videos { get; } = Utils.ConvertRenderers(playlist.Contents);
}