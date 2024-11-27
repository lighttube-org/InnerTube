using InnerTube.Parsers;
using InnerTube.Protobuf;

namespace InnerTube.Models;

public class PlaylistSidebar(PlaylistHeaderRenderer sidebar, string parserLanguage)
{
	public string Title { get; } = Utils.ReadRuns(sidebar.Title);

	public Thumbnail[] Thumbnails { get; } = sidebar.CinematicContainer?.CinematicContainerRenderer.BackgroundImageConfig
		.Thumbnails.Thumbnails_.ToArray() ?? [];

	public string VideoCountText { get; } = Utils.ReadRuns(sidebar.NumVideosText);
	public long VideoCount { get; } = ValueParser.ParseVideoCount(parserLanguage, Utils.ReadRuns(sidebar.NumVideosText));
	public string ViewCountText { get; } = Utils.ReadRuns(sidebar.ViewCountText);
	public long ViewCount { get; } = ValueParser.ParseViewCount(parserLanguage, Utils.ReadRuns(sidebar.ViewCountText));
	public string LastUpdatedText { get; } = Utils.ReadRuns(sidebar.Byline.PlaylistBylineRenderer.Text.Last());

	public DateTimeOffset LastUpdated { get; } = ValueParser.ParseLastUpdated(parserLanguage,
		Utils.ReadRuns(sidebar.Byline.PlaylistBylineRenderer.Text.Last()));
	public string Description { get; } = Utils.ReadRuns(sidebar.DescriptionText);
	public Channel? Channel { get; } = Channel.From(sidebar.OwnerText);
}