using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;

namespace InnerTube.Tests;

public class SimpleInnerTubeClientTests
{
	private SimpleInnerTubeClient client;

	[OneTimeSetUp]
	public void Setup()
	{
		client = new SimpleInnerTubeClient();
	}

	[TestCase("BaW_jenozKc", true, TestName = "Load a video with an HLS manifest")]
	[TestCase("J6Ga4wciA2k", true, TestName = "Load a video with the endscreen & info cards")]
	[TestCase("jfKfPfyJRdk", true, TestName = "Load a livestream")]
	[TestCase("9gIXoaB-Jik", true, TestName = "Video with WEBSITE endscreen item")]
	[TestCase("4ZX9T0kWb4Y", true, TestName = "Video with multiple audio tracks")]
	[TestCase("-UBaW1OIgTo", true, TestName = "EndScreenItem ctor")]
	[TestCase("dQw4w9WgXcQ", true, TestName = "EndScreenItem ctor 2")]
	public async Task GetVideoPlayerAsync(string videoId, bool contentCheckOk)
	{
		InnerTubePlayer player = await client.GetVideoPlayerAsync(videoId, contentCheckOk, "en", "US");

		StringBuilder sb = new();
		sb.AppendLine("\n=== DETAILS")
			.AppendLine($"Id: {player.Details.Id}")
			.AppendLine($"Title: {player.Details.Title}")
			.AppendLine($"Keywords: {player.Details.Keywords}")
			.AppendLine($"ShortDescription: {player.Details.ShortDescription}")
			.AppendLine($"Category: {player.Details.Category}")
			.AppendLine($"IsLive: {player.Details.IsLive}")
			.AppendLine($"AllowRatings: {player.Details.AllowRatings}")
			.AppendLine($"IsFamilySafe: {player.Details.IsFamilySafe}")
			.AppendLine($"Thumbnails: {player.Details.Thumbnails}");

		sb.AppendLine("\n=== OTHER METADATA")
			.AppendLine($"ExpiryTimeStamp: {player.ExpiryTimeStamp}")
			.AppendLine($"HlsManifestUrl: {player.HlsManifestUrl}")
			.AppendLine($"DashManifestUrl: {player.DashManifestUrl}");

		sb.AppendLine("\n=== ENDSCREEN");
		if (player.Endscreen == null) sb.AppendLine("<no endscreen>");
		else
		{
			sb.AppendLine($"StartMs: {player.Endscreen.StartMs}");
			foreach (EndscreenItem item in player.Endscreen.Items)
			{
				sb.AppendLine($"-> [{item.Type}] {item.Title}")
					.AppendLine($"   {item.Metadata}")
					.AppendLine($"   {item.Target}");
			}
		}

		sb.AppendLine("\n=== STORYBOARD");
		if (player.Storyboard == null) sb.AppendLine("<no storyboard>");
		else
		{
			sb.AppendLine($"RecommendedLevel: {player.Storyboard.RecommendedLevel}");
			foreach ((int level, Uri url) in player.Storyboard.Levels)
				sb.AppendLine($"-> [{level}] {url}");
		}

		sb.AppendLine("\n=== CAPTIONS");
		if (player.Captions.Length == 0) sb.AppendLine("<no captions>");
		else
			foreach (InnerTubePlayer.VideoCaption caption in player.Captions)
				sb.AppendLine($"-> [{caption.VssId}/{caption.LanguageCode}] {caption.Label}")
					.AppendLine($"   {caption.BaseUrl}");

		sb.AppendLine("\n=== FORMATS");
		if (player.Formats.Length == 0) sb.AppendLine("<no formats>");
		else
			foreach (Format format in player.Formats)
				sb.AppendLine($"-> [{format.Itag}]");

		sb.AppendLine("\n=== ADAPTIVE FORMATS");
		if (player.AdaptiveFormats.Length == 0) sb.AppendLine("<no formats>");
		else
			foreach (Format format in player.AdaptiveFormats)
				sb.AppendLine($"-> [{format.Itag}]");

		Assert.Pass(sb.ToString());
	}


