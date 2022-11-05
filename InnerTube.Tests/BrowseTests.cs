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
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Shorts, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Live, null)]
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

	[TestCase("4qmFsgKnARIYVUNGQWlGeUdzNm9EaUYxTmYtclJKcFpBGooBOGdaaUdtQnlYZ3BhQ2pKRlozTkpjMG95Y1dnMmFWY3RUR0pyUVZObmVVMUJSVFJJYTBsTlExQlFYMjF3YzBkRlVFUTRiWEkwUTFOQlJsRkJRUklrTmpNM1lqZzBZamt0TURBd01DMHlZMkprTFRreFpESXRNMk15T0Raa05EYzNZakl5R0FFJTNE")]
	public async Task ContinueChannel(string key)
	{
		InnerTubeContinuationResponse response = await _innerTube.ContinueChannelAsync(key);
		StringBuilder sb = new();
		
		foreach (IRenderer renderer in response.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		sb.AppendLine($"Continuation: {response.Continuation?.Substring(0, 20)}");
		
		Assert.Pass(sb.ToString());
	}

	[TestCase("PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", false)]
	[TestCase("VLPLiDvcIUGEFPv2K8h3SRrpc7FN7Ks0Z_A7", true)]
	public async Task GetPlaylist(string playlistId, bool includeUnavailable)
	{
		InnerTubePlaylist playlist = await _innerTube.GetPlaylistAsync(playlistId, includeUnavailable);

		StringBuilder sb = new();
		sb.AppendLine(playlist.Id);
		if (playlist.Alerts.Any())
		{
			sb.AppendLine()
				.AppendLine("/!\\ ALERTS");

			foreach (string alert in playlist.Alerts) 
				sb.AppendLine("->\t" + alert);
		}

		sb.AppendLine(playlist.Sidebar.ToString());
		
		foreach (PlaylistVideoRenderer renderer in playlist.Videos)
			sb.AppendLine("->\t" + string.Join("\n\t", renderer.ToString().Split("\n")));

		sb.AppendLine($"Continuation: {string.Join("", playlist.Continuation?.Take(20) ?? "")}...");
		
		Assert.Pass(sb.ToString());
	}

	[TestCase(
		"4qmFsgJhEiRWTFBMdjNUVEJyMVdfOXRwcGlrQnhBRV9HNnFqV2RCbGpCSEoaFENBRjZCbEJVT2tOSFp3JTNEJTNEmgIiUEx2M1RUQnIxV185dHBwaWtCeEFFX0c2cWpXZEJsakJISg%3D%3D")]
	public async Task ContinuePlaylist(string continuation)
	{
		InnerTubeContinuationResponse response = await _innerTube.ContinuePlaylistAsync(continuation);
		StringBuilder sb = new();
		
		foreach (IRenderer renderer in response.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		sb.AppendLine($"Continuation: {response.Continuation?.Substring(0, 20)}");
		
		Assert.Pass(sb.ToString());
	}

	[TestCase("UCfba251A_nwl141Ahgt16", Description = "Invalid ID")]
	public async Task FailChannel(string playlistId)
	{
		try
		{
			await _innerTube.GetChannelAsync(playlistId);
			Assert.Fail("Exception not thrown");
		}
		catch (InnerTubeException e)
		{
			Assert.Pass(e.ToString());
		}
		catch (Exception e)
		{
			Assert.Fail(e.ToString());
		}
	}

	[TestCase("q12f3g6ask2d5v71b4v7qıuysfqoh", Description = "Invalid ID")]
	[TestCase("PLiDvcIUGEFPsDSRP5ErtC90k-gtzs2bNQ", Description = "Private playlist")]
	public async Task FailPlaylist(string playlistId)
	{
		try
		{
			await _innerTube.GetPlaylistAsync(playlistId);
			Assert.Fail("Exception not thrown");
		}
		catch (InnerTubeException e)
		{
			Assert.Pass(e.ToString());
		}
		catch (Exception e)
		{
			Assert.Fail(e.ToString());
		}
	}

	[TestCase("FEexplore")]
	[TestCase("FEwhat_to_watch")]
	public async Task Browse(string browseId)
	{
		InnerTubeExploreResponse innerTubeExploreResponse = await _innerTube.BrowseAsync(browseId);

		StringBuilder sb = new($"[{innerTubeExploreResponse.BrowseId}]\n");
		sb.AppendLine("Contents:");
		sb.AppendLine(innerTubeExploreResponse.Contents.ToString());
		sb.AppendLine();
		sb.AppendLine("Header:");
		sb.AppendLine(innerTubeExploreResponse.Header.ToString());
		
		Assert.Pass(sb.ToString());
	}
}