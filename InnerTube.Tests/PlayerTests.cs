using System.Text;
using InnerTube.Exceptions;
using InnerTube.Renderers;

namespace InnerTube.Tests;

public class PlayerTests
{
	private InnerTube _innerTube;

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[TestCase("BaW_jenozKc", true, true, Description = "Load a video with an HLS manifest")]
	[TestCase("J6Ga4wciA2k", true, false, Description = "Load a video with the endscreen & info cards")]
	[TestCase("jfKfPfyJRdk", true, false, Description = "Load a livestream")]
	public async Task GetPlayer(string videoId, bool contentCheckOk, bool includeHls)
	{
		InnerTubePlayer player = await _innerTube.GetPlayerAsync(videoId, contentCheckOk, includeHls);
		StringBuilder sb = new();

		sb.AppendLine("== DETAILS")
			.AppendLine("Id: " + player.Details.Id)
			.AppendLine("Title: " + player.Details.Title)
			.AppendLine("Author: " + player.Details.Author)
			.AppendLine("Keywords: " + string.Join(", ", player.Details.Keywords.Select(x => $"#{x}")))
			.AppendLine("ShortDescription: " + player.Details.ShortDescription)
			.AppendLine("Length: " + player.Details.Length)
			.AppendLine("IsLive: " + player.Details.IsLive)
			.AppendLine("ViewCount: " + player.Details.ViewCount)
			.AppendLine("AllowRatings: " + player.Details.AllowRatings);

		sb.AppendLine("== STORYBOARD")
			.AppendLine("RecommendedLevel: " + player.Storyboard.RecommendedLevel);
		foreach ((int level, Uri? uri) in player.Storyboard.Levels) sb.AppendLine($"-> L{level}: {uri}");

		sb.AppendLine("== ENDSCREEN")
			.AppendLine("Start: " + TimeSpan.FromMilliseconds(player.Endscreen.StartMs));
		foreach (EndScreenItem item in player.Endscreen.Items)
		{
			sb
				.AppendLine($"-> [{item.Type}] Endscreen item")
				.AppendLine("   Target: " + item.Target)
				.AppendLine("   Title: " + item.Title)
				.AppendLine("   Image: " + item.Image.First().Url)
				.AppendLine("   Metadata: " + item.Metadata)
				.AppendLine("   Style: " + item.Style)
				.AppendLine("   AspectRatio: " + item.AspectRatio)
				.AppendLine("   Left: " + item.Left)
				.AppendLine("   Top: " + item.Top)
				.AppendLine("   Width: " + item.Width);
		}

		sb.AppendLine("== CAPTIONS");
		foreach (InnerTubePlayer.VideoCaption item in player.Captions)
		{
			sb
				.AppendLine($"-> [{item.LanguageCode}] {item.Label}")
				.AppendLine("   Url: " + item.BaseUrl);
		}

		sb.AppendLine("== FORMATS");
		foreach (Format f in player.Formats)
		{
			sb
				.AppendLine($"-> [{f.Itag}] {f.QualityLabel}")
				.AppendLine("   Bitrate: " + f.Bitrate)
				.AppendLine("   ContentLength: " + f.ContentLength)
				.AppendLine("   Fps: " + f.Fps)
				.AppendLine("   Height: " + f.Height)
				.AppendLine("   Width: " + f.Width)
				.AppendLine("   InitRange: " + f.InitRange)
				.AppendLine("   IndexRange: " + f.IndexRange)
				.AppendLine("   MimeType: " + f.MimeType)
				.AppendLine("   Url: " + f.Url)
				.AppendLine("   Quality: " + f.Quality)
				.AppendLine("   AudioQuality: " + f.AudioQuality)
				.AppendLine("   AudioSampleRate: " + f.AudioSampleRate)
				.AppendLine("   AudioChannels: " + f.AudioChannels);
		}

		sb.AppendLine("== ADAPTIVE FORMATS");
		foreach (Format f in player.AdaptiveFormats)
		{
			sb
				.AppendLine($"-> [{f.Itag}] {f.QualityLabel}")
				.AppendLine("   Bitrate: " + f.Bitrate)
				.AppendLine("   ContentLength: " + f.ContentLength)
				.AppendLine("   Fps: " + f.Fps)
				.AppendLine("   Height: " + f.Height)
				.AppendLine("   Width: " + f.Width)
				.AppendLine("   InitRange: " + f.InitRange)
				.AppendLine("   IndexRange: " + f.IndexRange)
				.AppendLine("   MimeType: " + f.MimeType)
				.AppendLine("   Url: " + f.Url)
				.AppendLine("   Quality: " + f.Quality)
				.AppendLine("   AudioQuality: " + f.AudioQuality)
				.AppendLine("   AudioSampleRate: " + f.AudioSampleRate)
				.AppendLine("   AudioChannels: " + f.AudioChannels);
		}

		sb.AppendLine("== OTHER")
			.AppendLine("ExpiresInSeconds: " + player.ExpiresInSeconds)
			.AppendLine("HlsManifestUrl: " + player.HlsManifestUrl)
			.AppendLine("DashManifestUrl: " + player.DashManifestUrl);


		Assert.Pass(sb.ToString());
	}