	[TestCase("BaW_jenozKc", null, null, null, TestName = "Regular video")]
	[TestCase("V6kJKxvbgZ0", null, null, null, TestName = "Age restricted video")]
	[TestCase("LACbVhgtx9I", null, null, null, TestName = "Video that includes self-harm topics")]
	[TestCase("Atvsg_zogxo", null, null, null, TestName = "something broke CompactPlaylistRenderer")]
	[TestCase("t6cZn-Fvwa0", null, null, null, TestName = "Video with comments disabled")]
	[TestCase("jPhJbKBuNnA", null, null, null, TestName = "Video with watchEndpoint in attributedDescription")]
	[TestCase("UoBFuLMlDkw", null, null, null, TestName = "Video with more special stuff in attributedDescription")]
	[TestCase("llrBX6FpMpM", null, null, null, TestName = "compactMovieRenderer")]
	[TestCase("jUUe6TuRlgU", null, null, null, TestName = "Chapters")]
	[TestCase("3BR7-AzE2dQ", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null,
		TestName = "[Playlist] Album playlist (index 1)")]
	[TestCase("o0tky2O8NlY", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null,
		TestName = "[Playlist] Album playlist (index 9)")]
	[TestCase("k_nLHgIM4yE", "PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", null, null, TestName = "[Playlist] Large playlist")]
	public async Task GetVideoDetailsAsync(string videoId, string? playlistId, int? playlistIndex,
		string? playlistParams)
	{
		InnerTubeVideo next =
			await client.GetVideoDetailsAsync(videoId, true, playlistId, playlistIndex, playlistParams, "en", "US");
		StringBuilder sb = new();

		sb.AppendLine("== DETAILS")
			.AppendLine("Id: " + next.Id)
			.AppendLine("Title: " + next.Title)
			.AppendLine("Channel: " + next.Channel)
			.AppendLine("DateText: " + next.DateText)
			.AppendLine("ViewCount: " + next.ViewCountText)
			.AppendLine("LikeCountText: " + next.LikeCountText)
			.AppendLine("Description:\n" + string.Join('\n', next.Description.Split("\n").Select(x => $"\t{x}")));

		sb.AppendLine("\n== CHAPTERS");
		if (next.Chapters != null && next.Chapters.Any())
			foreach (VideoChapter chapter in next.Chapters)
				sb.AppendLine($"- [{TimeSpan.FromMilliseconds(chapter.StartSeconds)}] {chapter.Title}");
		else
			sb.AppendLine("No chapters available");

		sb.AppendLine("\n== COMMENTS")
			.AppendLine("CommentsCountText: " + (next.CommentsCountText ?? "<null>"))
			.AppendLine("CommentsErrorMessage: " + (next.CommentsErrorMessage ?? "<null>"));

		sb.AppendLine("\n== PLAYLIST");
		if (next.Playlist != null)
		{
			sb.AppendLine("PlaylistId: " + next.Playlist.PlaylistId);
			sb.AppendLine("Title: " + next.Playlist.Title);
			sb.AppendLine("TotalVideos: " + next.Playlist.TotalVideos);
			sb.AppendLine("CurrentIndex: " + next.Playlist.CurrentIndex);
			sb.AppendLine("LocalCurrentIndex: " + next.Playlist.LocalCurrentIndex);
			sb.AppendLine("Channel: " + next.Playlist.Channel);
			sb.AppendLine("IsCourse: " + next.Playlist.IsCourse);
			sb.AppendLine("IsInfinite: " + next.Playlist.IsInfinite);
			foreach (RendererContainer renderer in next.Playlist.Videos)
				sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
				              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		}
		else
			sb.AppendLine("No playlist available");

		sb.AppendLine("\n== RECOMMENDED");
		foreach (RendererContainer renderer in next.Recommended)
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));

		Assert.Pass(sb.ToString());
	}

	[TestCase(
		"CBQSFhILdDZjWm4tRnZ3YTDAAQHIAQH4AgEYACqbCDJzNkw2d3lOQmdxS0Jnb0Q4ajRBQ2h1YVB4Z0tGazk4TVRZeE9EazRNall3T1RRek1EVXlPRFUzT0RnS0dwb19Gd29WVDN3M09EUXpPVEl3TVRreE1qWTVOalUzTVRnMkNocWFQeGNLRlU5OE5EazVOVEkzTlRBME9UUXhPVE00TVRjMk9Rb2JtajhZQ2haUGZERXhPRFV3TlRreU1qZzFNakEzT1RRM01UZ3pDZ1B5UGdBS0RjSS1DZ2kxcXI2WjlweUtqa0FLQV9JLUFBb093ajRMQ1Bhb21NR0U3dE9PcXdFS0FfSS1BQW9Od2o0S0NNdmd0TzJ4NC1TdmZnb0Q4ajRBQ2c3Q1Bnc0l6ODN6bzdmeG5kWHBBUW9EOGo0QUNnM0NQZ29JcmVUcXpLUDFsX1FzQ2dQeVBnQUtEY0ktQ2dqdmhlM1d1clNyNjNvS0FfSS1BQW9Od2o0S0NJNjJxTXVUOHM3dE9Bb0Q4ajRBQ2czQ1Bnb0lsY09mXzV1RV9JOWhDZ1B5UGdBS0RjSS1DZ2lDbnFmNm1NQ2oza29LQV9JLUFBb053ajRLQ0pyXzJaSGcyT2JfSHdvRDhqNEFDZzNDUGdvSW1aRHQ1S3JWN094TkNnUHlQZ0FLRGNJLUNnalczTld2MnN1S3VWVUtBX0ktQUFvT3dqNExDSVhKNXJybTl2aWRoUUVLQV9JLUFBb053ajRLQ0lhUnhNMmt4SmlYUVFvRDhqNEFDZzdDUGdzSXY0VEg4NVBSNElyT0FRb0Q4ajRBQ2c3Q1Bnc0l5WnVYMExPTXZPckVBUW9EOGo0QUNnN0NQZ3NJdXU2dzJZU3F3dEhRQVFvRDhqNEFDZzNDUGdvSWhwckM4dHZEeV9KX0NnUHlQZ0FLRHNJLUN3ak5vdWZPNWMySHlmOEJDZ1B5UGdBS0RjSS1DZ2lWX1lLVTRZTEsyVFVLQV9JLUFBb0Q4ajRBRWhjQUJRY0pDdzBQRVJNVkZ4a2JIUjhoSXlVbktTc3RMaG9FQ0FBUUFSb0VDQUFRQWhvRUNBQVFBeG9FQ0FBUUJCb0VDQVVRQmhvRUNBY1FDQm9FQ0FrUUNob0VDQXNRREJvRUNBMFFEaG9FQ0E4UUVCb0VDQkVRRWhvRUNCTVFGQm9FQ0JVUUZob0VDQmNRR0JvRUNCa1FHaG9FQ0JzUUhCb0VDQjBRSGhvRUNCOFFJQm9FQ0NFUUlob0VDQ01RSkJvRUNDVVFKaG9FQ0NjUUtCb0VDQ2tRS2hvRUNDc1FMQm9FQ0MwUUFSb0VDQzBRQWhvRUNDMFFBeG9FQ0MwUUJCb0VDQzRRQVJvRUNDNFFBaG9FQ0M0UUF4b0VDQzRRQkNvWEFBVUhDUXNORHhFVEZSY1pHeDBmSVNNbEp5a3JMUzRqD3dhdGNoLW5leHQtZmVlZA%3D%3D",
		TestName = "Test #1")]
	public async Task ContinueVideoDetailsAsync(string token)
	{
		ContinuationResponse continuationResponse = await client.ContinueVideoRecommendationsAsync(token, "en", "US");

		StringBuilder sb = new("\n");
		sb.AppendLine("Continuation: " + (continuationResponse.ContinuationToken ?? "<null>"));
		sb.AppendLine("\n== ITEMS");
		foreach (RendererContainer renderer in continuationResponse.Results)
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));

		Assert.Pass(sb.ToString());
	}

	[TestCase("BaW_jenozKc", TestName = "Regular video comments")]
	[TestCase("5UCz9i2K9gY", TestName = "Has unescaped HTML tags")]
	[TestCase("quI6g4HpePc", TestName = "Contains pinned & hearted comments")]
	[TestCase("kYwB-kZyNU4", TestName = "Contains authors with badges")]
	public async Task GetVideoCommentsAsync(string token)
	{
		ContinuationResponse continuationResponse =
			await client.GetVideoCommentsAsync(token, CommentsContext.Types.SortOrder.TopComments);

		StringBuilder sb = new();
		sb.AppendLine("== ITEMS");
		foreach (RendererContainer renderer in continuationResponse.Results)
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		sb.AppendLine("\nContinuation: " + (continuationResponse.ContinuationToken ?? "<null>"));

		Assert.Pass(sb.ToString());
	}

	[TestCase(
		"Eg0SC0JhV19qZW5vektjGAYy1QIKqwJnZXRfcmFua2VkX3N0cmVhbXMtLUNxY0JDSUFFRlJlMzBUZ2FuQUVLbHdFSTJGOFFnQVFZQnlLTUFmdXlxRzg0ZWRpMVFNazF5ZUNyWTBvVVJNTmpRQXNVQjBJb1ZwUEpGQjA3UEM4alFCMFI0amxvUWhNckkwdXRDZWFOVDkySlZqRm1hS2w5U29UUmNvS3ZySWVfTlMtN0M4b2d2OTJqY0ZpV1A0T1FqX2dXd2pMSzAzWW9uRnJTaUxPTUhEQUI5UVNDRGt3WDZlZTRZc2g4ZjM4VmtadWVjakV2aGdvT3NkRUVacDZHOFVKRmFRWHd6eDRFRUJRU0JRaW9JQmdBRWdVSWlDQVlBQklGQ0ljZ0dBQVNCUWlKSUJnQUVnY0loU0FRRkJnQkdBQSIRIgtCYVdfamVub3pLYzAAeAEoFEIQY29tbWVudHMtc2VjdGlvbg%3D%3D",
		TestName = "Regular video comment continuation")]
	public async Task ContinueVideoCommentsAsync(string continuationToken)
	{
		ContinuationResponse continuationResponse = await client.ContinueVideoCommentsAsync(continuationToken);

		StringBuilder sb = new();
		sb.AppendLine("== ITEMS");
		foreach (RendererContainer renderer in continuationResponse.Results)
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		sb.AppendLine("\nContinuation: " + (continuationResponse.ContinuationToken ?? "<null>"));

		Assert.Pass(sb.ToString());
	}

	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Featured, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Videos, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Shorts, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Streams, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Playlists, null)]
	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", (int)ChannelTabs.Community, null)]
	[TestCase("UCRS3ZUNqkEyTd9XZEphFRMA", (int)ChannelTabs.Featured, null)]
	[TestCase("UCXuqSBlHAE6Xw-yeJA0Tunw", (int)ChannelTabs.Podcasts, null)]
	[TestCase("UC_kRDKYrUlrbtrSiyu5Tflg", (int)ChannelTabs.Releases, null)]
	[TestCase("UCcd-GOvl9DdyPVHQxy58bOw", (int)ChannelTabs.Store, null)]
	public async Task GetChannelAsync(string channelId, int channelTab, string searchQuery)
	{
		InnerTubeChannel channel = await client.GetChannelAsync(channelId, (ChannelTabs)channelTab);
		StringBuilder sb = new();
		sb.AppendLine("=== HEADER");
		ChannelHeader? header = channel.Header;
		if (header is null) sb.AppendLine("<null>");
		else
		{
			sb.AppendLine("Channel ID: " + header.Id);
			sb.AppendLine("Title: " + header.Title);
			sb.AppendLine("Handle: " + header.Handle);
			sb.AppendLine("Subscribers: " + header.SubscriberCountText);
			sb.AppendLine("Videos: " + header.VideoCountText);
			sb.AppendLine($"Tagline: {header.Tagline}");
			sb.AppendLine($"Avatar: ({header.Avatars.Length})" + string.Join("",
				header.Avatars.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"Banner: ({header.Banner.Length})" + string.Join("",
				header.Banner.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"MobileBanner: ({header.MobileBanner.Length})" + string.Join("",
				header.MobileBanner.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"TVBanner: ({header.TvBanner.Length})" + string.Join("",
				header.TvBanner.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"Badges: ({header.Badges.Length})\n- " + string.Join("",
				header.Badges.Select(x =>
					string.Join("\n  ", Utils.SerializeRenderer(new RendererWrapper
					{
						MetadataBadgeRenderer = x
					}).Trim().Split("\n")))));
			sb.AppendLine("Links:");
			sb.AppendLine("- First: " + (header.PrimaryLink ?? "<null>"));
			sb.AppendLine("- More: " + (header.SecondaryLink ?? "<null>"));
		}

		sb.AppendLine("\n=== TABS");
		foreach (ChannelTab tab in channel.Tabs)
			sb.AppendLine($"- [{tab.Tab}/{tab.Params}] {tab.Title} {(tab.Selected ? "(selected)" : "")}");

		sb.AppendLine("\n=== CONTENT");
		foreach (RendererContainer renderer in channel.Contents)
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));

		Assert.Pass(sb.ToString());
	}

	[TestCase(
		"4qmFsgKrCBIYVUNGQWlGeUdzNm9EaUYxTmYtclJKcFpBGo4IOGdhRUJocUJCbnItQlFyNUJRclFCVUZpTkZvdFEwaFpNRVI0VTJRMU4zVmZVVjk0WXpJeVdYQlhjR1JsVlhkcmJFSmxXWFJoVGpKTlgwNU1XRlpmZDJkQllXbFBXVXRUUlRoV2RrZGtNemswV2tRNU1teFJkMmRXTkZscVNsb3dSM0Y1U2poSVkwVnpSbk56Vm5GSFJsSmlWSGRYVkhZd2NGTTJWMjFXTkVjd1pWaFNiV3hGWVUxNVJYRk9UMjVtTkhaNVdVeGZVWEZGWjFwMGRFeENTbnBET1hsRlUxSlZUekpIZG0weVFVRlJNMjQ1Vm04Mk1tczRTa0ZZTTJnMkxYaFpWM2xJY21GMWJDMVpRMngyY0VaMlIyaEthRmc0UmtZeWVUSnNNRmR1TW5KR1ZGTmxaVkV0VW5kTmRWWlpTQzF2ZVdWMmVqRkRkamx1YlhjMVlXOHpNMFo0VlZkeVpUbHRhMlpxZVc1SVZtODRkVnB0VmxCa04zb3lTSGRuYlhsUGVXYzVWalJzVUVObWJuSjZXVmhpWjNselFUQjZVVnB4WlVsQmNpMTFiR0pST0RVd2EyVkVOblpQYjBaT05pMVVOR1k1VlhwdVVVODNabkZLVTAxbVoxRldXRmh6UjBwVlMwSXRiM0ZWYWpCSkxXdHJXbTVaWkZoclVGUTNaV2x1UjFseUxVcHBlVzVTZVY5MVYybGFNbmcxTW5oNWJtOUdOSGx3U21wZmJHZ3lhVU52ZEhSNU5FTkVRVzFvVlc5VWNYbFdjMmhYWm5SSU1GaDNZbWRSV0RoQmIwRmFRMkpvWVdsMVluWndhRVpUVFZOc1pVNUVhMmRpZFRoclkxUXdjbW8zVjBJd1dqaFNlSGR0VW1JMFREWlBVazlTVWt0T2RVOVdiRzVLVjBnM1pHOHdWbEJxVmtkQ2J6ZE5ZbGc0ZDBsT2VrTmhhR3RXU1daR1JESkhSbEpNV21oRlpVOVhRbWd5TTNwdFMxaHBXVjgwVG5GSlJGTnZhMnRGTUVJMVlYUTFUVkYzU0VKZk56bGtiVXBuWXpoWlgxQlRhRWxKTm5KU05WZzJWSEo1YkV4Mk1rMUhVbVZUZUc1emJHSmZTVk5IWW1acVNrWTRaM3BDTWxWNU5uVTJSVTVDTlU1RmFHdDNkbXMyZEVGRVJVZHFTMjlYYjNCWVNUWkhjVlJFT0ZwcVZGbzFYMjFVTWxad01GazVWbWgxWW1ab1ZtUkRURzFOTm13d1VXdzNiM1JzVm1oYVgycENOakJ2UWtkRlQybGhXVFk1VDI5QlUxRkdSUklrTmpneU5UWXdaakV0TURBd01DMHlZelEwTFRrME5qZ3ROVGd5TkRJNVpESXhORGM0R0FFJTNE",
		TestName = "Continuation test #1")]
	public async Task ContinueChannelAsync(string continuationToken)
	{
		ContinuationResponse continuationResponse = await client.ContinueChannelAsync(continuationToken);

		StringBuilder sb = new();
		sb.AppendLine("== ITEMS");
		foreach (RendererContainer renderer in continuationResponse.Results)
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));
		sb.AppendLine("\nContinuation: " + (continuationResponse.ContinuationToken ?? "<null>"));

		Assert.Pass(sb.ToString());
	}

	[TestCase("UCFAiFyGs6oDiF1Nf-rRJpZA", "skyblock")]
	public async Task SearchChannelAsync(string channelId, string query)
	{
		InnerTubeChannel channel = await client.SearchChannelAsync(channelId, query);
		StringBuilder sb = new();
		sb.AppendLine("=== HEADER");
		ChannelHeader? header = channel.Header;
		if (header is null) sb.AppendLine("<null>");
		else
		{
			sb.AppendLine("Channel ID: " + header.Id);
			sb.AppendLine("Title: " + header.Title);
			sb.AppendLine("Handle: " + header.Handle);
			sb.AppendLine("Subscribers: " + header.SubscriberCountText);
			sb.AppendLine("Videos: " + header.VideoCountText);
			sb.AppendLine($"Tagline: {header.Tagline}");
			sb.AppendLine($"Avatar: ({header.Avatars.Length})" + string.Join("",
				header.Avatars.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"Banner: ({header.Banner.Length})" + string.Join("",
				header.Banner.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"MobileBanner: ({header.MobileBanner.Length})" + string.Join("",
				header.MobileBanner.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"TVBanner: ({header.TvBanner.Length})" + string.Join("",
				header.TvBanner.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
			sb.AppendLine($"Badges: ({header.Badges.Length})\n- " + string.Join("",
				header.Badges.Select(x =>
					string.Join("\n  ", Utils.SerializeRenderer(new RendererWrapper
					{
						MetadataBadgeRenderer = x
					}).Trim().Split("\n")))));
			sb.AppendLine("Links:");
			sb.AppendLine("- First: " + (header.PrimaryLink ?? "<null>"));
			sb.AppendLine("- More: " + (header.SecondaryLink ?? "<null>"));
		}

		sb.AppendLine("\n=== TABS");
		foreach (ChannelTab tab in channel.Tabs)
			sb.AppendLine($"- [{tab.Tab}/{tab.Params}] {tab.Title} {(tab.Selected ? "(selected)" : "")}");

		sb.AppendLine("\n=== CONTENT");
		foreach (RendererContainer renderer in channel.Contents)
			sb.AppendLine($"-> [{renderer.Type} ({renderer.OriginalType})] [{renderer.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", renderer.Data.ToString()!.Split("\n")));

		Assert.Pass(sb.ToString());
	}
}