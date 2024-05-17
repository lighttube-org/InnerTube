using Google.Protobuf.Collections;
using InnerTube.Exceptions;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;

namespace InnerTube.Models;

public class InnerTubeVideo
{
	public string Id { get; }
	public string Title { get; }
	public string Description { get; }
	public string DateText { get; }
	public string ViewCountText { get; }
	public string LikeCountText { get; }
	public Channel Channel { get; }
	public string? CommentsCountText { get; }
	public string? CommentsErrorMessage { get; }
	public RendererContainer[] Recommended { get; }
	public VideoPlaylistInfo? Playlist { get; }
	public IEnumerable<VideoChapter>? Chapters { get; }

	public InnerTubeVideo(NextResponse next)
	{
		Id = next.CurrentVideoEndpoint.EndpointTypeCase switch
		{
			Endpoint.EndpointTypeOneofCase.WatchEndpoint => next.CurrentVideoEndpoint.WatchEndpoint.VideoId,
			Endpoint.EndpointTypeOneofCase.ReelWatchEndpoint => next.CurrentVideoEndpoint.ReelWatchEndpoint.VideoId,
			_ => ""
		};
		RepeatedField<RendererWrapper> firstColumnResults =
			next.Contents.TwoColumnWatchNextResults.Results.ResultsContainer.Results;
		if (firstColumnResults == null)
			throw new InnerTubeException("Cannot get information about this video");

		VideoPrimaryInfoRenderer videoPrimaryInfoRenderer = firstColumnResults.First(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.VideoPrimaryInfoRenderer).VideoPrimaryInfoRenderer;
		VideoSecondaryInfoRenderer videoSecondaryInfoRenderer = firstColumnResults.First(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.VideoSecondaryInfoRenderer).VideoSecondaryInfoRenderer;
		RendererWrapper commentsSection = firstColumnResults.First(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.ItemSectionRenderer &&
			x.ItemSectionRenderer.SectionIdentifier.StartsWith("comment")).ItemSectionRenderer.Contents[0];

		if (firstColumnResults[0].RendererCase == RendererWrapper.RendererOneofCase.ItemSectionRenderer &&
		    firstColumnResults[0].ItemSectionRenderer.Contents[0].BackgroundPromoRenderer != null)
			throw new InnerTubeException(Utils.ReadRuns(firstColumnResults[0].ItemSectionRenderer.Contents[0]
				.BackgroundPromoRenderer.Text));

		Title = Utils.ReadRuns(videoPrimaryInfoRenderer.Title, true);
		Description = Utils.ReadAttributedDescription(videoSecondaryInfoRenderer.AttributedDescription, true);
		DateText = Utils.ReadRuns(videoPrimaryInfoRenderer.DateText);
		ViewCountText = Utils.ReadRuns(videoPrimaryInfoRenderer.ViewCount?.VideoViewCountRenderer.ViewCount);
		LikeCountText = videoPrimaryInfoRenderer.VideoActions.MenuRenderer.TopLevelButtons
			.First(x => x.RendererCase == RendererWrapper.RendererOneofCase.SegmentedLikeDislikeButtonViewModel)
			.SegmentedLikeDislikeButtonViewModel.LikeButtonViewModel.LikeButtonViewModel.ToggleButtonViewModel
			.ToggleButtonViewModel.DefaultButtonViewModel.ButtonViewModel2.Title; // jesus christ
		Channel = Channel.From(videoSecondaryInfoRenderer.Owner.VideoOwnerRenderer);

		switch (commentsSection.RendererCase)
		{
			case RendererWrapper.RendererOneofCase.CommentsEntryPointHeaderRenderer:
				CommentsCountText = Utils.ReadRuns(commentsSection.CommentsEntryPointHeaderRenderer.CommentCount);
				break;
			case RendererWrapper.RendererOneofCase.MessageRenderer:
				CommentsErrorMessage = Utils.ReadRuns(commentsSection.MessageRenderer.Text, true);
				break;
			default:
				CommentsErrorMessage =
					$"[InnerTube] Unknown RendererCase for commentsSection: {commentsSection.RendererCase}";
				break;
		}

		Chapters = next.EngagementPanels
			.FirstOrDefault(x =>
				x.EngagementPanelSectionListRenderer.TargetId == "engagement-panel-macro-markers-description-chapters")
			?.EngagementPanelSectionListRenderer?.Content?.MacroMarkersListRenderer?.Contents?.Select(x =>
				new VideoChapter
				{
					StartSeconds = x.MacroMarkersListItemRenderer.OnTap.WatchEndpoint.StartTimeSeconds,
					Title = Utils.ReadRuns(x.MacroMarkersListItemRenderer.Title),
					Thumbnails = x.MacroMarkersListItemRenderer.Thumbnail
				}) ?? [];
		
		// NOTE: for age restricted videos, the first SecondaryResults is null
		Recommended =
			Utils.ConvertRenderers(next.Contents.TwoColumnWatchNextResults.SecondaryResults?.SecondaryResults?.Results);

		Playlist = next.Contents.TwoColumnWatchNextResults.Playlist != null
			? new VideoPlaylistInfo(next.Contents.TwoColumnWatchNextResults.Playlist.Playlist)
			: null;
	}
}