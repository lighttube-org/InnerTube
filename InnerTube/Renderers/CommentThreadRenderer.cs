using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class CommentThreadRenderer : IRenderer
{
	public string Type => "commentThreadRenderer";

	public string Id { get; }
	public string Content { get; }
	public string? PublishedTimeText { get; }
	public Channel Owner { get; }
	public string? LikeCount { get; }
	public bool Loved { get; }
	public bool Pinned { get; }
	public string? ReplyContinuation { get; }
	public bool AuthorIsChannelOwner { get; }

	public CommentThreadRenderer(JToken renderer)
	{
		Id = renderer.GetFromJsonPath<string>("comment.commentRenderer.commentId")!;
		Content = Utils.ReadText(renderer.GetFromJsonPath<JObject>("comment.commentRenderer.contentText")!, true);
		Badge? authorBadge = Badge.FromAuthorCommentBadgeRenderer(
			renderer.GetFromJsonPath<JObject>("comment.commentRenderer.authorCommentBadge.authorCommentBadgeRenderer"));
		Owner = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("comment.commentRenderer.authorEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("comment.commentRenderer.authorText.simpleText")!,
			Avatar = Utils
				.GetThumbnails(renderer.GetFromJsonPath<JArray>("comment.commentRenderer.authorThumbnail.thumbnails")!)
				.Last().Url,
			Subscribers = null,
			Badges = authorBadge is null ? Array.Empty<Badge>() : new[] { authorBadge }
		};
		PublishedTimeText = renderer.GetFromJsonPath<string>("comment.commentRenderer.publishedTimeText.runs[0].text")!;
		AuthorIsChannelOwner = renderer.GetFromJsonPath<bool>("comment.commentRenderer.authorIsChannelOwner")!;
		LikeCount = renderer.GetFromJsonPath<string>("comment.commentRenderer.voteCount.simpleText");
		Loved = renderer.GetFromJsonPath<JToken>(
			"comment.commentRenderer.actionButtons.commentActionButtonsRenderer.creatorHeart") != null;
		Pinned = renderer.GetFromJsonPath<JToken>("comment.commentRenderer.pinnedCommentBadge") != null;
		ReplyContinuation = renderer.GetFromJsonPath<string>(
			"replies.commentRepliesRenderer.contents[0].continuationItemRenderer.continuationEndpoint.continuationCommand.token");
	}

	public CommentThreadRenderer(string id, string content, Channel owner, string? likeCount, bool loved,
		string? replyContinuation, string? publishedTimeText)
	{
		Id = id;
		Content = content;
		Owner = owner;
		LikeCount = likeCount;
		Loved = loved;
		ReplyContinuation = replyContinuation;
		PublishedTimeText = publishedTimeText;
	}

	public override string ToString() =>
		$"[{Id}] {(Pinned ? "[PINNED] " : "")}{Owner.ToString(false)}: \"{Content}\" | {LikeCount ?? "0"} likes{(Loved ? " | Loved by the video author" : "")}";
}