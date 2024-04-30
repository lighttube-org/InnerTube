using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class CommentRendererData : IRendererData
{
	public string Id { get; }
	public string Content { get; }
	public string? PublishedTimeText { get; }
	public Channel Owner { get; }
	public string? LikeCountText { get; }
	public string? ReplyCountText { get; }
	public HeartInfo? Loved { get; }
	// todo: try to put the channel name here
	// "Pinned by kuylar"
	public bool Pinned { get; }
	public bool AuthorIsChannelOwner { get; }
	public string? ReplyContinuation { get; }

	public override string ToString() =>
		$"[{Id}] {(Pinned ? "[PINNED] |" : "|")} {Owner}{(Loved != null ? " (channel author)" : "")} | {PublishedTimeText}\n{Content}\n{LikeCountText ?? "0"} likes | {ReplyCountText ?? "0"} replies{(Loved != null ? $" | {Loved.HeartedBy}" : "")}";


	public CommentRendererData(
		CommentThreadRenderer thread,
		CommentEntityPayload viewModel,
		EngagementToolbarStateEntityPayload toolbarState)
	{
		Id = viewModel.Properties.CommentId;
		Content =
			Utils.Formatter.HandleLineBreaks(
				Utils.Formatter.Sanitize(viewModel.Properties.Content.Content)); // todo: formatting doesnt exist here
		PublishedTimeText = viewModel.Properties.PublishedTime;
		Owner = Channel.From(viewModel.Author);
		LikeCountText = viewModel.Toolbar.LikeCountNotLiked;
		ReplyCountText = viewModel.Toolbar.ReplyCount;
		Loved = toolbarState.HeartState == 1 ? new HeartInfo(viewModel) : null;
		Pinned = thread.CommentViewModel.CommentViewModel.HasPinnedText;
		AuthorIsChannelOwner = viewModel.Author.IsCreator;
		ReplyContinuation = thread.Replies?.CommentRepliesRenderer.ContinuationItemRenderer[0]
			.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token;
	}

	public CommentRendererData(CommentThreadRenderer thread)
	{
		CommentRenderer comment = thread.Comment.CommentRenderer;
		Id = comment.CommentId;
		Content = Utils.ReadRuns(comment.ContentText, true);
		PublishedTimeText = Utils.ReadRuns(comment.PublishedTimeText);
		Owner = new Channel(
			id: comment.AuthorEndpoint.BrowseEndpoint.BrowseId,
			title: Utils.ReadRuns(comment.AuthorText),
			handle: Channel.TryGetHandle(comment.AuthorEndpoint.BrowseEndpoint
				.CanonicalBaseUrl),
			avatar: comment.AuthorThumbnail.Thumbnails_.ToArray(),
			subscribersText: null,
			badges: null
		);
		LikeCountText = Utils.ReadRuns(comment.VoteCount);
		ReplyCountText = comment.ReplyCount.ToString();
		RendererWrapper creatorHeart = comment.ActionButtons.CommentActionButtonsRenderer.CreatorHeart;
		Loved = creatorHeart != null ? new HeartInfo(creatorHeart.CreatorHeartRenderer) : null;
		Pinned = comment.PinnedCommentBadge != null;
		//Utils.ReadRuns(comment.PinnedCommentBadge.PinnedCommentBadgeRenderer.Label);
		AuthorIsChannelOwner = comment.AuthorIsChannelOwner;
		ReplyContinuation = thread.Replies?.CommentRepliesRenderer.ContinuationItemRenderer[0]
			.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token;
	}

	public class HeartInfo
	{
		public string HeartedBy { get; }
		public string HeartedAvatarUrl { get; }

		public HeartInfo(CommentEntityPayload viewModel)
		{
			HeartedBy = viewModel.Toolbar.HeartActiveTooltip;
			HeartedAvatarUrl = viewModel.Toolbar.CreatorThumbnailUrl;
		}

		public HeartInfo(CreatorHeartRenderer creatorHeart)
		{
			HeartedBy = creatorHeart.HeartedTooltip;
			HeartedAvatarUrl = creatorHeart.CreatorThumbnail.Thumbnails_.LastOrDefault()?.Url ?? "";
		}
	}
}