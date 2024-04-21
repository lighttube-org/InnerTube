using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
		BrowseResponse channel = await _innerTube.BrowseAsync(channelId, Utils.GetParams(channelTab));

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
				x.TabRenderer.Content?.ResultsContainer?.Results ?? x.TabRenderer.Content?.RichGridRenderer.Contents),
			RendererWrapper.RendererOneofCase.ExpandableTabRenderer => (x.ExpandableTabRenderer.Selected, []),
			_ => (false, [])
		});
		RepeatedField<RendererWrapper> selectedTab = tabs.FirstOrDefault(x => x.Selected).Results ?? [];
		foreach (RendererWrapper renderer in selectedTab)
			sb.AppendLine(Utils.SerializeRenderer(renderer));
		Assert.Pass(sb.ToString());
	}

	[TestCase("4qmFsgKrCBIYVUNjZC1HT3ZsOURkeVBWSFF4eTU4Yk93Go4IOGdhRUJocUJCbnItQlFyNUJRclFCVUZpTkZvdFEwVTBSVlJTVUc0eFJtbG9kRXhwYlRSMWJ6VlZiMHByTTFSRGRUWXlaSFJxV0d0V05HZExPWEJEZG5sQ1VFMHRYMDlJWDNOSWJFOTBUMmR2WWpsMlFXTnNXWGN4UjBGYVRtOWxPV1Z2ZDBGSWJqVmxkMWhGYzBSVk5FVnpSMnhZVFdSNFEwNTVRMWhDY2tocWNYSk9XVmRzWDJabVptaGhaV2RGV1hwQ1JWSjJlRE53VW5oVVowZzBVa2xHWm04MlF6aElXbkprYVdwM2FpMUZhVVV0ZFRsRFZtdGhjR3BJUVRKbFkweFdaR0kzU2pOTFJIRlZSR2xqT1VaMlJHOTZURk4xY1dKaFVUUndaMjVFV0dsdFpGQmlOakZaZEdFMVNXaFBORWRhWWxGWWRIUjJTRXh5UlRscVpuQmlWMjE0VWpkUWNsVkJXV3RmWDNWMFQyNU1ORFpCVEhWck1FaEtlbTVyTmpCc2FXSTJhVTlJTm5SSlNWSTRNMmRSTVZoWVZVNW9NamhtTkdVelVtZFZVMWRKV1ZKZlFtUlFaek5YUjNOQ1IwaFJiRXRSUjJOZmJFdFRTelpFVlZWSGVYTk9Va05wTkUwMGQyWlViamhUZDJsVVMxbzNia3hwUm1WNk5XbElZMDlxYmt0VGFXRldZM2R5Y0RCb2VVTTNVbmRsUVZadk4wRTFUVVozWTFGNGNFRmtXV1p2ZUdRMVVTMXpPVjlmZUZRMVJrbHpOVkoyWDB0MVVXcHdWa05aTVcxaGNuSXlZekl0VmxkUk16QnVWMDQwTFV4MmNUbHFkR1JpYlV4VmJYSm9XVk16TFhZNWJUbHRkRUZ0YUhsUWNWRmhPREJyYnpSWGMwaHdUMnRwVFdGdVZscFJabUpaYW14UlIybDVURWxHVWtnd1dFY3RSbFpuT0hoTFIxaE5ORzVUWDJKcVlWZEtWWEppTjNkT1ZtcGhaVUkyV0ZWalJqSkxPRWQ2V2pOeU1rbFFYeTFXY2pabVJYRmxPRTAxZUdNd2FraEdlbGRKZG5CVVh6WXpRbDkxZVMxM1pYUk9UREpqY1V4Q1RFUkhOMGhvVmpoaFNqWm1ZVWszTldkSFJFOXdkRWhtV0VWWU9EVm5ZMHB6WjFGaFNVdDVTSEp2TVZKemNFOXpXbkkwTmpOMVRsa3pSMnN4YWpBeVlsVkpXVEZLVDNOUmVXVldRMk5hWlU0NFMyNUNlbUZrTVVoUE5rRlNTMmhUYURkdlRrOHdWSFpyWTNoNWVWcEJieElrTmpZeVpUQmpOR1V0TURBd01DMHlZV0ZqTFdGak9XWXROVGd5TkRJNVkySXlaRFE0R0FFJTNE", TestName = "Continuation test #1")]
	public async Task ContinueChannel(string key)
	{
		BrowseResponse channel = await _innerTube.ContinueBrowseAsync(key);

		StringBuilder sb = new();
		foreach (RendererWrapper renderer in channel.OnResponseReceivedActions.AppendContinuationItemsAction
			         .ContinuationItems)
			sb.AppendLine(Utils.SerializeRenderer(renderer));

		Assert.Pass(sb.ToString());
	}
	
	[TestCase("PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", false, TestName = "Playlist")]
	[TestCase("VLPLiDvcIUGEFPv2K8h3SRrpc7FN7Ks0Z_A7", true, TestName = "Playlist with unavailable videos")]
	[TestCase("PLWA4fx92eWNstZbKK52BK9Ox-I4KvxdkF", false, TestName = "Intentionally empty playlist")]
	public async Task GetPlaylist(string playlistId, bool includeUnavailable)
	{
		BrowseResponse playlist =
			await _innerTube.BrowseAsync(playlistId.StartsWith("VL") ? playlistId : "VL" + playlistId,
				includeUnavailable ? "wgYCCAA%3D" : null);
		StringBuilder sb = new();
		
		sb.AppendLine("=== HEADER");
		PlaylistHeaderRenderer? header = playlist.Header?.PlaylistHeaderRenderer;
		if (header is null) sb.AppendLine("<null>");
		else
		{
			sb.AppendLine("Playlist ID: " + header.PlaylistId);
			sb.AppendLine("Title: " + Utils.ReadRuns(header.Title));
			sb.AppendLine("Description: " + Utils.ReadRuns(header.DescriptionText));
			sb.AppendLine("Owner: " + Utils.ReadRuns(header.OwnerText));
			sb.AppendLine("NumVideos: " + Utils.ReadRuns(header.NumVideosText));
			sb.AppendLine("ViewCount: " + Utils.ReadRuns(header.ViewCountText));
			sb.AppendLine("Privacy: " + header.Privacy);
			sb.AppendLine("Byline: " + string.Join(" | ", header.Byline.PlaylistBylineRenderer.Text.Select(x => Utils.ReadRuns(x))));
			sb.AppendLine("Gradient: LIGHT     DARK");
			foreach (GradientConfig config in header.CinematicContainer?.CinematicContainerRenderer.GradientColorConfig ?? [])
				sb.AppendLine($"-  [{Math.Round(config.StartLocation * 100).ToString().PadLeft(3, ' ')}%]" +
				              $" #{Convert.ToHexString(BitConverter.GetBytes(config.LightThemeColor)[..4])}" +
				              $" #{Convert.ToHexString(BitConverter.GetBytes(config.DarkThemeColor)[..4])}");
		}

		sb.AppendLine($"\n=== ALERTS ({playlist.Alerts.Count})");
		foreach (RendererWrapper renderer in playlist.Alerts)
			sb.AppendLine(Utils.SerializeRenderer(renderer));

		sb.AppendLine("\n=== CONTENTS");
		RepeatedField<RendererWrapper> selectedTab = playlist.Contents.TwoColumnBrowseResultsRenderer.Tabs
			.FirstOrDefault(x => x.TabRenderer.Selected)?.TabRenderer.Content.ResultsContainer.Results[0]
			.ItemSectionRenderer.Contents[0].PlaylistVideoListRenderer?.Contents ?? [];
		foreach (RendererWrapper renderer in selectedTab)
			sb.AppendLine(Utils.SerializeRenderer(renderer));
		Assert.Pass(sb.ToString());
	}

	[TestCase("VLPLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", 100)]
	public async Task ContinuePlaylist(string playlistId, int skipAmount)
	{
		BrowseResponse playlist =
			await _innerTube.ContinueBrowseAsync(Utils.PackPlaylistContinuation(playlistId, skipAmount));
		
		StringBuilder sb = new();
		foreach (RendererWrapper renderer in playlist.OnResponseReceivedActions.AppendContinuationItemsAction
			         .ContinuationItems)
			sb.AppendLine(Utils.SerializeRenderer(renderer));

		Assert.Pass(sb.ToString());
	}

	[TestCase("FEexplore")]
	[TestCase("FEwhat_to_watch")]
	public async Task Browse(string browseId)
	{
		BrowseResponse browse = await _innerTube.BrowseAsync(browseId);

	   //await File.WriteAllBytesAsync($"/home/kuylar/Projects/DotNet/InnerTube/Protobuf/{browseId}.bin", browse.ToByteArray());
		
		Assert.Pass(JsonSerializer.Serialize(browse, new JsonSerializerOptions
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = true
		}));
	}
}