using InnerTube.Protobuf;

namespace InnerTube.Models;

public class Channel(
	string id,
	string title,
	string? handle,
	Thumbnails? avatar,
	string? subscribersText,
	MetadataBadgeRenderer[]? badges)
{
	public string Id { get; } = id;
	public string Title { get; } = title;
	public string? Handle { get; } = handle;
	public Thumbnail[]? Avatar { get; } = avatar?.Thumbnails_.ToArray();
	public string? SubscribersText { get; } = subscribersText;
	public MetadataBadgeRenderer[]? Badges { get; } = badges;

	public override string ToString()
	{
		string res =  $"[{Id}] {Title}";
		if (Handle != null)
			res += $" ({Handle})";
		if (SubscribersText != null)
			res += $" ({SubscribersText})";
		if (Avatar != null)
			res += $" (has {Avatar.Length} avatars)";
		if (Badges != null)
			res += $" (has {Badges.Length} badges)";
		return res;
	}

	public static Channel From(VideoOwnerRenderer videoOwnerRenderer, MetadataBadgeRenderer[]? badges = null) =>
		new(id: videoOwnerRenderer.NavigationEndpoint.BrowseEndpoint.BrowseId,
			title: Utils.ReadRuns(videoOwnerRenderer.Title),
			handle: TryGetHandle(videoOwnerRenderer.NavigationEndpoint.BrowseEndpoint.CanonicalBaseUrl),
			avatar: videoOwnerRenderer.Thumbnail,
			subscribersText: Utils.ReadRuns(videoOwnerRenderer.SubscriberCountText),
			badges: badges);

	public static Channel? From(Text bylineText, MetadataBadgeRenderer[]? badges = null)
	{
		try
		{
			return new Channel(id: bylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId,
				title: Utils.ReadRuns(bylineText),
				handle: TryGetHandle(bylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.CanonicalBaseUrl),
				avatar: null,
				subscribersText: null,
				badges: badges);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static string? TryGetHandle(string url)
	{
		string res = url.TrimStart('/');
		return url.Length != 0 ? res[0] == '@' ? res : null : null;
	}
}