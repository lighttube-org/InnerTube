using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using Google.Protobuf;
using InnerTube.Formatters;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Params;
using InnerTube.Renderers;

namespace InnerTube;

public static partial class Utils
{
	public static IFormatter Formatter = new HtmlFormatter();
	private static readonly Regex NotDigitsRegex = GeneratedNotDigitsRegex();

	public static string ReadRuns(Text? text, bool includeFormatting = false)
	{
		if (text == null) return "";

		if (text.HasSimpleText) return Formatter.HandleLineBreaks(text.SimpleText);

		// TODO: check for .label
		/*
			if (richText.ContainsKey("label"))
				return richText["label"]!.ToString();
		 */

		if (text.Runs.Count <= 0) return text.ToString();
		
		string str = "";
		foreach (Text.Types.Run run in text.Runs)
		{
			if (!includeFormatting)
			{
				str += run.Text;
				continue;
			}

			string currentString = Formatter.Sanitize(run.Text);
			if (run.Bold)
				currentString = Formatter.FormatBold(currentString);

			if (run.NavigationEndpoint != null)
			{
				switch (run.NavigationEndpoint.EndpointTypeCase)
				{
					case Endpoint.EndpointTypeOneofCase.UrlEndpoint:
						string? target = UnwrapRedirectUrl(run.NavigationEndpoint.UrlEndpoint.Url);
						if (target != null)
							currentString = Formatter.FormatUrl(currentString, target);
						break;
					case Endpoint.EndpointTypeOneofCase.WatchEndpoint:
						string url = "https://youtube.com/watch?v=" + run.NavigationEndpoint.WatchEndpoint.VideoId;
						if (run.NavigationEndpoint.WatchEndpoint.HasPlaylistId)
							url += "&list=" + run.NavigationEndpoint.WatchEndpoint.PlaylistId;
						if (run.NavigationEndpoint.WatchEndpoint.HasPlayerParams)
							url += "&pp=" + run.NavigationEndpoint.WatchEndpoint.PlayerParams;
						if (run.NavigationEndpoint.WatchEndpoint.HasStartTimeSeconds)
							url += "&t=" + run.NavigationEndpoint.WatchEndpoint.StartTimeSeconds;
						currentString = Formatter.FormatUrl(currentString, url);
						break;
				}
			}
				
			str += currentString;
		}

		return Formatter.HandleLineBreaks(str);

	}

	public static string? UnwrapRedirectUrl(string url)
	{
		if (url.StartsWith("https://www.youtube.com/redirect"))
		{
			NameValueCollection qsl = HttpUtility.ParseQueryString(url.Split("?")[1]);
			url = qsl["url"] ?? qsl["q"] ?? url;
		}

		if (!url.StartsWith("http"))
			url = "https://" + url;

		return url;
	}

	public static Dictionary<int, Uri> ParseStoryboardSpec(string? specStr, long duration)
	{
		Dictionary<int, Uri> urls = new();
		if (specStr is null) return new Dictionary<int, Uri>();
		List<string> spec = new(specStr.Split("|"));
		string baseUrl = spec[0];
		spec.RemoveAt(0);
		spec.Reverse();
		int L = spec.Count - 1;
		for (int i = 0; i < spec.Count; i++)
		{
			string[] args = spec[i].Split("#");
			int width = int.Parse(args[0]);
			int height = int.Parse(args[1]);
			int frameCount = int.Parse(args[2]);
			int cols = int.Parse(args[3]);
			int rows = int.Parse(args[4]);
			string N = args[6];
			string sigh = args[7];
			string url = baseUrl
				.Replace("$L", (spec.Count - 1 - i).ToString())
				.Replace("$N", N) + "&sigh=" + sigh;
			float fragmentCount = frameCount / (cols * rows);
			float fragmentDuration = duration / fragmentCount;

			for (int j = 0; j < Math.Ceiling(fragmentCount); j++)
				urls.TryAdd(spec.Count - 1 - i, new Uri(url.Replace("$M", j.ToString())));
		}

		return urls;
	}

	public static Uri? ParseLiveStoryboardSpec(string? specStr) =>
		specStr is null ? null : new Uri(specStr.Replace("$M", "0"));

	public static TimeSpan ParseDuration(string duration)
	{
		if (!TimeSpan.TryParseExact(duration, "%m\\:%s", CultureInfo.InvariantCulture, out TimeSpan timeSpan))
			if (!TimeSpan.TryParseExact(duration, "%h\\:%m\\:%s",
				    CultureInfo.InvariantCulture, out timeSpan))
				timeSpan = TimeSpan.Zero;
		return timeSpan;
	}

