using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class CommentThreadRenderer : IRenderer
{
	public string Type { get; }

	public string Content { get; }
	public string? PublishedTimeText { get; }
	public Channel Owner { get; }
	public string? LikeCount { get; }
	public bool Loved { get; }
	public string? ReplyContinuation { get; }
	public bool AuthorIsChannelOwner { get; }

	public CommentThreadRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();
		Content = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("comment.commentRenderer.contentText.runs")!);
		Owner = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("comment.commentRenderer.authorEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("comment.commentRenderer.authorText.simpleText")!,
			Avatar = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("comment.commentRenderer.authorThumbnail.thumbnails")!).Last().Url,
			Subscribers = null,
			Badges = Array.Empty<Badge>() //TODO
		};
		PublishedTimeText = renderer.GetFromJsonPath<string>("comment.commentRenderer.publishedTimeText.runs[0].text")!;
		AuthorIsChannelOwner = renderer.GetFromJsonPath<bool>("comment.commentRenderer.authorIsChannelOwner")!;
		LikeCount = renderer.GetFromJsonPath<string>("comment.commentRenderer.voteCount.simpleText");
		Loved = false; //TODO
		ReplyContinuation = renderer.GetFromJsonPath<string>("replies.commentRepliesRenderer.contents[0].continuationItemRenderer.continuationEndpoint.continuationCommand.token");
	}

	public CommentThreadRenderer(string content, Channel owner, string? likeCount, bool loved, string? replyContinuation, string? publishedTimeText)
	{
		Type = "comment";
		Content = content;
		Owner = owner;
		LikeCount = likeCount;
		Loved = loved;
		ReplyContinuation = replyContinuation;
		PublishedTimeText = publishedTimeText;
	}

	public override string ToString()
	{
		return $"{Owner.ToString(false)}: \"{Content}\" | {LikeCount ?? "0"} likes {(Loved ? "| Loved by the video author" : "")}";
	}
}