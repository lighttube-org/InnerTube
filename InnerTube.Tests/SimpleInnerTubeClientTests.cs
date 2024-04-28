using System.Text;
using InnerTube.Models;
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
			sb.AppendLine($"StartMs: {player.Endscreen!.Value.StartMs}");
			foreach (EndscreenItem item in player.Endscreen!.Value.Items)
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
			sb.AppendLine($"RecommendedLevel: {player.Storyboard!.Value.RecommendedLevel}");
			foreach ((int level, Uri url) in player.Storyboard!.Value.Levels) 
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
	[TestCase("3BR7-AzE2dQ", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null, TestName = "[Playlist] Album playlist (index 1)")]
	[TestCase("o0tky2O8NlY", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null, TestName = "[Playlist] Album playlist (index 9)")]
	[TestCase("k_nLHgIM4yE", "PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", null, null, TestName = "[Playlist] Large playlist")]
	public async Task GetVideoDetailsAsync(string videoId, string? playlistId, int? playlistIndex,
		string? playlistParams)
	{
		InnerTubeVideo next = await client.GetVideoDetailsAsync(videoId, true, playlistId, playlistIndex, playlistParams, "en", "US");
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
				sb.AppendLine($"-> [{renderer.GetType().Name}] " + string.Join("\n\t",renderer.Data.ToString()));
		}
		else
			sb.AppendLine("No playlist available");

		sb.AppendLine("\n== RECOMMENDED");
		foreach (RendererContainer renderer in next.Recommended)
			sb.AppendLine($"-> [{renderer.GetType().Name}] " + string.Join("\n\t",renderer.Data.ToString()));

		Assert.Pass(sb.ToString());
	}
}