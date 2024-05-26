using System.Text;
using InnerTube.Models;
using InnerTube.Parsers;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class ChannelRendererData : IRendererData
{
	public string ChannelId { get; set; }
	public string Title { get; set; }
	public string? Handle { get; set; }
	public Thumbnail[] Avatar { get; set; }
	public string? VideoCountText { get; set; }
	public long VideoCount { get; set; }
	public string? SubscriberCountText { get; set; }
	public long SubscriberCount { get; set; }
	public Badge[] Badges { get; set; }

	public ChannelRendererData()
	{ }

	public ChannelRendererData(ChannelRenderer channelRenderer, string parserLanguage)
	{
		ChannelId = channelRenderer.ChannelId;
		Title = Utils.ReadRuns(channelRenderer.Title);
		Handle = Channel.TryGetHandle(channelRenderer.NavigationEndpoint.BrowseEndpoint
			.CanonicalBaseUrl);
		Avatar = channelRenderer.Thumbnail.Thumbnails_.ToArray();
		VideoCountText = Utils.ReadRuns(channelRenderer.VideoCountText);
		SubscriberCountText = Utils.ReadRuns(channelRenderer.SubscriberCountText);
		Badges = Utils.SimplifyBadges(channelRenderer.OwnerBadges);
		if (SubscriberCountText.StartsWith('@'))
		{
			SubscriberCount = ValueParser.ParseSubscriberCount(parserLanguage, Utils.ReadRuns(channelRenderer.VideoCountText));
		}
		else
		{
			SubscriberCount = ValueParser.ParseSubscriberCount(parserLanguage, Utils.ReadRuns(channelRenderer.SubscriberCountText));
			VideoCount = ValueParser.ParseVideoCount(parserLanguage, VideoCountText);
		}
	}

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{ChannelId}] {Title} ({Avatar.Length} avatars)");
		if (Handle != null)
			sb.AppendLine($"Handle: {Handle}");
		sb.AppendLine($"VideoCountText: {VideoCountText}");
		sb.AppendLine($"SubscriberCountText: {SubscriberCountText}");
		return sb.ToString();
	}
}