	/// <summary>
	/// Parses a string to a <see cref="ulong"/> by removing all non-digit characters first.<br /><br />
	/// Negative numbers are not supported. If string contains no digits, returns 0.
	/// </summary>
	/// <param name="input">The string to parse, e.g. "10,341 views".</param>
	/// <returns>The number, e.g. 10341.</returns>
	/// <exception cref="OverflowException">
	/// The number is less than <see cref="ulong.MinValue"/> or greater than <see cref="ulong.MaxValue"/>.
	/// </exception> 
	public static ulong ParseNumber(string input)
	{
		input = NotDigitsRegex.Replace(input, "");
		return string.IsNullOrWhiteSpace(input)
			? 0
			: ulong.Parse(input);
	}

	public static string GetParams(this ChannelTabs tab) =>
		tab switch
		{
			ChannelTabs.Home => "EghmZWF0dXJlZA%3D%3D",
			ChannelTabs.Videos => "EgZ2aWRlb3PyBgQKAjoA",
			ChannelTabs.Shorts => "EgZzaG9ydHPyBgUKA5oBAA%3D%3D",
			ChannelTabs.Live => "EgdzdHJlYW1z8gYECgJ6AA%3D%3D",
			ChannelTabs.Playlists => "EglwbGF5bGlzdHM%3D",
			ChannelTabs.Podcasts => "Eghwb2RjYXN0c_IGBQoDugEA",
			ChannelTabs.Releases => "EghyZWxlYXNlc_IGBQoDsgEA",
			ChannelTabs.Community => "Egljb21tdW5pdHk%3D",
			ChannelTabs.Channels => "EghjaGFubmVscw%3D%3D",
			ChannelTabs.Store => "EgVzdG9yZfIGBAoCGgA%3D",
			ChannelTabs.About => "EgVhYm91dA%3D%3D",
			ChannelTabs.Search => "EgZzZWFyY2g%3D",
			var _ => ""
		};

	public static ChannelTabs GetTabFromParams(string param) =>
		// this method is starting to look slightly stupid with every new tab youtube adds
		string.Join("", param.Take(9)) switch
		{
			"EghmZWF0d" => ChannelTabs.Home,
			"EgZ2aWRlb" => ChannelTabs.Videos,
			"EgZzaG9yd" => ChannelTabs.Shorts,
			"EgdzdHJlY" => ChannelTabs.Live,
			"EglwbGF5b" => ChannelTabs.Playlists,
			"Eghwb2RjY" => ChannelTabs.Podcasts,
			"EghyZWxlY" => ChannelTabs.Releases,
			"Egljb21td" => ChannelTabs.Community,
			"EghjaGFub" => ChannelTabs.Channels,
			"EgVzdG9yZ" => ChannelTabs.Store,
			"EgVhYm91d" => ChannelTabs.About,
			"" => ChannelTabs.Search,
			var _ => ChannelTabs.Home
		};

	public static string ReadAttributedDescription(AttributedDescription? attributedDescription,
		bool includeFormatting = false)
	{
		if (string.IsNullOrEmpty(attributedDescription?.Content)) return "";

		string text = attributedDescription.Content ?? "";

		if (!includeFormatting) return text;
		if (attributedDescription.CommandRuns.Count == 0) return Formatter.HandleLineBreaks(text);

		foreach (AttributedDescription.Types.CommandRun run in attributedDescription
			         .CommandRuns.Reverse())
		{
			string replacement = Formatter.Sanitize(text.Substring(run.StartIndex, run.Length));
			switch (run.Command.InnertubeCommand.EndpointTypeCase)
			{
				case Endpoint.EndpointTypeOneofCase.UrlEndpoint:
					replacement = Formatter.FormatUrl(replacement,
						UnwrapRedirectUrl(run.Command.InnertubeCommand.UrlEndpoint.Url));
					break;

				case Endpoint.EndpointTypeOneofCase.WatchEndpoint:
					string url = "https://youtube.com/watch?v=" + run.Command.InnertubeCommand.WatchEndpoint.VideoId;
					if (run.Command.InnertubeCommand.WatchEndpoint.HasPlaylistId)
						url += "&list=" + run.Command.InnertubeCommand.WatchEndpoint.PlaylistId;
					if (run.Command.InnertubeCommand.WatchEndpoint.HasPlayerParams)
						url += "&pp=" + run.Command.InnertubeCommand.WatchEndpoint.PlayerParams;
					if (run.Command.InnertubeCommand.WatchEndpoint.HasStartTimeSeconds)
						url += "&t=" + run.Command.InnertubeCommand.WatchEndpoint.StartTimeSeconds;
					replacement = Formatter.FormatUrl(replacement, url);
					break;
				
				case Endpoint.EndpointTypeOneofCase.BrowseEndpoint:
					replacement = Formatter.FormatUrl(replacement,
						run.Command.InnertubeCommand.BrowseEndpoint.CanonicalBaseUrl);
					break;
			}

			text = text
				.Remove(run.StartIndex, run.Length)
				.Insert(run.StartIndex, replacement);
		}

		return Formatter.HandleLineBreaks(text);
	}

