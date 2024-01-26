using System.Diagnostics;
using System.Text;
using InnerTube.Exceptions;
using InnerTube.Protobuf.Renderers;
using InnerTube.Protobuf.Requests;

namespace InnerTube.Tests;

public class PlayerTests
{
	private InnerTube _innerTube;

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[TestCase("BaW_jenozKc", true, Description = "Load a video with an HLS manifest")]
	[TestCase("J6Ga4wciA2k", true, Description = "Load a video with the endscreen & info cards")]
	[TestCase("jfKfPfyJRdk", true, Description = "Load a livestream")]
	[TestCase("9gIXoaB-Jik", true, Description = "Video with WEBSITE endscreen item")]
	[TestCase("4ZX9T0kWb4Y", true, Description = "Video with multiple audio tracks")]
	[TestCase("-UBaW1OIgTo", true, Description = "EndScreenItem ctor")]
	[TestCase("UoBFuLMlDkw", true, Description = "Video with cards")]
	public async Task GetPlayer(string videoId, bool contentCheckOk)
	{
		PlayerResponse player = await _innerTube.GetPlayerAsync(videoId, contentCheckOk);
		StringBuilder sb = new();

		sb.AppendLine("== DETAILS")
			.AppendLine("Id: " + player.VideoDetails.VideoId)
			.AppendLine("Title: " + player.VideoDetails.Title)
			.AppendLine("Author: " + player.VideoDetails.Author)
			.AppendLine("Keywords: " + string.Join(", ", player.VideoDetails.Keywords.Select(x => $"#{x}")))
			.AppendLine("ShortDescription: " + player.VideoDetails.ShortDescription.Split('\n')[0])
			.AppendLine("Length: " + player.VideoDetails.LengthSeconds)
			.AppendLine("IsOwnerViewing: " + player.VideoDetails.IsOwnerViewing)
			.AppendLine("IsCrawlable: " + player.VideoDetails.IsCrawlable)
			.AppendLine("AllowRatings: " + player.VideoDetails.AllowRatings)
			.AppendLine("IsPrivate: " + player.VideoDetails.IsPrivate)
			.AppendLine("IsUnpluggedCorpus: " + player.VideoDetails.IsUnpluggedCorpus)
			.AppendLine("IsLiveContent: " + player.VideoDetails.IsLiveContent)
			.AppendLine("Thumbnails: " + player.VideoDetails.Thumbnail.Thumbnails_.Count);

		sb.AppendLine("== MICROFORMAT");
		if (player.Microformat != null)
			sb.AppendLine("Thumbnails: " + player.Microformat.PlayerMicroformatRenderer.Thumbnail.Thumbnails_.Count)
				.AppendLine("Embed: " + player.Microformat.PlayerMicroformatRenderer.Embed)
				.AppendLine("Title: " + player.Microformat.PlayerMicroformatRenderer.Title.SimpleText)
				.AppendLine("Description: " + player.Microformat.PlayerMicroformatRenderer.Description.SimpleText)
				.AppendLine("LengthSeconds: " + player.Microformat.PlayerMicroformatRenderer.LengthSeconds)
				.AppendLine("OwnerProfileUrl: " + player.Microformat.PlayerMicroformatRenderer.OwnerProfileUrl)
				.AppendLine("ExternalChannelId: " + player.Microformat.PlayerMicroformatRenderer.ExternalChannelId)
				.AppendLine("IsFamilySafe: " + player.Microformat.PlayerMicroformatRenderer.IsFamilySafe)
				.AppendLine("AvailableCountries: " +
				            string.Join(", ", player.Microformat.PlayerMicroformatRenderer.AvailableCountries))
				.AppendLine("IsUnlisted: " + player.Microformat.PlayerMicroformatRenderer.IsUnlisted)
				.AppendLine("HasYpcMetadata: " + player.Microformat.PlayerMicroformatRenderer.HasYpcMetadata)
				.AppendLine("ViewCount: " + player.Microformat.PlayerMicroformatRenderer.ViewCount)
				.AppendLine("Category: " + player.Microformat.PlayerMicroformatRenderer.Category)
				.AppendLine("PublishDate: " + player.Microformat.PlayerMicroformatRenderer.PublishDate)
				.AppendLine("OwnerChannelName: " + player.Microformat.PlayerMicroformatRenderer.OwnerChannelName)
				.AppendLine("UploadDate: " + player.Microformat.PlayerMicroformatRenderer.UploadDate);

		sb.AppendLine("== STORYBOARD");
		if (player.Storyboards != null)
		{
			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (player.Storyboards.RendererCase)
			{
				case RendererWrapper.RendererOneofCase.PlayerStoryboardSpecRenderer:
				{
					sb.AppendLine("RecommendedLevel: " +
					              player.Storyboards.PlayerStoryboardSpecRenderer.RecommendedLevel)
						.AppendLine("HighResolutionRecommendedLevel: " +
						            player.Storyboards.PlayerStoryboardSpecRenderer.HighResolutionRecommendedLevel)
						.AppendLine("Spec: " + player.Storyboards.PlayerStoryboardSpecRenderer.Spec);
					foreach ((int level, Uri? uri) in Utils.ParseStoryboardSpec(
						         player.Storyboards.PlayerStoryboardSpecRenderer.Spec,
						         player.VideoDetails.LengthSeconds))
					{
						sb.AppendLine($"-> L{level}: {uri}");
					}

					break;
				}

				case RendererWrapper.RendererOneofCase.PlayerLiveStoryboardSpecRenderer:
				{
					sb.AppendLine("[LIVE] Spec: " + player.Storyboards.PlayerLiveStoryboardSpecRenderer.Spec);
					sb.AppendLine(
						$"-> L0: {Utils.ParseLiveStoryboardSpec(player.Storyboards.PlayerLiveStoryboardSpecRenderer.Spec)}");

					break;
				}
			}
		}


		sb.AppendLine("== ENDSCREEN");
		if (player.Endscreen != null)
		{
			sb.AppendLine("Start: " + TimeSpan.FromMilliseconds(player.Endscreen.EndscreenRenderer.StartMs));
			foreach (EndscreenElementRenderer item in player.Endscreen.EndscreenRenderer.Elements.Select(x =>
				         x.EndscreenElementRenderer))
			{
				sb
					.AppendLine($"-> [{item.Style}] Endscreen item")
					.AppendLine("   Target: " + item.Endpoint)
					.AppendLine("   Title: " + item.Title)
					.AppendLine("   Image: " + item.Image.Thumbnails_.First().Url)
					.AppendLine("   Icon: " + item.Icon?.Thumbnails_.First().Url)
					.AppendLine("   Metadata: " + item.Metadata)
					.AppendLine("   Style: " + item.Style)
					.AppendLine("   AspectRatio: " + item.AspectRatio)
					.AppendLine("   Left: " + item.Left)
					.AppendLine("   Top: " + item.Top)
					.AppendLine("   Width: " + item.Width);
			}
		}

		sb.AppendLine("== CAPTIONS");
		if (player.Captions != null) // why doesnt protoc create a HasCaptions value????
			foreach (PlayerCaptionsTracklistRenderer.Types.Caption item in player.Captions.CaptionsTrackListRenderer
				         .Captions)
			{
				sb
					.AppendLine($"-> [{item.VssId}] ({item.Language}) {item.Name}")
					.AppendLine("   Url: " + item.BaseUrl)
					.AppendLine("   Kind: " + item.Kind);
			}

		sb.AppendLine("== FORMATS");
		foreach (Format f in player.StreamingData.Formats)
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
				.AppendLine("   MimeType: " + f.Mime)
				.AppendLine("   Url: " + f.Url)
				.AppendLine("   Quality: " + f.Quality)
				//.AppendLine("   AudioQuality: " + f.AudioQuality)
				.AppendLine("   AudioSampleRate: " + f.AudioSampleRate)
				.AppendLine("   AudioChannels: " + f.AudioChannels)
				.AppendLine("   AudioTrack: " + (f.AudioTrack?.ToString() ?? "<no audio track>"));
		}

