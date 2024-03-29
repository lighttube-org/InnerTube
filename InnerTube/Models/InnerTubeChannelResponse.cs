﻿using InnerTube.Exceptions;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeChannelResponse
{
	public C4TabbedHeaderRenderer? Header { get; }
	public ChannelMetadataRenderer Metadata { get; }
	public IEnumerable<IRenderer> Contents { get; }
	public ChannelTabs[] EnabledTabs { get; }

	public InnerTubeChannelResponse(JObject browseResponse)
	{
		if (browseResponse.ContainsKey("alerts"))
			throw new NotFoundException(
				browseResponse.GetFromJsonPath<string>("alerts[0].alertRenderer.text.simpleText")!);
		Header = (C4TabbedHeaderRenderer?)RendererManager.ParseRenderer(
			browseResponse.GetFromJsonPath<JToken>("header.c4TabbedHeaderRenderer"), "c4TabbedHeaderRenderer");
		Metadata = (ChannelMetadataRenderer)RendererManager.ParseRenderer(
			browseResponse.GetFromJsonPath<JToken>("metadata.channelMetadataRenderer")!, "channelMetadataRenderer")!;
		JToken currentTab =
			browseResponse.GetFromJsonPath<JArray>("contents.twoColumnBrowseResultsRenderer.tabs")!
				.Select(x =>
					x.GetFromJsonPath<JToken>("tabRenderer.content.sectionListRenderer.contents") ??
					x.GetFromJsonPath<JToken>("tabRenderer.content.richGridRenderer.contents") ??
					x.GetFromJsonPath<JToken>("expandableTabRenderer.content"))
				.First(x => x != null)!;
		if (currentTab is JObject)
			currentTab = currentTab["sectionListRenderer"]!["contents"]!.ToObject<JToken>()!;
		EnabledTabs = browseResponse.GetFromJsonPath<JArray>("contents.twoColumnBrowseResultsRenderer.tabs")!
			.Select(tabRenderer => 
				Utils.GetTabFromParams(
					tabRenderer.GetFromJsonPath<string>("tabRenderer.endpoint.browseEndpoint.params") ?? ""))
			.ToArray();

		Contents = RendererManager.ParseRenderers(currentTab.ToObject<JArray>()!);
	}
}