namespace InnerTube;

public class Comment
{
	public string Content { get; }
	public Channel Owner { get; }
	public string? LikeCount { get; }
	public bool Loved { get; }
	public string? ReplyContinuation { get; }

	public Comment(string content, Channel owner, string? likeCount, bool loved, string? replyContinuation)
	{
		Content = content;
		Owner = owner;
		LikeCount = likeCount;
		Loved = loved;
		ReplyContinuation = replyContinuation;
	}

	public override string ToString()
	{
		return $"{Owner.ToString(false)}: \"{Content}\" | {LikeCount ?? "0"} likes {(Loved ? "| Loved by the video author" : "")}";
	}
}