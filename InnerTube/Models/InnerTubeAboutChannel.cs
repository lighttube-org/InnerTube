using InnerTube.Parsers;
using InnerTube.Protobuf;

namespace InnerTube.Models;

public class InnerTubeAboutChannel(AboutChannelViewModel viewModel, string parserLanguage)
{
	public string? Description { get; } = viewModel.Description;
	public string? ArtistBio { get; } = Utils.ReadAttributedDescription(viewModel.ArtistBio);
	public string Country { get; } = viewModel.Country;
	public string SubscriberCountText { get; } = viewModel.SubscriberCountText;
	public long SubscriberCount { get; } = ValueParser.ParseSubscriberCount(parserLanguage, viewModel.SubscriberCountText);
	public string ViewCountText { get; } = viewModel.ViewCountText;
	public long ViewCount { get; } = ValueParser.ParseViewCount(parserLanguage, viewModel.ViewCountText);
	public string VideoCountText { get; } = viewModel.VideoCountText;
	public long VideoCount { get; } = ValueParser.ParseVideoCount(parserLanguage, viewModel.VideoCountText);
	public string JoinedDateText { get; } = Utils.ReadAttributedDescription(viewModel.JoinedDateText);
	public DateTimeOffset JoinedDate { get; } = ValueParser.ParseFullDate(parserLanguage, Utils.ReadAttributedDescription(viewModel.JoinedDateText));
	public string CanonicalChannelUrl { get; } = viewModel.CanonicalChannelUrl;
	public string ChannelId { get; } = viewModel.ChannelId;
	public Link[] ChannelLinks { get; } = viewModel.Links.Select(x => new Link(x.ChannelExternalLinkViewModel)).ToArray();

	public class Link(ChannelExternalLinkViewModel viewModel)
	{
		public string Title { get; } = Utils.ReadAttributedDescription(viewModel.Title);
		public string LinkText { get; } = Utils.ReadAttributedDescription(viewModel.Link);
		public string Url { get; } = Utils.UnwrapRedirectUrl(viewModel.Link.CommandRuns[0].Command.InnertubeCommand.UrlEndpoint.Url);

		public Thumbnail[] Favicon { get; } = viewModel.Favicon.Sources.Select(x => new Thumbnail
		{
			Url = x.Url,
			Width = x.Width,
			Height = x.Height
		}).ToArray();
	}
}