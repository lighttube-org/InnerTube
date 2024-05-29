using InnerTube.Parsers;
using InnerTube.Protobuf;

namespace InnerTube.Models;

public class Channel(
	string parserLanguage,
	string id,
	string title,
	string? handle,
	Thumbnail[]? avatar,
	string? subscribersText,
	MetadataBadgeRenderer[]? badges)
{
	public string Id { get; } = id;
	public string Title { get; } = title;
	public string? Handle { get; } = handle;
	public Thumbnail[]? Avatar { get; } = avatar;
	public string? SubscribersText { get; } = subscribersText;

	public long? Subscribers { get; } = subscribersText != null
		? ValueParser.ParseSubscriberCount(parserLanguage, subscribersText)
		: null;
	public Badge[]? Badges { get; } = badges != null ? Utils.SimplifyBadges(badges) : null;

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

	public static Channel From(VideoOwnerRenderer videoOwnerRenderer, string parserLanguage, MetadataBadgeRenderer[]? badges = null) =>
		new(parserLanguage,
			id: videoOwnerRenderer.NavigationEndpoint.BrowseEndpoint.BrowseId,
			title: Utils.ReadRuns(videoOwnerRenderer.Title),
			handle: TryGetHandle(videoOwnerRenderer.NavigationEndpoint.BrowseEndpoint.CanonicalBaseUrl),
			avatar: videoOwnerRenderer.Thumbnail.Thumbnails_.ToArray(),
			subscribersText: Utils.ReadRuns(videoOwnerRenderer.SubscriberCountText),
			badges: badges);

	public static Channel? From(Text? bylineText, MetadataBadgeRenderer[]? badges = null, Thumbnails? avatar = null)
	{
		if (bylineText == null)
			return null;
		try
		{
			return new Channel("",
				id: bylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId,
				title: bylineText.Runs[0].Text,
				handle: TryGetHandle(bylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.CanonicalBaseUrl),
				avatar: avatar?.Thumbnails_.ToArray(),
				subscribersText: null,
				badges: badges);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static Channel From(CommentEntityPayload.Types.CommentAuthor commentAuthor) =>
		new("",
			id: commentAuthor.ChannelId,
			title: commentAuthor.DisplayName,
			handle: TryGetHandle(commentAuthor.ChannelCommand.InnertubeCommand.BrowseEndpoint.CanonicalBaseUrl),
			avatar: [
				new Thumbnail
				{
					Url = commentAuthor.AvatarThumbnailUrl,
					Width = 88,
					Height = 88
				}
			],
			subscribersText: null,
			badges: null); // TODO: badges

	internal static string? TryGetHandle(string url)
	{
		string res = url.TrimStart('/');
		return url.Length != 0 ? res[0] == '@' ? res : null : null;
	}
}