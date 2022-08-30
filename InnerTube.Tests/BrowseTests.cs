using System.Text;
using InnerTube.Exceptions;
using InnerTube.Renderers;

namespace InnerTube.Tests;

public class BrowseTests
{
	private InnerTube _innerTube;

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Home, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Videos, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Playlists, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Community, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Channels, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.About, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Search, "skyblock")]
	public async Task GetChannel(string channelId, ChannelTabs tab, string query)
	{
		try
		{
			InnerTubeChannelResponse channel = await _innerTube.GetChannelAsync(channelId, tab, query);

			StringBuilder sb = new();

			sb
				.AppendLine("== HEADER")
				.AppendLine(channel.Header?.ToString() ?? "== NO HEADER ==")
				.AppendLine()
				.AppendLine("== METADATA")
				.AppendLine(channel.Metadata.ToString())
				.AppendLine()
				.AppendLine("== CONTENTS");

			foreach (IRenderer renderer in channel.Contents) 
				sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));
			Assert.Pass(sb.ToString());
		}
		catch (RequestException e)
		{
			Assert.Fail($"{e.Message}\n{e.JsonResponse}");
		}
	}

	[TestCase("4qmFsgKnARIYVUNGQWlGeUdzNm9EaUYxTmYtclJKcFpBGlxFZ1oyYVdSbGIzTVlBeUFBTUFFNEFlb0RNa1ZuYzBseFQxZHlhbGxwUTNFMlpWWkJVMmQ1VFVGRk5FaHJTVTFEU1hWTGRWcG5SMFZLV0VaM1MyTkVVMEZHVVVGQpoCLGJyb3dzZS1mZWVkVUNGQWlGeUdzNm9EaUYxTmYtclJKcFpBdmlkZW9zMTAy")]
	public async Task ContinueChannel(string key)
	{
		InnerTubeContinuationResponse response = await _innerTube.ContinueChannelAsync(key);
		StringBuilder sb = new();
		
		foreach (IRenderer renderer in response.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		sb.AppendLine($"Continuation: {response.Continuation?.Substring(0, 20)}");
		
		Assert.Pass(sb.ToString());
	}
}