		sb.AppendLine("== ADAPTIVE FORMATS");
		foreach (Format f in player.StreamingData.AdaptiveFormats)
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
				.AppendLine("   MimeType: " + f.Mime)
				.AppendLine("   Url: " + f.Url)
				.AppendLine("   Quality: " + f.Quality)
				//.AppendLine("   AudioQuality: " + f.AudioQuality)
				.AppendLine("   AudioSampleRate: " + f.AudioSampleRate)
				.AppendLine("   AudioChannels: " + f.AudioChannels)
				.AppendLine("   AudioTrack: " + (f.AudioTrack?.ToString() ?? "<no audio track>"));
		}

		sb.AppendLine("== OTHER")
			.AppendLine("ExpiresInSeconds: " + player.StreamingData.ExpiresInSeconds)
			.AppendLine("HlsManifestUrl: " + player.StreamingData.HlsManifestUrl)
			.AppendLine("DashManifestUrl: " + player.StreamingData.DashManifestUrl);


		Assert.Pass(sb.ToString());
	}

	[TestCase("V6kJKxvbgZ0", true, false, Description = "Age restricted video")]
	[TestCase("LACbVhgtx9I", false, false, Description = "Video that includes self-harm topics")]
	public async Task FailPlayer(string videoId, bool contentCheckOk, bool includeHls)
	{
		try
		{
			await _innerTube.GetPlayerAsync(videoId, contentCheckOk);
			Assert.Fail("No exceptions were thrown");
		}
		catch (PlayerException e)
		{
			Assert.Pass(e.ToString());
		}
		catch (Exception e)
		{
			Assert.Fail($"Wrong exception was thrown ({e.GetType().Name} instead of {nameof(PlayerException)}).\n{e}");
		}
	}

	[Test]
	public async Task CachePlayer()
	{
		StringBuilder sb = new();
		Stopwatch sp = Stopwatch.StartNew();
		await _innerTube.GetPlayerAsync("BaW_jenozKc", true);
		sb.AppendLine($"First request : {sp.ElapsedMilliseconds}ms");
		sp.Restart();
		await _innerTube.GetPlayerAsync("BaW_jenozKc", true);
		sb.AppendLine($"Second request: {sp.ElapsedMilliseconds}ms");
		Assert.Pass(sb.ToString());
	}

	[TestCase("BaW_jenozKc", Description = "Regular video")]
	[TestCase("V6kJKxvbgZ0", Description = "Age restricted video")]
	[TestCase("LACbVhgtx9I", Description = "Video that includes self-harm topics")]
	[TestCase("Atvsg_zogxo", Description = "something broke CompactPlaylistRenderer")]
	[TestCase("t6cZn-Fvwa0", Description = "Video with comments disabled")]
	[TestCase("jPhJbKBuNnA", Description = "Video with watchEndpoint in attributedDescription")]
	[TestCase("UoBFuLMlDkw", Description = "Video with more special stuff in attributedDescription")]
	[TestCase("llrBX6FpMpM", Description = "compactMovieRenderer")]
	[TestCase("jUUe6TuRlgU", Description = "Chapters")]
	public async Task GetVideoNext(string videoId)
	{
		/*
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

		sb.AppendLine("\n== CHAPTERS");
		if (next.Chapters != null)
		{
			foreach (ChapterRenderer chapter in next.Chapters)
				sb.AppendLine($"- [{TimeSpan.FromMilliseconds(chapter.TimeRangeStartMillis)}] {chapter.Title}");
		}
		else
		{
			sb.AppendLine("No chapters available");
		}

		sb.AppendLine("\n== COMMENTS")
			.AppendLine("CommentCount: " + next.CommentCount)
			.AppendLine("CommentsContinuation: " + next.CommentsContinuation);

		sb.AppendLine("\n== RECOMMENDED");
		foreach (IRenderer renderer in next.Recommended)
		{
			sb.AppendLine("->\t" + string.Join("\n\t",
				(renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));
		}

		Assert.Pass(sb.ToString());
		*/
	}

	[TestCase("CBQSKRILMlFPNDBIalJIMnfAAQDIAQDgAQOiAg0o____________AUAA-AIAGAAqmAsyczZMNnd5ckNBcW9DQW9EOGo0QUNnM0NQZ29JcFBXZDF1UzM1WmwwQ2czQ1Bnb0l0SXppd3YzZno2TmpDZzNDUGdvSTY1RHprY1hnOXR3RkNnN0NQZ3NJck1HcHRLaW04ZnVpQVFvT3dqNExDUGJSZzYtUHdfX2V4UUVLRHNJLUN3aXI4WnpSd3FEbjZMQUJDZzdDUGdzSTlvSDBpc245cDVYVkFRb093ajRMQ1BUMTA1anR3OEtDX3dFS0RjSS1DZ2pOM1l5aThQVFB1Z1VLRGNJLUNnakg5OTdSdTRqMjdoQUtEY0ktQ2dpOS1jR0c3N1NONGlBS0RzSS1Dd2o3cHA3bnVON3N2UFFCQ2c3Q1Bnc0luSnp3aktmeTVfamFBUW9Od2o0S0NNZU4yYnV4clpydmJBb053ajRLQ0lPcTA2LTM3c2VpQXdvTXdqNEpDTWJac2ZqeDlfNGxDZzNDUGdvSXZ1S0V1OEN4LS1rS0NnM0NQZ29JbkltaXk5ZTU5dkV6Q2c3Q1Bnc0kwSUwyd3B5dnNPV3FBUW9Od2o0S0NKZXVpSlQ4NHFXd0FRb053ajRLQ09MYzZjLV9ucC05U1FvT3dqNExDSXlRX19qWnJMVFk4QUVLQV9JLUFBb053ajRLQ1A3U3NJMnZzWUdZSEFvRDhqNEFDaF9TUGh3S0dsSkVSVTFKUVhSYU4zZFRibVZzTm1waFQyNDBjVjlwUjNWbkNnUHlQZ0FLRHNJLUN3akY1cHZXMGZ1bms4NEJDZ1B5UGdBS0RjSS1DZ2prc2RLbnFhM0FfbE1LQV9JLUFBb053ajRLQ0lQZTRlLUkwcUxpT1FvRDhqNEFDZzdDUGdzSXlkVC1nY3pPai1MS0FRb0Q4ajRBQ2czQ1Bnb0lxcC15cWZyQXpyaHRDZ1B5UGdBS0RzSS1Dd2lya2MzQ204LU56bzRCQ2dQeVBnQUtEc0ktQ3dpYzFLcjZ1TG1ELUw0QkNnUHlQZ0FLRHNJLUN3alA5ZUMzcVpxLXBPb0JDZ1B5UGdBS0RjSS1DZ2pzNmNHMzlPYXY5bmtLQV9JLUFBb093ajRMQ05qTHJ2REZ3WS1icUFFS0FfSS1BQW9Od2o0S0NLLTJ5YVdobGQzNVlBb0Q4ajRBQ2czQ1Bnb0lzcDNGbE5yaTJOUmpDZ1B5UGdBS0RzSS1Dd2lsbjlYSXBhT3JrdGNCQ2dQeVBnQUtEc0ktQ3dqcXRLdWExNm1ua3ZrQkNnUHlQZ0FLRGNJLUNnaXI3OWUzejVmUzYxUUtBX0ktQUFvT3dqNExDSzIxanRUNXFiLWctZ0VLQV9JLUFBb0owajRHQ2dSU1JFMU5DZ1B5UGdBS0RjSS1DZ2lxOThqNW1hM1dxblVTRlFBWEdSc2RIeUVqSlNjcEt5MHZNVE0xTnprN1BSb0VDQUFRQVJvRUNBQVFBaG9FQ0FBUUF4b0VDQUFRQkJvRUNBQVFCUm9FQ0FBUUJob0VDQUFRQnhvRUNBQVFDQm9FQ0FBUUNSb0VDQUFRQ2hvRUNBQVFDeG9FQ0FBUURCb0VDQUFRRFJvRUNBQVFEaG9FQ0FBUUR4b0VDQUFRRUJvRUNBQVFFUm9FQ0FBUUVob0VDQUFRRXhvRUNBQVFGQm9FQ0FBUUZSb0VDQUFRRmhvRUNCY1FHQm9FQ0JrUUdob0VDQnNRSEJvRUNCMFFIaG9FQ0I4UUlCb0VDQ0VRSWhvRUNDTVFKQm9FQ0NVUUpob0VDQ2NRS0JvRUNDa1FLaG9FQ0NzUUxCb0VDQzBRTGhvRUNDOFFNQm9FQ0RFUU1ob0VDRE1RTkJvRUNEVVFOaG9FQ0RjUU9Cb0VDRGtRT2hvRUNEc1FQQm9FQ0QwUVBpb1ZBQmNaR3gwZklTTWxKeWtyTFM4eE16VTNPVHM5ag93YXRjaC1uZXh0LWZlZWQ%3D")]
	public async Task ContinueVideoNext(string continuation)
	{
		InnerTubeContinuationResponse response = await _innerTube.ContinueVideoAsync(continuation);
		StringBuilder sb = new();
		
		foreach (IRenderer renderer in response.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t", (renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		sb.AppendLine($"Continuation: {response.Continuation?.Substring(0, 20)}");
		
		Assert.Pass(sb.ToString());
	}

	[TestCase("3BR7-AzE2dQ", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null)]
	[TestCase("o0tky2O8NlY", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null)]
	[TestCase("NZwS7Cja6oE", "PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", null, null)]
	[TestCase("k_nLHgIM4yE", "PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", null, null)]
	public async Task GetVideoNextWithPlaylist(string videoId, string playlistId, int? playlistIndex,
		string? playlistParams)
	{
		/*
		InnerTubeNextResponse next = await _innerTube.GetVideoAsync(videoId, playlistId, playlistIndex, playlistParams);
		if (next.Playlist is null)
		{
			Assert.Fail("Playlist is null");
			return;
		}

		StringBuilder sb = new();

		sb.AppendLine($"[{next.Playlist.PlaylistId}] {next.Playlist.Title}")
			.AppendLine($"{next.Playlist.Channel}")
			.AppendLine(
				$"{next.Playlist.CurrentIndex} ({next.Playlist.LocalCurrentIndex}) / {next.Playlist.TotalVideos}")
			.AppendLine($"IsCourse: {next.Playlist.IsCourse}")
			.AppendLine($"IsInfinite: {next.Playlist.IsInfinite}");

		sb.AppendLine()
			.AppendLine("== VIDEOS");

		foreach (PlaylistPanelVideoRenderer video in next.Playlist.Videos)
			sb.AppendLine(video.ToString());

		Assert.Pass(sb.ToString());
		*/
	}

	[TestCase("1234567890a", Description = "An ID I just made up")]
	[TestCase("a62882basgl", Description = "Another ID I just made up")]
	[TestCase("32nkdvLq3oQ", Description = "A deleted video")]
	[TestCase("mVp-gQuCJI8", Description = "A private video")]
	public async Task DontGetVideoNext(string videoId)
	{
		/*
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
		*/
	}

	[TestCase("BaW_jenozKc", Description = "Regular video comments")]
	[TestCase(
		"Eg0SC3F1STZnNEhwZVBjGAYyVSIuIgtxdUk2ZzRIcGVQYzAAeAKqAhpVZ3p3MnBIQXR1VW9xamRLbUtWNEFhQUJBZzABQiFlbmdhZ2VtZW50LXBhbmVsLWNvbW1lbnRzLXNlY3Rpb24%3D",
		Description = "Contains pinned & hearted comments")]
	[TestCase("Eg0SC2tZd0Ita1p5TlU0GAYyJSIRIgtrWXdCLWtaeU5VNDAAeAJCEGNvbW1lbnRzLXNlY3Rpb24%3D",
		Description = "Contains authors with badges")]
	[TestCase("5UCz9i2K9gY", Description = "Has unescaped HTML tags")]
	public async Task GetVideoComments(string videoId)
	{
		/*
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
		*/
	}

	[TestCase("BaW_jenozKc", Description = "Regular video comments")]
	public async Task GetVideoCommentsProtobuf(string videoId)
	{
		/*
		InnerTubeContinuationResponse comments =
			await _innerTube.GetVideoCommentsAsync(videoId, CommentsContext.Types.SortOrder.TopComments);

		StringBuilder sb = new();
		foreach (IRenderer renderer in comments.Contents) sb.AppendLine(renderer.ToString());
		sb.AppendLine($"\nContinuation: {comments.Continuation?[..20]}...");

		Assert.Pass(sb.ToString());
		*/
	}


	[TestCase("astISOttCQ0", Description = "Video with comments disabled")]
	public void DontGetVideoCommentsProtobuf(string videoId)
	{
		/*
		Assert.Catch(() =>
		{
			_ = _innerTube.GetVideoCommentsAsync(videoId, CommentsContext.Types.SortOrder.TopComments).Result;
		});
		*/
	}

	[TestCase("there's no way they will accept this as a continuation key", Description = "Self explanatory")]
	public async Task DontGetVideoComments(string continuationToken)
	{
		/*
		try
		{
			await _innerTube.GetVideoCommentsAsync(continuationToken);
		}
		catch (InnerTubeException e)
		{
			Assert.Pass($"Exception thrown: [{e.GetType().Name}] {e.Message}");
		}
		catch (ArgumentException e)
		{
			Assert.Pass($"Exception thrown: [{e.GetType().Name}] {e.Message}");
		}
		catch (Exception e)
		{
			Assert.Fail("Wrong type of exception has been thrown\n" + e);
		}

		Assert.Fail("Didn't throw an exception");
		*/
	}
}