	[TestCase("V6kJKxvbgZ0", true, false, Description = "Age restricted video")]
	[TestCase("LACbVhgtx9I", false, false, Description = "Video that includes self-harm topics")]
	public void FailPlayer(string videoId, bool contentCheckOk, bool includeHls)
	{
		Assert.Catch(() =>
		{
			InnerTubePlayer _ = _innerTube.GetPlayerAsync(videoId, contentCheckOk, includeHls).Result;
		});
	}

	[TestCase("BaW_jenozKc", Description = "Regular video")]
	[TestCase("V6kJKxvbgZ0", Description = "Age restricted video")]
	[TestCase("LACbVhgtx9I", Description = "Video that includes self-harm topics")]
	public async Task GetVideoNext(string videoId)
	{
		InnerTubeNextResponse next = await _innerTube.GetVideoAsync(videoId);

		StringBuilder sb = new();

		sb.AppendLine("== DETAILS")
			.AppendLine("Id: " + next.Id)
			.AppendLine("Title: " + next.Title)
			.AppendLine("Channel: " + next.Channel)
			.AppendLine("DateText: " + next.DateText)
			.AppendLine("ViewCount: " + next.ViewCount)
			.AppendLine("LikeCount: " + next.LikeCount)
			.AppendLine("Description:\n" + string.Join('\n', next.Description.Split("\n").Select(x => $"\t{x}")));

		sb.AppendLine("\n== COMMENTS")
			.AppendLine("CommentCount: " + next.CommentCount)
			.AppendLine("TeaserComment: " + next.TeaserComment)
			.AppendLine("CommentsContinuation: " + next.CommentsContinuation);

		sb.AppendLine("\n== RECOMMENDED");
		foreach (IRenderer renderer in next.Recommended)
		{
			sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));
		}
		
		Assert.Pass(sb.ToString());
	}

	[TestCase("1234567890a", Description = "An ID I just made up")]
	[TestCase("a62882basgl", Description = "Another ID I just made up")]
	[TestCase("32nkdvLq3oQ", Description = "A deleted video")]
	[TestCase("mVp-gQuCJI8", Description = "A private video")]
	public async Task DontGetVideoNext(string videoId)
	{
		try
		{
			await _innerTube.GetVideoAsync(videoId);
		}
		catch (InnerTubeException e)
		{
			Assert.Pass($"Exception thrown: [{e.GetType().Name}] {e.Message}");
		}
		catch (Exception e)
		{
			Assert.Fail("Wrong type of exception has been thrown\n" + e);
		}

		Assert.Fail("Didn't throw an exception");
	}

	[TestCase("BaW_jenozKc", Description = "Regular video comments")]
	[TestCase("Eg0SC3F1STZnNEhwZVBjGAYyVSIuIgtxdUk2ZzRIcGVQYzAAeAKqAhpVZ3p3MnBIQXR1VW9xamRLbUtWNEFhQUJBZzABQiFlbmdhZ2VtZW50LXBhbmVsLWNvbW1lbnRzLXNlY3Rpb24%3D", Description = "Contains pinned & hearted comments")]
	[TestCase("Eg0SC2tZd0Ita1p5TlU0GAYyJSIRIgtrWXdCLWtaeU5VNDAAeAJCEGNvbW1lbnRzLXNlY3Rpb24%3D", Description = "Contains authors with badges")]
	public async Task GetVideoComments(string videoId)
	{
		InnerTubeContinuationResponse comments;
		if (videoId.Length == 11)
		{
			InnerTubeNextResponse next = await _innerTube.GetVideoAsync(videoId);
			if (next.CommentsContinuation is null) Assert.Fail("Video did not contain a comment continuation token");
			comments = await _innerTube.GetVideoCommentsAsync(next.CommentsContinuation!);
		}
		else
		{
			comments = await _innerTube.GetVideoCommentsAsync(videoId!);
		}
		StringBuilder sb = new();

		foreach (IRenderer renderer in comments.Contents) sb.AppendLine(renderer.ToString());

		sb.AppendLine($"\nContinuation: {comments.Continuation?.Substring(0, 20)}...");

		Assert.Pass(sb.ToString());
	}

	[TestCase("there's no way they will accept this as a continuation key", Description = "Self explanatory")]
	public async Task DontGetVideoComments(string continuationToken)
	{
		try
		{
			await _innerTube.GetVideoCommentsAsync(continuationToken);
		}
		catch (InnerTubeException e)
		{
			Assert.Pass($"Exception thrown: [{e.GetType().Name}] {e.Message}");
		}
		catch (Exception e)
		{
			Assert.Fail("Wrong type of exception has been thrown\n" + e);
		}

		Assert.Fail("Didn't throw an exception");
	}
}