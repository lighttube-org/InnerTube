using System.Collections.ObjectModel;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;

namespace InnerTube.Models;

public class InnerTubeChannel(BrowseResponse channel)
{
	public ChannelHeader? Header { get; } = channel.Header.RendererCase switch
	{
		RendererWrapper.RendererOneofCase.C4TabbedHeaderRenderer => new ChannelHeader(channel.Header
			.C4TabbedHeaderRenderer),
		RendererWrapper.RendererOneofCase.PageHeaderRenderer => new ChannelHeader(channel.Header.PageHeaderRenderer,
			channel.Metadata.ChannelMetadataRenderer.ExternalId),
		_ => null
	};

	public ReadOnlyCollection<ChannelTab> Tabs { get; } = channel.Contents.TwoColumnBrowseResultsRenderer.Tabs.Select(x =>
			x.RendererCase switch
			{
				RendererWrapper.RendererOneofCase.TabRenderer => new ChannelTab(x.TabRenderer),
				RendererWrapper.RendererOneofCase.ExpandableTabRenderer => new ChannelTab(x.ExpandableTabRenderer),
				_ => new ChannelTab(x.RendererCase.ToString())
			})
		.ToList()!
		.AsReadOnly();

	public ChannelMetadataRenderer Metadata { get; } = channel.Metadata.ChannelMetadataRenderer;

	public RendererContainer[] Contents { get; } = Utils.ConvertRenderers(channel.Contents
		.TwoColumnBrowseResultsRenderer.Tabs.Select(x =>
			x.RendererCase switch
			{
				RendererWrapper.RendererOneofCase.TabRenderer => (x.TabRenderer.Selected,
					x.TabRenderer.Content?.ResultsContainer?.Results ??
					x.TabRenderer.Content?.RichGridRenderer.Contents),
				RendererWrapper.RendererOneofCase.ExpandableTabRenderer => (x.ExpandableTabRenderer.Selected,
					x.ExpandableTabRenderer.Content.ResultsContainer.Results),
				_ => (false, [])
			}).FirstOrDefault(x => x.Selected).Item2 ?? []);
}