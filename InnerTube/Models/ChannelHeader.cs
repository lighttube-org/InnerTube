using InnerTube.Parsers;
using InnerTube.Protobuf;

namespace InnerTube.Models;

public class ChannelHeader
{
	public string Id { get; }
	public Thumbnail[] Avatars { get; }
	public Thumbnail[] Banner { get; }
	public Thumbnail[] TvBanner { get; }
	public Thumbnail[] MobileBanner { get; }
	public Badge[] Badges { get; }
	public string? PrimaryLink { get; }
	public string? SecondaryLink { get; }
	public string SubscriberCountText { get; }
	public long SubscriberCount { get; }
	public string Title { get; }
	public string Handle { get; }
	public string VideoCountText { get; }
	public long VideoCount { get; }
	public string Tagline { get; }

	public ChannelHeader(C4TabbedHeaderRenderer header, string parserLanguage)
	{
		Id = header.ChannelId;
		Avatars = header.Avatar.Thumbnails_.ToArray();
		Banner = header.Banner?.Thumbnails_?.ToArray() ?? [];
		TvBanner = header.TvBanner?.Thumbnails_?.ToArray() ?? [];
		MobileBanner = header.MobileBanner?.Thumbnails_?.ToArray() ?? [];
		Badges = Utils.SimplifyBadges(header.Badges);
		PrimaryLink = Utils
			.ReadAttributedDescription(header.HeaderLinks.FirstOrDefault()?.ChannelHeaderLinksViewModel.FirstLink, true)
			.NullIfEmpty();
		SecondaryLink = Utils
			.ReadAttributedDescription(header.HeaderLinks.FirstOrDefault()?.ChannelHeaderLinksViewModel.More, true)
			.NullIfEmpty();
		SubscriberCountText = Utils.ReadRuns(header.SubscriberCountText);
		SubscriberCount = ValueParser.ParseSubscriberCount(parserLanguage, SubscriberCountText);
		Title = header.Title;
		Handle = Utils.ReadRuns(header.ChannelHandleText);
		VideoCountText = Utils.ReadRuns(header.VideosCountText);
		VideoCount = ValueParser.ParseVideoCount(parserLanguage, VideoCountText);
		Tagline = header.Tagline.ChannelTaglineRenderer.Content;
	}

	public ChannelHeader(PageHeaderRenderer header, string channelId, string parserLanguage)
	{
		Id = channelId;
		Avatars = header.Content.PageHeaderViewModel.Image.DecoratedAvatarViewModel.Avatar.AvatarViewModel.Image.Sources
			.Select(x => new Thumbnail
			{
				Url = x.Url,
				Width = x.Width,
				Height = x.Height
			}).ToArray();
		Banner = header.Content.PageHeaderViewModel.Banner?.ImageBannerViewModel.Image.Sources
			.Select(x => new Thumbnail
			{
				Url = x.Url,
				Width = x.Width,
				Height = x.Height
			}).ToArray() ?? [];
		TvBanner = [];
		MobileBanner = [];
		Badges = [];
		PrimaryLink = Utils
			.ReadAttributedDescription(header.Content.PageHeaderViewModel.Attribution?.AttributionViewModel.Text, true)
			.NullIfEmpty();
		SecondaryLink = Utils
			.ReadAttributedDescription(header.Content.PageHeaderViewModel.Attribution?.AttributionViewModel.Suffix, true)
			.NullIfEmpty();
		SubscriberCountText = header.Content.PageHeaderViewModel.Metadata.ContentMetadataViewModel.MetadataRows[1]
			                      .MetadataParts.Count > 1
			? Utils.ReadAttributedDescription(header.Content.PageHeaderViewModel.Metadata
				.ContentMetadataViewModel.MetadataRows[1].MetadataParts[0].Text)
			: "";
		SubscriberCount = ValueParser.ParseSubscriberCount(parserLanguage, SubscriberCountText);
		Title = header.PageTitle;
		Handle = Utils.ReadAttributedDescription(header.Content.PageHeaderViewModel.Metadata.ContentMetadataViewModel
			.MetadataRows[0].MetadataParts[0].Text);
		VideoCountText = header.Content.PageHeaderViewModel.Metadata.ContentMetadataViewModel.MetadataRows[1]
			.MetadataParts.Count > 1
			? Utils.ReadAttributedDescription(header.Content.PageHeaderViewModel.Metadata
				.ContentMetadataViewModel.MetadataRows[1].MetadataParts[1].Text)
			: Utils.ReadAttributedDescription(header.Content.PageHeaderViewModel.Metadata
				.ContentMetadataViewModel.MetadataRows[1].MetadataParts[0].Text);
		VideoCount = ValueParser.ParseVideoCount(parserLanguage, VideoCountText);
		Tagline = Utils.ReadAttributedDescription(
			header.Content.PageHeaderViewModel.Description.DescriptionPreviewViewModel.Content, true);
	}
}