	public static string ToBase64UrlString(byte[] buffer) =>
		Convert.ToBase64String(buffer)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');

	public static byte[] FromBase64UrlString(string s)
	{
		string b64 = HttpUtility.UrlDecode(s);
		if (!b64.EndsWith('='))
			b64 = b64.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
		return Convert.FromBase64String(b64
			.Replace('-', '+')
			.Replace('_', '/'));
	}

	public static string PackProtobufInt(int value) =>
		ToBase64UrlString(new IntContainer { Value = value }.ToByteArray());

	public static int UnpackProtobufInt(string encoded) =>
		IntContainer.Parser.ParseFrom(FromBase64UrlString(encoded)).Value;

	// TODO: add "unavailable videos" flag
	public static string PackPlaylistContinuation(string playlistId, int skipAmount)
	{
		PaginationInfo info = new()
		{
			SkipAmountEncoded = $"PT:{PackProtobufInt(skipAmount)}"
		};

		PlaylistContinuationContainer container = new()
		{
			Continuation = new PlaylistContinuation
			{
				InternalPlaylistId = playlistId,
				PaginationInfo = ToBase64UrlString(info.ToByteArray()),
				PlaylistId = playlistId[2..]
			}
		};

		return ToBase64UrlString(container.ToByteArray());
	}

	public static PlaylistContinuationInfo UnpackPlaylistContinuation(string continuationKey)
	{
		PlaylistContinuationContainer container =
			PlaylistContinuationContainer.Parser.ParseFrom(FromBase64UrlString(continuationKey));
		PaginationInfo info =
			PaginationInfo.Parser.ParseFrom(FromBase64UrlString(container.Continuation.PaginationInfo));
		return new PlaylistContinuationInfo(container.Continuation.InternalPlaylistId,
			container.Continuation.PlaylistId, UnpackProtobufInt(info.SkipAmountEncoded.Split(":").Last()));
	}

	public static string PackCommentsContinuation(string videoId, CommentsContext.Types.SortOrder sortOrder)
	{
		CommentsContinuation continuation = new()
		{
			Something = 6,
			VideoId = new VideoIdContainer
			{
				Id = videoId
			},
			ContextContainer = new CommentsContextContainer
			{
				Context = new CommentsContext
				{
					VideoId = videoId,
					SortingOrder = sortOrder,
				},
				Source = "comments-section"
			}
		};

		return ToBase64UrlString(continuation.ToByteArray());
	}

