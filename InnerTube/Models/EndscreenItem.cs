using InnerTube.Protobuf;

namespace InnerTube.Models;

public struct EndscreenItem(EndscreenElementRenderer item)
{
	public EndscreenItemType Type { get; } = (EndscreenItemType)item.Style;
	public string Title { get; } = Utils.ReadRuns(item.Title);
	public Thumbnails Image { get; } = item.Image;
	public string Metadata { get; } = Utils.ReadRuns(item.Metadata);
	public long StartMs { get; } = item.StartMs;
	public long EndMs { get; } = item.EndMs;
	public float AspectRatio { get; } = item.AspectRatio;
	public float Left { get; } = item.Left;
	public float Top { get; } = item.Top;
	public float Width { get; } = item.Width;

	public string? Target { get; } = item.Endpoint != null ? item.Style switch
	{
		EndscreenElementRenderer.Types.EndscreenElementStyle.Video => $"/watch?v={item.Endpoint.WatchEndpoint.VideoId}",
		EndscreenElementRenderer.Types.EndscreenElementStyle.Playlist =>
			$"/watch?v={item.Endpoint.WatchEndpoint.VideoId}&list={item.Endpoint.WatchEndpoint.PlaylistId}",
		EndscreenElementRenderer.Types.EndscreenElementStyle.Channel =>
			$"/channel/{item.Endpoint.BrowseEndpoint.BrowseId}",
		EndscreenElementRenderer.Types.EndscreenElementStyle.Website => Utils.UnwrapRedirectUrl(item.Endpoint
			.UrlEndpoint.Url),
		_ => ""
	} : null;
}