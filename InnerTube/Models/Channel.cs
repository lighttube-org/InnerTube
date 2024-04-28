using InnerTube.Protobuf;

namespace InnerTube.Models;

public readonly struct Channel(
	string id,
	string title,
	string? handle,
	Thumbnails? avatar,
	string? subscribersText)
{
	public string Id { get; } = id;
	public string Title { get; } = title;
	public string? Handle { get; } = handle;
	public Thumbnail[]? Avatar { get; } = avatar?.Thumbnails_.ToArray();
	public string? SubscribersText { get; } = subscribersText;

	public override string ToString()
	{
		string res =  $"[{Id}] {Title}";
		if (Handle != null)
			res += $" ({Handle})";
		if (SubscribersText != null)
			res += $" ({SubscribersText})";
		if (Avatar != null)
			res += $" (has {Avatar.Length} avatars)";
		return res;
	}

	public static Channel From(VideoOwnerRenderer videoOwnerRenderer) =>
		new(id: videoOwnerRenderer.NavigationEndpoint.BrowseEndpoint.BrowseId,
			title: Utils.ReadRuns(videoOwnerRenderer.Title),
			handle: TryGetHandle(videoOwnerRenderer.NavigationEndpoint.BrowseEndpoint.CanonicalBaseUrl),
			avatar: videoOwnerRenderer.Thumbnail,
			subscribersText: Utils.ReadRuns(videoOwnerRenderer.SubscriberCountText));

	public static Channel From(Text bylineText) =>
		new(id: bylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId,
			title: Utils.ReadRuns(bylineText),
			handle: TryGetHandle(bylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.CanonicalBaseUrl),
			avatar: null,
			subscribersText: null);

	private static string? TryGetHandle(string url)
	{
		string res = url.TrimStart('/');
		return res[0] == '@' ? res : null;
	}
}