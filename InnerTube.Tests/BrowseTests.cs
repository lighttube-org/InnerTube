using System.Text;
using Google.Protobuf;
using Google.Protobuf.Collections;
using InnerTube.Exceptions;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;

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
	[TestCase("UCRS3ZUNqkEyTd9XZEphFRMA", (int)ChannelTabs.Home, null)]
	[TestCase("UCXuqSBlHAE6Xw-yeJA0Tunw", (int)ChannelTabs.Podcasts, null)]
	[TestCase("UC_kRDKYrUlrbtrSiyu5Tflg", (int)ChannelTabs.Releases, null)]
	[TestCase("UCcd-GOvl9DdyPVHQxy58bOw", (int)ChannelTabs.Store, null)]
	public async Task GetChannel(string channelId, ChannelTabs channelTab, string query)
	{
		BrowseResponse channel = await _innerTube.BrowseAsync(channelId);

		StringBuilder sb = new();

		sb.AppendLine("=== HEADER");
		C4TabbedHeaderRenderer? header = channel.Header?.C4TabbedHeaderRenderer;
		if (header is null) sb.AppendLine("<null>");
		else
		{
			sb.AppendLine("Channel ID: " + header.ChannelId);
			sb.AppendLine("Title: " + header.Title);
			sb.AppendLine("Handle: " + Utils.ReadRuns(header.ChannelHandleText));
			sb.AppendLine("Subscribers: " + Utils.ReadRuns(header.SubscriberCountText));
			sb.AppendLine("Videos: " + Utils.ReadRuns(header.VideosCountText));
			sb.AppendLine($"Tagline: {header.Tagline.ChannelTaglineRenderer.Content}");
			sb.AppendLine($"Avatar: ({header.Avatar.Thumbnails_.Count})" + string.Join("",
					header.Avatar.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"Banner: ({header.Banner.Thumbnails_.Count})" + string.Join("",
					header.Banner.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"MobileBanner: ({header.MobileBanner.Thumbnails_.Count})" + string.Join("",
					header.MobileBanner.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"TVBanner: ({header.TvBanner.Thumbnails_.Count})" + string.Join("",
					header.TvBanner.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"Badges: ({header.Badges.Count})\n- " + string.Join("",
				header.Badges.Select(x =>
					string.Join("\n  ", Utils.SerializeRenderer(x).Trim().Split("\n")))));
			sb.AppendLine("Links:");
			if (header.HeaderLinks.Count > 0)
			{
				sb.AppendLine("- First: " + Utils.ReadAttributedDescription(header.HeaderLinks[0].ChannelHeaderLinksViewModel.FirstLink, true));
				sb.AppendLine("- More: " + Utils.ReadAttributedDescription(header.HeaderLinks[0].ChannelHeaderLinksViewModel.More, true));
			}
			else
				sb.AppendLine("- No links");
		}

		sb.AppendLine("\n=== TABS");
		foreach (IMessage? message in channel.Contents.TwoColumnBrowseResultsRenderer.Tabs.Select(x => (IMessage)x.TabRenderer ?? x.ExpandableTabRenderer))
		{
			switch (message)
			{
				case TabRenderer tab:
					sb.AppendLine($"- {tab.Title} {(tab.Selected ? "(Selected)" : "")}");
					break;
				case ExpandableTabRenderer etab:
					sb.AppendLine($"- {etab.Title} {(etab.Selected ? "(Selected)" : "")}");
					break;
				default:
					sb.AppendLine("- Unexpected renderer: " + message.GetType().Name);
					break;
			}
		}

		sb.AppendLine("\n=== METADATA");
		ChannelMetadataRenderer metadata = channel.Metadata.ChannelMetadataRenderer;
		sb.AppendLine($"Title: {metadata.Title}");
		sb.AppendLine($"RssUrl: {metadata.RssUrl}");
		sb.AppendLine($"ExternalId: {metadata.ExternalId}");
		sb.AppendLine($"OwnerUrls: {string.Join(", ", metadata.OwnerUrls)}");
		sb.AppendLine($"Avatar: ({metadata.Avatar.Thumbnails_.Count})" + string.Join("",
			metadata.Avatar.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
		sb.AppendLine($"ChannelUrl: {metadata.ChannelUrl}");
		sb.AppendLine($"IsFamilySafe: {metadata.IsFamilySafe}");
		sb.AppendLine($"AvailableCountryCodes: ({metadata.AvailableCountryCodes.Count}) {string.Join(", ", metadata.AvailableCountryCodes)}");
		sb.AppendLine($"AndroidDeepLink: {metadata.AndroidDeepLink}");
		sb.AppendLine($"AndroidAppindexingLink: {metadata.AndroidAppindexingLink}");
		sb.AppendLine($"IosAppindexingLink: {metadata.IosAppindexingLink}");
		sb.AppendLine($"VanityChannelUrl: {metadata.VanityChannelUrl}");
		sb.AppendLine("\n=== CONTENTS");

		IEnumerable<(bool Selected, RepeatedField<RendererWrapper>? Results)> tabs =
			channel.Contents.TwoColumnBrowseResultsRenderer.Tabs.Select(x => x.RendererCase switch
		{
			RendererWrapper.RendererOneofCase.TabRenderer => (x.TabRenderer.Selected,
				x.TabRenderer.Content?.ResultsContainer.Results),
			RendererWrapper.RendererOneofCase.ExpandableTabRenderer => (x.ExpandableTabRenderer.Selected, []),
			_ => (false, [])
		});
		RepeatedField<RendererWrapper> selectedTab = tabs.FirstOrDefault(x => x.Selected).Results ?? [];
		foreach (RendererWrapper renderer in selectedTab)
			sb.AppendLine(Utils.SerializeRenderer(renderer));
		Assert.Pass(sb.ToString());
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