	public static string SerializeRenderer(RendererWrapper? renderer)
	{
		if (renderer == null) return "[Renderer is null]";

		switch (renderer.RendererCase)
		{
			case RendererWrapper.RendererOneofCase.None:
				return $"[Unknown Renderer]\n{Convert.ToBase64String(renderer.ToByteArray())}";
			case RendererWrapper.RendererOneofCase.VideoRenderer:
			{
				VideoRenderer video = renderer.VideoRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[VideoRenderer] [{video.VideoId}] {ReadRuns(video.Title)}");
				sb.AppendLine($"Thumbnail: ({video.Thumbnail.Thumbnails_.Count})" + string.Join("",
					video.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine("Owner: " +
				              $"[{video.OwnerText?.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
				              video.OwnerText?.Runs[0].Text);
				sb.AppendLine($"OwnerBadges: ({video.OwnerBadges.Count})\n- " + string.Join("",
						video.OwnerBadges.Select(x =>
							$"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}"))}"))
					.TrimStart());
				sb.AppendLine("Duration: " + video.LengthText?.SimpleText);
				sb.AppendLine("ViewCount: " + video.ViewCountText.SimpleText);
				sb.AppendLine("ShortViewCount: " + video.ShortViewCountText.SimpleText);
				sb.AppendLine("PublishDate: " + video.PublishedTimeText?.SimpleText);
				sb.AppendLine($"Badges: ({video.Badges.Count})\n- " +
				              string.Join("",
						              video.Badges.Select(x =>
							              $"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}"))}"))
					              .TrimStart());
				sb.AppendLine(ReadRuns(video.DetailedMetadataSnippets?.SnippetText));
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.PlaylistVideoRenderer:
			{
				PlaylistVideoRenderer video = renderer.PlaylistVideoRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[PlaylistVideoRenderer] [{video.VideoId}] {ReadRuns(video.Title)}");
				sb.AppendLine($"Thumbnail: ({video.Thumbnail.Thumbnails_.Count})" + string.Join("",
					video.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine("Owner: " +
				              $"[{video.ShortBylineText?.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
				              video.ShortBylineText?.Runs[0].Text);
				sb.AppendLine("Duration: " + video.LengthSeconds);
				sb.AppendLine("VideoInfo: " + ReadRuns(video.VideoInfo));
				if (video.VideoInfo?.Runs.Count > 0)
				{
					sb.AppendLine("-> ViewCount: " + video.VideoInfo.Runs[0].Text);
					sb.AppendLine("-> PublishDate: " + video.VideoInfo.Runs[2].Text);
				}
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.CompactVideoRenderer:
			{
				CompactVideoRenderer video = renderer.CompactVideoRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[CompactVideoRenderer] [{video.VideoId}] {ReadRuns(video.Text)}")
					.AppendLine($"Thumbnail: ({video.Thumbnail.Thumbnails_.Count})" + string.Join("",
						video.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")))
					.AppendLine("Owner: " +
					            $"[{video.LongBylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
					            video.LongBylineText.Runs[0].Text)
					.AppendLine($"OwnerBadges: ({video.OwnerBadges.Count})\n- " + string.Join("",
							video.OwnerBadges.Select(x =>
								$"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}"))}"))
						.TrimStart())
					.AppendLine("Duration: " + video.LengthText?.SimpleText)
					.AppendLine("ViewCount: " + video.ViewCountText.SimpleText)
					.AppendLine("ShortViewCount: " + video.ShortViewCountText.SimpleText)
					.AppendLine("PublishDate: " + video.PublishedTimeText?.SimpleText)
					.AppendLine($"Badges: ({video.Badges.Count})\n- " +
					            string.Join("",
							            video.Badges.Select(x =>
								            $"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}"))}"))
						            .TrimStart());
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.GridVideoRenderer:
			{
				GridVideoRenderer video = renderer.GridVideoRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[GridVideoRenderer] [{video.VideoId}] {ReadRuns(video.Title)}");
				sb.AppendLine($"Thumbnail: ({video.Thumbnail.Thumbnails_.Count})" + string.Join("",
					video.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine("Owner: " +
				              $"[{video.ShortBylineText?.Runs?[0]?.NavigationEndpoint?.BrowseEndpoint?.BrowseId}] " +
				              video.ShortBylineText?.Runs?[0]?.Text);
				sb.AppendLine($"OwnerBadges: ({video.OwnerBadges.Count})\n- " + string.Join("",
						video.OwnerBadges.Select(x =>
							$"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}"))}"))
					.TrimStart());
				sb.AppendLine("Duration: " + ReadRuns(video.ThumbnailOverlays.FirstOrDefault(x =>
						x.RendererCase == RendererWrapper.RendererOneofCase.ThumbnailOverlayTimeStatusRenderer)
					?.ThumbnailOverlayTimeStatusRenderer.Text));
				sb.AppendLine("ViewCount: " + ReadRuns(video.ViewCountText));
				sb.AppendLine("ShortViewCount: " + ReadRuns(video.ShortViewCountText));
				sb.AppendLine("PublishDate: " + ReadRuns(video.PublishedTimeText));
				sb.AppendLine($"Badges: ({video.Badges.Count})\n- " +
				              string.Join("",
						              video.Badges.Select(x =>
							              $"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}"))}"))
					              .TrimStart());
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ChannelVideoPlayerRenderer:
			{
				ChannelVideoPlayerRenderer video = renderer.ChannelVideoPlayerRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[ChannelVideoPlayerRenderer] [{video.VideoId}] {ReadRuns(video.Title)}");
				sb.AppendLine("ViewCount: " + video.ViewCountText.SimpleText);
				sb.AppendLine("ShortViewCount: " + video.ViewCountText.SimpleText);
				sb.AppendLine("PublishDate: " + video.PublishedTimeText?.SimpleText);
				sb.AppendLine(ReadRuns(video.Description));
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.CompactRadioRenderer:
			{
				CompactRadioRenderer radio = renderer.CompactRadioRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[CompactRadioRenderer] [{radio.PlaylistId}] {ReadRuns(radio.Title)}")
					.AppendLine($"Thumbnail: ({radio.Thumbnail.Thumbnails_.Count})" + string.Join("",
						radio.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")))
					.AppendLine("Subtitle: " + ReadRuns(radio.LongBylineText))
					.AppendLine("VideoCount: " + radio.VideoCountText?.SimpleText)
					.AppendLine("VideoCountShort: " + radio.VideoCountShortText?.SimpleText);
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.CompactPlaylistRenderer:
			{
				CompactPlaylistRenderer playlist = renderer.CompactPlaylistRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[CompactPlaylistRenderer] [{playlist.PlaylistId}] {ReadRuns(playlist.Title)}")
					.AppendLine($"Thumbnail: ({playlist.Thumbnail.Thumbnails_.Count})" + string.Join("",
						playlist.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")))
					.AppendLine("Owner: " +
					            $"[{playlist.LongBylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
					            playlist.LongBylineText.Runs[0].Text)
					.AppendLine("VideoCount: " + playlist.VideoCountText?.SimpleText)
					.AppendLine("VideoCountShort: " + playlist.VideoCountShortText?.SimpleText);
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.CompactMovieRenderer:
			{
				CompactMovieRenderer movie = renderer.CompactMovieRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[CompactMovieRenderer] [{movie.VideoId}] {ReadRuns(movie.Title)}")
					.AppendLine($"Thumbnail: ({movie.Thumbnail.Thumbnails_.Count})" + string.Join("",
						movie.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")))
					.AppendLine("TopMetadataItems: " + ReadRuns(movie.TopMetadataItems))
					.AppendLine("Owner: " +
					            $"[{movie.ShortBylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
					            movie.ShortBylineText.Runs[0].Text)
					.AppendLine("Length: " + movie.LengthText?.SimpleText)
					.AppendLine($"Badges: ({movie.Badges.Count})\n" +
					            string.Join("\n",
							            movie.Badges.Select(x =>
								            $"- {string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}")).TrimStart()}"))
						            .TrimStart());
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ChannelRenderer:
			{
				ChannelRenderer channel = renderer.ChannelRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[ChannelRenderer] [{channel.ChannelId}] {ReadRuns(channel.Title)}")
					.AppendLine($"Thumbnail: ({channel.Thumbnail.Thumbnails_.Count})" + string.Join("",
						channel.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")))
					.AppendLine("VideoCountText*: " + ReadRuns(channel.VideoCountText))
					.AppendLine("SubscriberCountText*: " + ReadRuns(channel.SubscriberCountText))
					.AppendLine($"Badges: ({channel.OwnerBadges.Count})\n" +
					            string.Join("\n",
							            channel.OwnerBadges.Select(x =>
								            $"- {string.Join("\n", SerializeRenderer(x).Split("\n").Select(x => $"  {x}")).TrimStart()}"))
						            .TrimStart())
					.AppendLine("DescriptionSnippet:\n" + ReadRuns(channel.DescriptionSnippet));
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.MetadataBadgeRenderer:
			{
				MetadataBadgeRenderer badge = renderer.MetadataBadgeRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[MetadataBadgeRenderer]")
					.AppendLine("Icon: " + badge.Icon?.IconType ?? "<no icon>")
					.AppendLine("Style: " + badge.Style)
					.AppendLine("Label: " + badge.Label ?? "<no label>")
					.AppendLine("Tooltip: " + badge.Tooltip ?? "<no tooltip>");
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.PlaylistPanelVideoRenderer:
			{
				PlaylistPanelVideoRenderer video = renderer.PlaylistPanelVideoRenderer;
				StringBuilder sb = new();
				sb.AppendLine(
						$"[PlaylistPanelVideoRenderer] [{ReadRuns(video.IndexText)}] [{video.VideoId}] {ReadRuns(video.Title)}")
					.AppendLine($"Thumbnail: ({video.Thumbnail.Thumbnails_.Count})" + string.Join("",
						video.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")))
					.AppendLine("Owner: " +
					            $"[{video.LongBylineText.Runs[0].NavigationEndpoint.BrowseEndpoint.BrowseId}] " +
					            video.LongBylineText.Runs[0].Text)
					.AppendLine("Duration: " + video.LengthText?.SimpleText);
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.CommentThreadRenderer:
			{
				return "[CommentThreadRenderer] " +
				       string.Join("\n",
						       SerializeRenderer(renderer.CommentThreadRenderer.Comment).Split("\n")
							       .Select(x => $"  {x}"))
					       .TrimStart();
			}
			case RendererWrapper.RendererOneofCase.CommentRenderer:
			{
				CommentRenderer comment = renderer.CommentRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[CommentRenderer] [{comment.CommentId}] {ReadRuns(comment.AuthorText)}")
					.AppendLine(ReadRuns(comment.ContentText, true));
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ContinuationItemRenderer:
			{
				return "[ContinuationItemRenderer] " + renderer.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token;
			}
			case RendererWrapper.RendererOneofCase.MessageRenderer:
			{
				MessageRenderer message = renderer.MessageRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[MessageRenderer] {ReadRuns(message.Text)}");
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.BackgroundPromoRenderer:
			{
				BackgroundPromoRenderer bpr = renderer.BackgroundPromoRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[BackgroundPromoRenderer] {ReadRuns(bpr.Text)}");
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.DidYouMeanRenderer:
			{
				DidYouMeanRenderer dymr = renderer.DidYouMeanRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[DidYouMeanRenderer] \"{ReadRuns(dymr.DidYouMean)}\" \"{ReadRuns(dymr.CorrectedQuery)}\"");
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ShowingResultsForRenderer:
			{
				ShowingResultsForRenderer srfr = renderer.ShowingResultsForRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[ShowingResultsForRenderer] \"{ReadRuns(srfr.ShowingResultsFor)}\" \"{ReadRuns(srfr.CorrectedQuery)}\"");
				sb.AppendLine($"\"{ReadRuns(srfr.SearchInsteadFor)}\" \"{ReadRuns(srfr.OriginalQuery)}\"");
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ShelfRenderer:
			{
				ShelfRenderer shelf = renderer.ShelfRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[ShelfRenderer] {ReadRuns(shelf.Title)}");
				IEnumerable<RendererWrapper> items = shelf.Content.RendererCase switch
				{
					RendererWrapper.RendererOneofCase.VerticalListRenderer => shelf.Content.VerticalListRenderer.Items,
					RendererWrapper.RendererOneofCase.HorizontalListRenderer => shelf.Content.HorizontalListRenderer.Items,
					_ =>
					[
						new RendererWrapper
						{
							MessageRenderer = new MessageRenderer
							{
								Text = new Text
								{
									SimpleText = $"INNERTUBE: Unknown RendererCase: {shelf.Content.RendererCase}"
								}
							}
						}
					]
				};
				foreach (RendererWrapper item in items) 
					sb.AppendLine(string.Join("\n", SerializeRenderer(item).Split("\n").Select(x => $"  {x}")));
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ReelShelfRenderer:
			{
				ReelShelfRenderer reelShelf = renderer.ReelShelfRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[ReelShelfRenderer] {ReadRuns(reelShelf.Title)}");
				foreach (RendererWrapper item in reelShelf.Items) 
					sb.AppendLine(string.Join("\n", SerializeRenderer(item).Split("\n").Select(x => $"  {x}")));
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ReelItemRenderer:
			{
				ReelItemRenderer reel = renderer.ReelItemRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[ReelItemRenderer] [{reel.VideoId}] {ReadRuns(reel.Headline)}");
				sb.AppendLine($"Thumbnail: ({reel.Thumbnail.Thumbnails_.Count})" + string.Join("",
					reel.Thumbnail.Thumbnails_.Select(x => $"\n- [{x.Width}x{x.Height}] {x.Url}")));
				sb.AppendLine("ViewCount: " + reel.ViewCountText.SimpleText);
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.ItemSectionRenderer:
				return "[ItemSectionRenderer]\n" + string.Join('\n',
					renderer.ItemSectionRenderer.Contents.Select(x =>
						string.Join('\n', SerializeRenderer(x).Split("\n").Select(x => $"  {x}"))));
			case RendererWrapper.RendererOneofCase.RichItemRenderer:
				return "[RichItemRenderer] " + SerializeRenderer(renderer.RichItemRenderer.Content);
			case RendererWrapper.RendererOneofCase.AlertWithButtonRenderer:
			{
				return "[AlertWithButtonRenderer] [" + renderer.AlertWithButtonRenderer.Type + "] " + ReadRuns(renderer.AlertWithButtonRenderer.Text);
			}
			default:
				return $"[Unknown RendererCase={renderer.RendererCase}]";
		}
	}

	public static RendererContainer[] ConvertRenderers(IEnumerable<RendererWrapper>? renderers) =>
		renderers?.Select(ConvertRenderer).ToArray() ?? [];

	public static RendererContainer ConvertRenderer(RendererWrapper renderer)
	{
		return renderer.RendererCase switch
		{
			RendererWrapper.RendererOneofCase.None => new RendererContainer
			{
				Type = "unknown",
				OriginalType = "UnknownProtobufRenderer",
				Data = new UnknownRendererData
				{
					ProtobufBytes = renderer.ToByteArray()
				}
			},
			RendererWrapper.RendererOneofCase.VideoRenderer => new RendererContainer
			{
				Type = "video",
				OriginalType = "videoRenderer",
				Data = new VideoRendererData
				{
					Id = renderer.VideoRenderer.VideoId,
					Title = ReadRuns(renderer.VideoRenderer.Title),
					Thumbnails = renderer.VideoRenderer.Thumbnail.Thumbnails_.ToArray(),
					Author = Channel.From(renderer.VideoRenderer.OwnerText,
						renderer.VideoRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray()),
					Duration = ParseDuration(renderer.VideoRenderer.LengthText?.SimpleText ?? "00:00"),
					PublishedText = ReadRuns(renderer.VideoRenderer.PublishedTimeText),
					ViewCountText = ReadRuns(renderer.VideoRenderer.ViewCountText),
					Badges = renderer.VideoRenderer.Badges.Select(x => x.MetadataBadgeRenderer).ToArray(),
					Description = ReadRuns(renderer.VideoRenderer.DetailedMetadataSnippets?.SnippetText)
				}
			},
			RendererWrapper.RendererOneofCase.PlaylistVideoRenderer => new RendererContainer
			{
				Type = "video",
				OriginalType = "playlistVideoRenderer",
				Data = new PlaylistVideoRendererData
				{
					Id = renderer.PlaylistVideoRenderer.VideoId,
					Title = ReadRuns(renderer.PlaylistVideoRenderer.Title),
					Thumbnails = renderer.PlaylistVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
					Author = Channel.From(renderer.PlaylistVideoRenderer.ShortBylineText),
					Duration = ParseDuration(renderer.PlaylistVideoRenderer.LengthText?.SimpleText ?? "00:00"),
					PublishedText = renderer.PlaylistVideoRenderer.VideoInfo?.Runs.Count > 0
						? renderer.PlaylistVideoRenderer.VideoInfo.Runs[2].Text
						: "",
					ViewCountText = renderer.PlaylistVideoRenderer.VideoInfo?.Runs.Count > 0
						? renderer.PlaylistVideoRenderer.VideoInfo.Runs[0].Text
						: "",
					Badges = [],
					Description = null,
					VideoIndexText = ReadRuns(renderer.PlaylistVideoRenderer.Index)
				}
			},
			RendererWrapper.RendererOneofCase.PlaylistPanelVideoRenderer => new RendererContainer
			{
				Type = "video",
				OriginalType = "playlistPanelVideoRenderer",
				Data = new PlaylistVideoRendererData
				{
					Id = renderer.PlaylistPanelVideoRenderer.VideoId,
					Title = ReadRuns(renderer.PlaylistPanelVideoRenderer.Title),
					Thumbnails = renderer.PlaylistPanelVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
					Author = Channel.From(renderer.PlaylistPanelVideoRenderer.ShortBylineText),
					Duration = ParseDuration(renderer.PlaylistPanelVideoRenderer.LengthText?.SimpleText ?? "00:00"),
					PublishedText = "",
					ViewCountText = "",
					Badges = [],
					Description = null,
					VideoIndexText = ReadRuns(renderer.PlaylistPanelVideoRenderer.IndexText)
				}
			},
			RendererWrapper.RendererOneofCase.CompactVideoRenderer => new RendererContainer
			{
				Type = "video",
				OriginalType = "compactVideoRenderer",
				Data = new VideoRendererData
				{
					Id = renderer.CompactVideoRenderer.VideoId,
					Title = ReadRuns(renderer.CompactVideoRenderer.Text),
					Thumbnails = renderer.CompactVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
					Author = Channel.From(renderer.CompactVideoRenderer.LongBylineText,
						renderer.CompactVideoRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray()),
					Duration = ParseDuration(renderer.CompactVideoRenderer.LengthText?.SimpleText ?? "00:00"),
					PublishedText = ReadRuns(renderer.CompactVideoRenderer.PublishedTimeText),
					ViewCountText = ReadRuns(renderer.CompactVideoRenderer.ViewCountText),
					Badges = renderer.CompactVideoRenderer.Badges.Select(x => x.MetadataBadgeRenderer).ToArray(),
					Description = null
				}
			},
			RendererWrapper.RendererOneofCase.GridVideoRenderer => new RendererContainer
			{
				Type = "video",
				OriginalType = "gridVideoRenderer",
				Data = new VideoRendererData
				{
					Id = renderer.GridVideoRenderer.VideoId,
					Title = ReadRuns(renderer.GridVideoRenderer.Title),
					Thumbnails = renderer.GridVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
					Author = Channel.From(renderer.GridVideoRenderer.ShortBylineText,
						renderer.GridVideoRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray()),
					Duration = ParseDuration(ReadRuns(renderer.GridVideoRenderer.ThumbnailOverlays
						.FirstOrDefault(x =>
							x.RendererCase == RendererWrapper.RendererOneofCase.ThumbnailOverlayTimeStatusRenderer)
						?.ThumbnailOverlayTimeStatusRenderer.Text)),
					PublishedText = ReadRuns(renderer.GridVideoRenderer.PublishedTimeText),
					ViewCountText = ReadRuns(renderer.GridVideoRenderer.ViewCountText),
					Badges = renderer.GridVideoRenderer.Badges.Select(x => x.MetadataBadgeRenderer).ToArray(),
					Description = null
				}
			},
			RendererWrapper.RendererOneofCase.ChannelVideoPlayerRenderer => new RendererContainer
			{
				Type = "video",
				OriginalType = "channelVideoPlayerRenderer",
				Data = new VideoRendererData
				{
					Id = renderer.ChannelVideoPlayerRenderer.VideoId,
					Title = ReadRuns(renderer.ChannelVideoPlayerRenderer.Title),
					Duration = TimeSpan.Zero,
					PublishedText = ReadRuns(renderer.ChannelVideoPlayerRenderer.PublishedTimeText),
					ViewCountText = ReadRuns(renderer.ChannelVideoPlayerRenderer.ViewCountText),
					Badges = [],
					Description = ReadRuns(renderer.ChannelVideoPlayerRenderer.Description)
				}
			},
			RendererWrapper.RendererOneofCase.CompactMovieRenderer => new RendererContainer
			{
				Type = "video",
				OriginalType = "compactMovieRenderer",
				Data = new VideoRendererData
				{
					Id = renderer.CompactMovieRenderer.VideoId,
					Title = ReadRuns(renderer.CompactMovieRenderer.Title),
					Thumbnails = renderer.CompactMovieRenderer.Thumbnail.Thumbnails_.ToArray(),
					Author = Channel.From(renderer.CompactMovieRenderer.ShortBylineText),
					Duration = ParseDuration(renderer.CompactMovieRenderer.LengthText?.SimpleText ?? "00:00"),
					PublishedText = "",
					ViewCountText = "",
					Badges = renderer.CompactMovieRenderer.Badges.Select(x => x.MetadataBadgeRenderer)
						.ToArray(),
					Description = ReadRuns(renderer.CompactMovieRenderer.TopMetadataItems)
				}
			},
			RendererWrapper.RendererOneofCase.ContinuationItemRenderer => new RendererContainer
			{
				Type = "continuation",
				OriginalType = "continuationItemRenderer",
				Data = new ContinuationRenderer
				{
					ContinuationToken = renderer.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token
				}
			},
			_ => new RendererContainer
			{
				Type = "unknown",
				OriginalType = renderer.GetType().Name,
				Data = new UnknownRendererData
				{
					Json = JsonSerializer.Serialize(renderer, new JsonSerializerOptions
					{
						DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
					})
				}
			}
		};
	}

    [GeneratedRegex(@"\D")]
    private static partial Regex GeneratedNotDigitsRegex();
}