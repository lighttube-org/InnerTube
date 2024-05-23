using System.Diagnostics;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Collections;
using InnerTube.Exceptions;
using InnerTube.Formatters;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;

namespace InnerTube.Tests;

public class PlayerTests
{
	private InnerTube _innerTube;

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[TestCase("BaW_jenozKc", true, TestName = "Load a video with an HLS manifest")]
	[TestCase("J6Ga4wciA2k", true, TestName = "Load a video with the endscreen & info cards")]
	[TestCase("jfKfPfyJRdk", true, TestName = "Load a livestream")]
	[TestCase("9gIXoaB-Jik", true, TestName = "Video with WEBSITE endscreen item")]
	[TestCase("4ZX9T0kWb4Y", true, TestName = "Video with multiple audio tracks")]
	[TestCase("-UBaW1OIgTo", true, TestName = "EndScreenItem ctor")]
	[TestCase("UoBFuLMlDkw", true, TestName = "Video with cards")]
	[TestCase("Atvsg_zogxo", true, TestName = "Music video (Descramble result always throws an SSL error)")]
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
				.AppendLine("   AudioQuality: " + f.AudioQuality)
				.AppendLine("   AudioSampleRate: " + f.AudioSampleRate)
				.AppendLine("   AudioChannels: " + f.AudioChannels)
				.AppendLine("   AudioTrack: " + (f.AudioTrack?.ToString() ?? "<no audio track>"))
                .AppendLine("   SignatureCipher: " + f.SignatureCipher);
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
				.AppendLine("   AudioQuality: " + f.AudioQuality)
				.AppendLine("   AudioSampleRate: " + f.AudioSampleRate)
				.AppendLine("   AudioChannels: " + f.AudioChannels)
				.AppendLine("   AudioTrack: " + (f.AudioTrack?.ToString() ?? "<no audio track>"))
                .AppendLine("   SignatureCipher: " + f.SignatureCipher);
		}

		sb.AppendLine("== OTHER")
			.AppendLine("ExpiresInSeconds: " + player.StreamingData.ExpiresInSeconds)
			.AppendLine("HlsManifestUrl: " + player.StreamingData.HlsManifestUrl)
			.AppendLine("DashManifestUrl: " + player.StreamingData.DashManifestUrl);


		Assert.Pass(sb.ToString());
	}

	[TestCase("V6kJKxvbgZ0", true, false, TestName = "Age restricted video")]
	[TestCase("LACbVhgtx9I", false, false, TestName = "Video that includes self-harm topics")]
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

	[TestCase("BaW_jenozKc", TestName = "Regular video")]
	[TestCase("V6kJKxvbgZ0", TestName = "Age restricted video")]
	[TestCase("LACbVhgtx9I", TestName = "Video that includes self-harm topics")]
	[TestCase("Atvsg_zogxo", TestName = "something broke CompactPlaylistRenderer")]
	[TestCase("t6cZn-Fvwa0", TestName = "Video with comments disabled")]
	[TestCase("jPhJbKBuNnA", TestName = "Video with watchEndpoint in attributedDescription")]
	[TestCase("UoBFuLMlDkw", TestName = "Video with more special stuff in attributedDescription")]
	[TestCase("llrBX6FpMpM", TestName = "compactMovieRenderer")]
	[TestCase("jUUe6TuRlgU", TestName = "Chapters")]
	public async Task GetVideoNext(string videoId)
	{
		NextResponse next = await _innerTube.GetNextAsync(videoId, true, true);
		StringBuilder sb = new();

		RepeatedField<RendererWrapper> firstColumnResults =
			next.Contents.TwoColumnWatchNextResults.Results.ResultsContainer.Results;
		VideoPrimaryInfoRenderer primary = firstColumnResults.First(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.VideoPrimaryInfoRenderer).VideoPrimaryInfoRenderer;
		VideoSecondaryInfoRenderer secondary = firstColumnResults.First(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.VideoSecondaryInfoRenderer).VideoSecondaryInfoRenderer;
		RendererWrapper commentsSection = firstColumnResults.First(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.ItemSectionRenderer &&
			x.ItemSectionRenderer.SectionIdentifier.StartsWith("comment")).ItemSectionRenderer.Contents[0];
		CommentsEntryPointHeaderRenderer? commentsEntryPoint = commentsSection.CommentsEntryPointHeaderRenderer;
		MessageRenderer? commentsMessage = commentsSection.MessageRenderer;

		Utils.Formatter = new MarkdownFormatter();

		sb.AppendLine("== DETAILS");
		sb.AppendLine("Id: " + next.CurrentVideoEndpoint.WatchEndpoint.VideoId);
		sb.AppendLine("Title: " + Utils.ReadRuns(primary.Title));
		sb.AppendLine("Channel: " +
		              $"[{secondary.Owner.VideoOwnerRenderer.NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
		              Utils.ReadRuns(secondary.Owner.VideoOwnerRenderer.Title) +
		              $" ({Utils.ReadRuns(secondary.Owner.VideoOwnerRenderer.SubscriberCountText)})");
		sb.AppendLine("DateText: " + Utils.ReadRuns(primary.RelativeDateText));
		sb.AppendLine("ViewCount: " + (primary.ViewCount != null
			? primary.ViewCount.VideoViewCountRenderer.HasOriginalViewCount &&
			  primary.ViewCount.VideoViewCountRenderer.OriginalViewCount != 0
				? primary.ViewCount.VideoViewCountRenderer.OriginalViewCount.ToString()
				: Utils.ReadRuns(primary.ViewCount.VideoViewCountRenderer.ViewCount)
			: "0"));
		sb.AppendLine("LikeCount: " + primary.VideoActions.MenuRenderer.TopLevelButtons
			.First(x => x.RendererCase == RendererWrapper.RendererOneofCase.SegmentedLikeDislikeButtonViewModel)
			.SegmentedLikeDislikeButtonViewModel.LikeButtonViewModel.LikeButtonViewModel.ToggleButtonViewModel
			.ToggleButtonViewModel.DefaultButtonViewModel.ButtonViewModel2.Title);
		sb.AppendLine("Description:\n" + Utils.ReadAttributedDescription(secondary.AttributedDescription, true));

		sb.AppendLine("\n== COMMENTS");
		if (commentsEntryPoint != null)
		{
			CommentsEntryPointTeaserRenderer? teaserComment =
				commentsEntryPoint.ContentRenderer?.CommentsEntryPointTeaserRenderer;
			sb.AppendLine("CommentCount: " + Utils.ReadRuns(commentsEntryPoint.CommentCount));
			if (teaserComment == null) sb.AppendLine("TeaserComment: null");
			else
			{
				Thumbnail avatar = teaserComment.TeaserAvatar.Thumbnails_.First();
				sb.AppendLine("TeaserComment: ")
					.AppendLine($"  Thumbnail: [{avatar.Width}x{avatar.Height}] {avatar.Url}")
					.AppendLine("  Content: " + Utils.ReadRuns(teaserComment.TeaserContent));
			}
		}
		else if (commentsMessage != null)
		{
			sb.AppendLine("Message: " + Utils.ReadRuns(commentsMessage.Text));
		}

		sb.AppendLine("\n== CHAPTERS");
		MacroMarkersListRenderer? chapterEngagementPanel = next.EngagementPanels.FirstOrDefault(x =>
				x.EngagementPanelSectionListRenderer.TargetId == "engagement-panel-macro-markers-description-chapters")
			?.EngagementPanelSectionListRenderer?.Content?.MacroMarkersListRenderer;
		if (chapterEngagementPanel != null)
		{
			foreach (MacroMarkersListItemRenderer chapter in chapterEngagementPanel.Contents.Select(x =>
				         x.MacroMarkersListItemRenderer))
				sb.AppendLine(
					$"- [{TimeSpan.FromSeconds(chapter.OnTap.WatchEndpoint.StartTimeSeconds)}] {Utils.ReadRuns(chapter.Title)}");
		}
		else
		{
			sb.AppendLine("-> No chapters available");
		}

		sb.AppendLine("\n== RECOMMENDED");
		// NOTE: for age restricted videos, the first SecondaryResults is null
		if (next.Contents.TwoColumnWatchNextResults.SecondaryResults != null)
			foreach (RendererWrapper? renderer in next.Contents.TwoColumnWatchNextResults.SecondaryResults
				         .SecondaryResults.Results)
				sb.AppendLine("->\t" + string.Join("\n\t", Utils.SerializeRenderer(renderer).Split("\n")));
		else
			sb.AppendLine("-> No recommendations");

		Assert.Pass(sb.ToString());
	}

	[TestCase("3BR7-AzE2dQ", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null)]
	[TestCase("o0tky2O8NlY", "OLAK5uy_l6pEkEJgy577R-aDlJ3Gkp5rmlgIOu8bc", null, null)]
	[TestCase("NZwS7Cja6oE", "PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", null, null)]
	[TestCase("k_nLHgIM4yE", "PLv3TTBr1W_9tppikBxAE_G6qjWdBljBHJ", null, null)]
	public async Task GetVideoNextWithPlaylist(string videoId, string playlistId, int? playlistIndex,
		string? playlistParams)
	{
		NextResponse next =
			await _innerTube.GetNextAsync(videoId, true, true, playlistId, playlistIndex, playlistParams);
		if (next.Contents.TwoColumnWatchNextResults.Playlist == null)
		{
			Assert.Fail("Playlist is null");
			return;
		}

		Playlist playlist = next.Contents.TwoColumnWatchNextResults.Playlist.Playlist!;
		StringBuilder sb = new();
		sb.AppendLine($"[{playlist.PlaylistId}] {playlist.Title}")
			.AppendLine($"[{playlist.LongBylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
			            playlist.LongBylineText.Runs[0].Text)
			.AppendLine($"{playlist.CurrentIndex} ({playlist.LocalCurrentIndex}) / {playlist.TotalVideos}")
			.AppendLine($"IsCourse: {playlist.IsCourse}")
			.AppendLine($"IsInfinite: {playlist.IsInfinite}");

		sb.AppendLine()
			.AppendLine("== VIDEOS");

		foreach (RendererWrapper? renderer in playlist.Contents)
			sb.AppendLine("->\t" + string.Join("\n\t", Utils.SerializeRenderer(renderer).Split("\n")));

		Assert.Pass(sb.ToString());
	}

	[TestCase("1234567890a", TestName = "An ID I just made up")]
	[TestCase("a62882basgl", TestName = "Another ID I just made up")]
	[TestCase("32nkdvLq3oQ", TestName = "A deleted video")]
	[TestCase("mVp-gQuCJI8", TestName = "A private video")]
	public async Task DontGetVideoNext(string videoId)
	{
		try
		{
			await _innerTube.GetNextAsync(videoId, false, false);
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

	[TestCase("BaW_jenozKc", 0, TestName = "Regular video comments")]
	[TestCase("BaW_jenozKc", 1, TestName = "Regular video comments")]
	[TestCase("5UCz9i2K9gY", 0, TestName = "Has unescaped HTML tags")]
	[TestCase("quI6g4HpePc", 0, TestName = "Contains pinned & hearted comments")]
	[TestCase("kYwB-kZyNU4", 0, TestName = "Contains authors with badges")]
	[TestCase(
		"Eg0SC0JhV19qZW5vektjGAYy4QIKtwJnZXRfcmFua2VkX3N0cmVhbXMtLUNxY0JDSUFFRlJlMzBUZ2FuQUVLbHdFSTJGOFFnQVFZQnlLTUFYc2JQLW9iVGg1MkxWQnlUZklTWUh4TWlZSm9lQUJta2VKQUNNVnFNakUwQjlhMEl0S01SaFJXSFJIaU9XaUNpc19LY1BuVm1tRGVNLXRTMENyR0RPMFNwZE55WVZrUGYtdFJYVkFHT2ZBMmo4Smg2VXlTWERfZ2UxWkduYkVEcXlXTk9NdklBUk5RajlDQjZhSmJDS3FZdlVlQlNOeEkxdHQ1TVZRd2lXMmpvWG1tRnlwS0s0QVZ2M2dRRUJRU0JRaUlJQmdBRWdVSXFDQVlBQklGQ0ljZ0dBQVNCUWlKSUJnQUVnY0loU0FRRkJnQkVnY0lseUFRQ3hnQUdBQSIRIgtCYVdfamVub3pLYzAAeAEoFEIQY29tbWVudHMtc2VjdGlvbg%3D%3D",
		-1, TestName = "Continuation")]
	public async Task GetVideoComments(string videoId, int sortOrder)
	{
		NextResponse comments = sortOrder >= 0
			? await _innerTube.ContinueNextAsync(Utils.PackCommentsContinuation(videoId,
				(CommentsContext.Types.SortOrder)sortOrder))
			: await _innerTube.ContinueNextAsync(videoId);

		StringBuilder sb = new();
		foreach (Endpoint endpoint in comments.OnResponseReceivedEndpoints)
		{
			switch (endpoint.EndpointTypeCase)
			{
				case Endpoint.EndpointTypeOneofCase.ReloadContinuationItemsCommand:
					if (endpoint.ReloadContinuationItemsCommand.Slot == 1)
						foreach (RendererWrapper? renderer in endpoint.ReloadContinuationItemsCommand.ContinuationItems)
							sb.AppendLine(Utils.SerializeRenderer(renderer));
					break;
				case Endpoint.EndpointTypeOneofCase.AppendContinuationItemsAction:
					if (endpoint.AppendContinuationItemsAction.TargetId == "comments-section")
						foreach (RendererWrapper? renderer in endpoint.AppendContinuationItemsAction.ContinuationItems)
							sb.AppendLine(Utils.SerializeRenderer(renderer));
					break;
			}
		}

		Assert.Pass(sb.ToString());
	}

	[TestCase("there's no way they will accept this as a continuation key", false, TestName = "Self explanatory")]
	[TestCase("astISOttCQ0", true, TestName = "Video with comments disabled")]
	public async Task DontGetVideoComments(string continuationToken, bool isVideoId)
	{
		try
		{
			if (isVideoId)
				await _innerTube.ContinueNextAsync(Utils.PackCommentsContinuation(continuationToken,
					CommentsContext.Types.SortOrder.TopComments));
			else
				await _innerTube.ContinueNextAsync(continuationToken);
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
	}
}