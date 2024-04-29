using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class CommentRendererData(
	CommentThreadRenderer thread,
	CommentEntityPayload viewModel,
	EngagementToolbarStateEntityPayload toolbarState) : IRendererData
{
	public string Id { get; } = viewModel.Properties.CommentId;

	public string Content { get; } =
		Utils.Formatter.HandleLineBreaks(
			Utils.Formatter.Sanitize(viewModel.Properties.Content.Content)); // todo: formatting doesnt exist here
	public string? PublishedTimeText { get; } = viewModel.Properties.PublishedTime;
	public Channel Owner { get; } = Channel.From(viewModel.Author);
	public string? LikeCountText { get; } = viewModel.Toolbar.LikeCountNotLiked;
	public string? ReplyCountText { get; } = viewModel.Toolbar.ReplyCount;
	public HeartInfo? Loved { get; } = toolbarState.HeartState == 1 ? new HeartInfo(viewModel) : null;
	public bool Pinned { get; } = thread.CommentViewModel.CommentViewModel.HasPinnedText;
	public bool AuthorIsChannelOwner { get; } = viewModel.Author.IsCreator;

	public string? ReplyContinuation { get; } = thread.Replies?.CommentRepliesRenderer.ContinuationItemRenderer[0]
		.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token;
	
	public override string ToString() =>
		$"[{Id}] {(Pinned ? "[PINNED] |" : "|")} {Owner}{(Loved != null ? " (channel author)" : "")} | {PublishedTimeText}\n{Content}\n{LikeCountText ?? "0"} likes | {ReplyCountText ?? "0"} replies{(Loved != null ? $" | {Loved.HeartedBy}" : "")}";

	public class HeartInfo(CommentEntityPayload viewModel)
	{
		public string HeartedBy { get; } = viewModel.Toolbar.HeartActiveTooltip;
		public string HeartedAvatarUrl { get; } = viewModel.Toolbar.CreatorThumbnailUrl;
	}
}