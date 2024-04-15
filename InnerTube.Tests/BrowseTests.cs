using System.Text;
using InnerTube.Exceptions;

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
	[TestCase("@kuylardev", (int)ChannelTabs.Home, null)]
	[TestCase("@LinusTechTips", (int)ChannelTabs.Podcasts, null)]
	[TestCase("@daftpunk", (int)ChannelTabs.Releases, null)]
	[TestCase("@ZUTOMAYO", (int)ChannelTabs.Store, null)]
	public async Task GetChannel(string channelId, ChannelTabs tab, string query)
	{
		await _innerTube.BrowseAsync("UCcd-GOvl9DdyPVHQxy58bOw");
		/*try
		{
			InnerTubeChannelResponse channel = await _innerTube.GetChannelAsync(channelId, tab, query);

			StringBuilder sb = new();

			sb
				.AppendLine("== HEADER")
				.AppendLine(channel.Header?.ToString() ?? "== NO HEADER ==")
				.AppendLine()
				.AppendLine("== TABS")
				.AppendLine(string.Join(" | ", channel.EnabledTabs))
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
		}*/
	}
	/*
	[TestCase("4qmFsgLzCBIYVUNwZDUwSnpRQXVvcHdTR1FRQ21KSDh3GtYIOGdhNkJocTNCbnEwQmdxdkJncUdCa0ZXU21GdFJqSlJkRGxsU0VRdFQySjZTVlJVWVRsS1EzazVVRzVhVldsalRqY3dUM1JXVUMxS1lUTTJTblptZEZRd09HZGFjbUpUY1ZWQkxVZFZWRk5VY2xwdFdDMHRha1pIUXpac2NWUjFaSE5TVTFCWWRFczBRVkYwVVRWdVpWTmpObmhOY1Y5aGNYQklabG8zZFVkdU1FVTBPRW93TW5sb1ZtczNRVkpHVVMxWVluZFlTamhPWHpOTFIxcGlkbVJVTXpWdFQyaFBOVlZ0V0haQlNsOUNTR3BZTjBGVmVHaHpUVlJYTFd0MFVFcElXVTFJV1ZoclFuQnFkR2hpUTFsU2RsWnRUbTh4YXpWNFJpMVNSWE5GTkVoMFFucElMV2RoYWtSMWFVZDZNekJ4VXpkV2IwNUtVamQ1TWxsd1NHZEdOM2x2TXpSSmJGSkRhakp6WlhGc2J6QmtiRVJUZWtvemRUSkhhalpGTFc1NWVscExaV2RFWlc4d2JsSmZXakY0TjA1bE5HNUhURU5LVGs1SmJHeEtTVjl5UmpKQ2NsZERjV1l5VlhaUGVFNXpRVFZFV1VaSk9DMVBjSGRqZW01WVdVOUhjRXRGVVc1WlRIb3lTbFUzYmpsWVZpMU9hMFowZVVGdVVXWXdlbDlTUkRsUWRWQTVTRXB2U0ROclZtdEpNamRoTVVGUlRWcE1kMGQwZG5CRlpuRTFTRnBJUzFad05UWnNia0p5YVhobU5HcHZORWRpTTJVMk1uVmpNa05uUmxRNGRYUXlRVGhDYldkWmNtUmtlSGx5YlRsUmVGUk1WQzFKT1ZCSU5FMWlZVEpsZGtwV09ISnJla2htUTJkS1ZVcFFkREZ4UldOVlJIcExTSFZGZG5wcmRVbFNjVkp4YUhkWGVqbFhZa2RyZEdac0xYRk9RVlZ0TkRGc1ZFRTNlVzVHWldWUVprWkVSako2TW1JMlVYTlNiR0Z0VERJek1IQm1RbkZSWlhwNFVWTXpZelkwVW5KRU4weFVkblZHYzBaWlJFSkVhVk0wVEVVek15MDNhbk40WTIxRVlVUkxRMVZrUkdScVVrMXllRVZxUzJGME1tWmhTR2MwWVZwdFJGWjZZVmhYZURKb1QyNUtVek56ZDNkb09FWjRkWFY2Y1ZKTlFtWTFVbEp3Tm1aVWRVVnlZV1EyVEZwVlNFOXlNa2gyYUUxclIwNUxSRFZGUkY5U1p6QlVXRUpmUzFWVlpucG5WV0Z2YTFWbWRsQkllVXh3TFVSMGREbExlRWRpTFc5aGJWZFlaa2wyUzFwRFNHaHZlV1V5U0ZSeU5sVlFTMGRYU0dkSU1sWnljelZSWmtWWGRYVnZNRXBhU0VoMGIzRXdaeElrTmpVelltSTJPV1F0TURBd01DMHlOemswTFRnNE5UZ3ROVGd5TkRJNVl6WmhOMlZqR0FFJTNE")]
	public async Task ContinueChannel(string key)
	{
		InnerTubeContinuationResponse response = await _innerTube.ContinueChannelAsync(key);
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

	[TestCase("PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", false)]
	[TestCase("VLPLiDvcIUGEFPv2K8h3SRrpc7FN7Ks0Z_A7", true)]
	[TestCase("PLWA4fx92eWNstZbKK52BK9Ox-I4KvxdkF", false, Description = "Intentionally empty playlist")]
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

		sb.AppendLine($"Continuation: {string.Join("", playlist.Continuation?.ToString() ?? "<no continuation>")}");
		
		Assert.Pass(sb.ToString());
	}

	[TestCase("VLPLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", 100)]
	public async Task ContinuePlaylist(string playlistId, int skipAmount)
	{
		InnerTubeContinuationResponse response = await _innerTube.ContinuePlaylistAsync(playlistId, skipAmount);
		StringBuilder sb = new();
		
		foreach (IRenderer renderer in response.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		sb.AppendLine($"Continuation: {response.Continuation?.Substring(0, 20)}");
		
		Assert.Pass(sb.ToString());
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
	*/
}