using System.Text;
using InnerTube.Models;
using InnerTube.Protobuf.Responses;

namespace InnerTube.Tests;

public class SimpleInnerTubeClientTests
{
	private SimpleInnerTubeClient Client;
	
	[SetUp]
	public void Setup()
	{
		Client = new SimpleInnerTubeClient();
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
		InnerTubePlayer player = await Client.GetVideoPlayerAsync(videoId, contentCheckOk, "en", "US");

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
}