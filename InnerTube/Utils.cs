﻿using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using Google.Protobuf;
using InnerTube.Formatters;
using InnerTube.Models;
using InnerTube.Parsers;
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

		if (text.HasSimpleText && !string.IsNullOrWhiteSpace(text.SimpleText))
			return Formatter.HandleLineBreaks(text.SimpleText);

		// TODO: check for .label
		/*
			if (richText.ContainsKey("label"))
				return richText["label"]!.ToString();
		 */

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
						string target = UnwrapRedirectUrl(run.NavigationEndpoint.UrlEndpoint.Url);
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

	public static string UnwrapRedirectUrl(string url)
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

	public static string GetParams(this ChannelTabs tab) => GetParamsFromChannelTabName(tab.ToString().ToLower());

	public static string GetNameFromChannelParams(string param) =>
		ChannelTabParams.Parser.ParseFrom(FromBase64UrlString(param)).Name;

	public static ChannelTabs GetTabFromChannelParams(string param)
	{
		return GetNameFromChannelParams(param) switch
		{
			"featured" => ChannelTabs.Featured,
			"videos" => ChannelTabs.Videos,
			"shorts" => ChannelTabs.Shorts,
			"streams" => ChannelTabs.Streams,
			"releases" => ChannelTabs.Releases,
			"playlists" => ChannelTabs.Playlists,
			"community" => ChannelTabs.Community,
			"store" => ChannelTabs.Store,
			"search" => ChannelTabs.Search,
			_ => ChannelTabs.Unknown
		};
	}

	public static string GetParamsFromChannelTabName(string name) =>
		ToBase64UrlString(new ChannelTabParams
		{
			Name = name
		}.ToByteArray());

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

	public static string ToBase64UrlString(byte[] buffer, bool keepPadding = false)
	{
		string res = Convert.ToBase64String(buffer)
			.Replace('+', '-')
			.Replace('/', '_');
		res = !keepPadding ? res.TrimEnd('=') : res.Replace("=", "%3D");
		return res;
	}

	public static byte[] FromBase64UrlString(string s)
	{
		string b64 = HttpUtility.UrlDecode(s);
		if (!b64.EndsWith('='))
			b64 = b64.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
		return Convert.FromBase64String(b64
			.Replace('-', '+')
			.Replace('_', '/'));
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

	public static string? PackPlaylistParams(bool showUnavailableVideos, PlaylistFilter filter = PlaylistFilter.All)
	{
		if (!showUnavailableVideos && filter == PlaylistFilter.All) return null;
		PlaylistParamsContainer container = new()
		{
			Params = new PlaylistParams
			{
				HideUnavailableVideos = !showUnavailableVideos,
				VideosOnly = filter == PlaylistFilter.Videos,
				ShortsOnly = filter == PlaylistFilter.Shorts
			}
		};
		return ToBase64UrlString(container.ToByteArray(), true);
	}

	public static string PackChannelAboutPageParams(string channelId, Guid? modalUuid = null)
	{
		Guid uuid = modalUuid ?? Guid.NewGuid();

		ChannelAboutParamsContainer aboutParamsContainer = new()
		{
			/* 110 */ ChannelAboutParams = new ChannelAboutParams
			{
				/* 3 */ ModalUuidParamsContainer = new ModalUuidParamsContainer
				{
					/* 19 > 1 */ Uuid = uuid.ToString()
				}
			}
		};

		ChannelBrowseParamsContainer container = new()
		{
			ChannelBrowse = new ChannelBrowseParams
			{
				BrowseId = channelId,
				Params = ToBase64UrlString(aboutParamsContainer.ToByteArray())
			}
		};

		return ToBase64UrlString(container.ToByteArray());
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
							$"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}"))}"))
					.TrimStart());
				sb.AppendLine("Duration: " + video.LengthText?.SimpleText);
				sb.AppendLine("ViewCount: " + video.ViewCountText.SimpleText);
				sb.AppendLine("ShortViewCount: " + video.ShortViewCountText.SimpleText);
				sb.AppendLine("PublishDate: " + video.PublishedTimeText?.SimpleText);
				sb.AppendLine($"Badges: ({video.Badges.Count})\n- " +
				              string.Join("",
						              video.Badges.Select(x =>
							              $"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}"))}"))
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
								$"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}"))}"))
						.TrimStart())
					.AppendLine("Duration: " + video.LengthText?.SimpleText)
					.AppendLine("ViewCount: " + video.ViewCountText.SimpleText)
					.AppendLine("ShortViewCount: " + video.ShortViewCountText.SimpleText)
					.AppendLine("PublishDate: " + video.PublishedTimeText?.SimpleText)
					.AppendLine($"Badges: ({video.Badges.Count})\n- " +
					            string.Join("",
							            video.Badges.Select(x =>
								            $"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}"))}"))
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
							$"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}"))}"))
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
							              $"\n{string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}"))}"))
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
								            $"- {string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}")).TrimStart()}"))
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
								            $"- {string.Join("\n", SerializeRenderer(x).Split("\n").Select(y => $"  {y}")).TrimStart()}"))
						            .TrimStart())
					.AppendLine("DescriptionSnippet:\n" + ReadRuns(channel.DescriptionSnippet));
				return sb.ToString();
			}
			case RendererWrapper.RendererOneofCase.MetadataBadgeRenderer:
			{
				MetadataBadgeRenderer badge = renderer.MetadataBadgeRenderer;
				StringBuilder sb = new();
				sb.AppendLine($"[MetadataBadgeRenderer]")
					.AppendLine("Icon: " + (badge.Icon?.IconType.ToString() ?? "<no icon>"))
					.AppendLine("Style: " + (badge.Style))
					.AppendLine("Label: " + (badge.Label ?? "<no label>"))
					.AppendLine("Tooltip: " + (badge.Tooltip ?? "<no tooltip>"));
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
						string.Join('\n', SerializeRenderer(x).Split("\n").Select(y => $"  {y}"))));
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

	public static RendererContainer[] ConvertRenderers(IEnumerable<RendererWrapper>? renderers, string parserLanguage) =>
		(renderers?.Select(x => ConvertRenderer(x, parserLanguage)).Where(x => x != null).ToArray() ?? [])!;

	public static RendererContainer? ConvertRenderer(RendererWrapper renderer, string parserLanguage)
	{
		try
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
						VideoId = renderer.VideoRenderer.VideoId,
						Title = ReadRuns(renderer.VideoRenderer.Title),
						Thumbnails = renderer.VideoRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.VideoRenderer.OwnerText,
							renderer.VideoRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray(),
							renderer.VideoRenderer.ChannelThumbnailSupportedRenderers?.ChannelThumbnailWithLinkRenderer
								?.Thumbnail != null
								? new Thumbnails
								{
									Thumbnails_ =
									{
										renderer.VideoRenderer.ChannelThumbnailSupportedRenderers
											?.ChannelThumbnailWithLinkRenderer?.Thumbnail.Thumbnails_
									}
								}
								: null),
						Duration = ParseDuration(renderer.VideoRenderer.LengthText?.SimpleText ?? "00:00"),
						PublishedText = ReadRuns(renderer.VideoRenderer.PublishedTimeText),
						RelativePublishedDate = ValueParser.ParseRelativeDate(parserLanguage,
							ReadRuns(renderer.VideoRenderer.PublishedTimeText)),
						ViewCountText = ReadRuns(renderer.VideoRenderer.ViewCountText),
						ViewCount = ValueParser.ParseViewCount(parserLanguage,
							ReadRuns(renderer.VideoRenderer.ViewCountText)),
						Badges = SimplifyBadges(renderer.VideoRenderer.Badges),
						Description = ReadRuns(renderer.VideoRenderer.DescriptionSnippet ??
						                       renderer.VideoRenderer.DetailedMetadataSnippets?.SnippetText),
						PremiereStartTime = renderer.VideoRenderer.UpcomingEventData != null
							? DateTimeOffset.FromUnixTimeSeconds(renderer.VideoRenderer.UpcomingEventData.StartTime)
							: null
					}
				},
				RendererWrapper.RendererOneofCase.PlaylistVideoRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "playlistVideoRenderer",
					Data = new PlaylistVideoRendererData
					{
						VideoId = renderer.PlaylistVideoRenderer.VideoId,
						Title = ReadRuns(renderer.PlaylistVideoRenderer.Title),
						Thumbnails = renderer.PlaylistVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.PlaylistVideoRenderer.ShortBylineText),
						Duration = ParseDuration(renderer.PlaylistVideoRenderer.LengthText?.SimpleText ?? "00:00"),
						PublishedText = renderer.PlaylistVideoRenderer.VideoInfo?.Runs.Count > 0
							? renderer.PlaylistVideoRenderer.VideoInfo.Runs[2].Text
							: "",
						RelativePublishedDate = renderer.PlaylistVideoRenderer.VideoInfo?.Runs.Count > 0
							? ValueParser.ParseRelativeDate(parserLanguage,
								renderer.PlaylistVideoRenderer.VideoInfo.Runs[2].Text)
							: "",
						ViewCountText = renderer.PlaylistVideoRenderer.VideoInfo?.Runs.Count > 0
							? renderer.PlaylistVideoRenderer.VideoInfo.Runs[0].Text
							: "",
						ViewCount = renderer.PlaylistVideoRenderer.VideoInfo?.Runs.Count > 0
							? ValueParser.ParseViewCount(parserLanguage,
								renderer.PlaylistVideoRenderer.VideoInfo.Runs[0].Text)
							: -1,
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
						VideoId = renderer.PlaylistPanelVideoRenderer.VideoId,
						Title = ReadRuns(renderer.PlaylistPanelVideoRenderer.Title),
						Thumbnails = renderer.PlaylistPanelVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.PlaylistPanelVideoRenderer.ShortBylineText),
						Duration = ParseDuration(renderer.PlaylistPanelVideoRenderer.LengthText?.SimpleText ?? "00:00"),
						PublishedText = "",
						RelativePublishedDate = "",
						ViewCountText = "",
						ViewCount = -1,
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
						VideoId = renderer.CompactVideoRenderer.VideoId,
						Title = ReadRuns(renderer.CompactVideoRenderer.Text),
						Thumbnails = renderer.CompactVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.CompactVideoRenderer.LongBylineText,
							renderer.CompactVideoRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray()),
						Duration = ParseDuration(renderer.CompactVideoRenderer.LengthText?.SimpleText ?? "00:00"),
						PublishedText = ReadRuns(renderer.CompactVideoRenderer.PublishedTimeText),
						RelativePublishedDate = ValueParser.ParseRelativeDate(parserLanguage,
							ReadRuns(renderer.CompactVideoRenderer.PublishedTimeText)),
						ViewCountText = ReadRuns(renderer.CompactVideoRenderer.ViewCountText),
						ViewCount = ValueParser.ParseViewCount(parserLanguage,
							ReadRuns(renderer.CompactVideoRenderer.ViewCountText)),
						Badges = SimplifyBadges(renderer.CompactVideoRenderer.Badges),
						Description = null,
						PremiereStartTime = renderer.CompactVideoRenderer.UpcomingEventData != null
							? DateTimeOffset.FromUnixTimeSeconds(renderer.CompactVideoRenderer.UpcomingEventData
								.StartTime)
							: null
					}
				},
				RendererWrapper.RendererOneofCase.GridVideoRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "gridVideoRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.GridVideoRenderer.VideoId,
						Title = ReadRuns(renderer.GridVideoRenderer.Title),
						Thumbnails = renderer.GridVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.GridVideoRenderer.ShortBylineText,
							renderer.GridVideoRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray()),
						Duration = ParseDuration(ReadRuns(renderer.GridVideoRenderer.ThumbnailOverlays
							.FirstOrDefault(x =>
								x.RendererCase == RendererWrapper.RendererOneofCase.ThumbnailOverlayTimeStatusRenderer)
							?.ThumbnailOverlayTimeStatusRenderer.Text)),
						PublishedText = ReadRuns(renderer.GridVideoRenderer.PublishedTimeText),
						RelativePublishedDate = ValueParser.ParseRelativeDate(parserLanguage,
							ReadRuns(renderer.GridVideoRenderer.PublishedTimeText)),
						ViewCountText = ReadRuns(renderer.GridVideoRenderer.ViewCountText),
						ViewCount = ValueParser.ParseViewCount(parserLanguage,
							ReadRuns(renderer.GridVideoRenderer.ViewCountText)),
						Badges = SimplifyBadges(renderer.GridVideoRenderer.Badges),
						Description = null,
						PremiereStartTime = renderer.GridVideoRenderer.UpcomingEventData != null
							? DateTimeOffset.FromUnixTimeSeconds(renderer.GridVideoRenderer.UpcomingEventData.StartTime)
							: null
					}
				},
				RendererWrapper.RendererOneofCase.ChannelVideoPlayerRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "channelVideoPlayerRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.ChannelVideoPlayerRenderer.VideoId,
						Title = ReadRuns(renderer.ChannelVideoPlayerRenderer.Title),
						Duration = TimeSpan.Zero,
						PublishedText = ReadRuns(renderer.ChannelVideoPlayerRenderer.PublishedTimeText),
						RelativePublishedDate = ValueParser.ParseRelativeDate(parserLanguage,
							ReadRuns(renderer.ChannelVideoPlayerRenderer.PublishedTimeText)),
						ViewCountText = ReadRuns(renderer.ChannelVideoPlayerRenderer.ViewCountText),
						ViewCount = ValueParser.ParseViewCount(parserLanguage,
							ReadRuns(renderer.ChannelVideoPlayerRenderer.ViewCountText)),
						Badges = [],
						Thumbnails = [],
						Description = ReadRuns(renderer.ChannelVideoPlayerRenderer.Description)
					}
				},
				RendererWrapper.RendererOneofCase.MovieRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "movieRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.MovieRenderer.VideoId,
						Title = ReadRuns(renderer.MovieRenderer.Title),
						Thumbnails = renderer.MovieRenderer.Thumbnails.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.MovieRenderer.LongBylineText,
							renderer.MovieRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray(),
							renderer.MovieRenderer.ChannelThumbnailSupportedRenderers?.ChannelThumbnailWithLinkRenderer
								?.Thumbnail != null
								? new Thumbnails
								{
									Thumbnails_ =
									{
										renderer.MovieRenderer.ChannelThumbnailSupportedRenderers
											?.ChannelThumbnailWithLinkRenderer?.Thumbnail.Thumbnails_
									}
								}
								: null),
						Duration = ParseDuration(renderer.MovieRenderer.LengthText?.SimpleText ?? "00:00"),
						PublishedText = null,
						RelativePublishedDate = null,
						ViewCountText = null,
						ViewCount = 0,
						Badges = SimplifyBadges(renderer.MovieRenderer.Badges),
						Description = ReadRuns(renderer.MovieRenderer.DescriptionSnippet),
						PremiereStartTime = null
					}
				},
				RendererWrapper.RendererOneofCase.CompactMovieRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "compactMovieRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.CompactMovieRenderer.VideoId,
						Title = ReadRuns(renderer.CompactMovieRenderer.Title),
						Thumbnails = renderer.CompactMovieRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.CompactMovieRenderer.ShortBylineText),
						Duration = ParseDuration(renderer.CompactMovieRenderer.LengthText?.SimpleText ?? "00:00"),
						PublishedText = "",
						RelativePublishedDate = "",
						ViewCountText = "",
						ViewCount = -1,
						Badges = SimplifyBadges(renderer.CompactMovieRenderer.Badges),
						Description = ReadRuns(renderer.CompactMovieRenderer.TopMetadataItems)
					}
				},
				RendererWrapper.RendererOneofCase.ReelItemRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "reelItemRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.ReelItemRenderer.VideoId,
						Title = ReadRuns(renderer.ReelItemRenderer.Headline),
						Thumbnails = renderer.ReelItemRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = null,
						Duration = TimeSpan.Zero,
						PublishedText = "",
						RelativePublishedDate = "",
						ViewCountText = ReadRuns(renderer.ReelItemRenderer.ViewCountText),
						ViewCount = ValueParser.ParseViewCount(parserLanguage,
							ReadRuns(renderer.ReelItemRenderer.ViewCountText)),
						Badges = [],
						Description = null
					}
				},
				RendererWrapper.RendererOneofCase.PromotedVideoRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "promotedVideoRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.PromotedVideoRenderer.VideoId,
						Title = ReadRuns(renderer.PromotedVideoRenderer.Title),
						Thumbnails = renderer.PromotedVideoRenderer.Thumbnail.Thumbnails_.ToArray(),
						Author = Channel.From(renderer.PromotedVideoRenderer.LongBylineText),
						Duration = ParseDuration(ReadRuns(renderer.PromotedVideoRenderer.LengthText)),
						PublishedText = "",
						RelativePublishedDate = "",
						ViewCountText = "",
						ViewCount = -1,
						Badges = SimplifyBadges([renderer.PromotedVideoRenderer.AdBadge]),
						Description = ReadRuns(renderer.PromotedVideoRenderer.Description)
					}
				},
				RendererWrapper.RendererOneofCase.ChildVideoRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "childVideoRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.ChildVideoRenderer.VideoId,
						Title = ReadRuns(renderer.ChildVideoRenderer.Title),
						Thumbnails = [],
						Author = null,
						Duration = ParseDuration(ReadRuns(renderer.ChildVideoRenderer.LengthText)),
						PublishedText = "",
						RelativePublishedDate = "",
						ViewCountText = "",
						ViewCount = -1,
						Badges = [],
						Description = null,
						PremiereStartTime = null
					}
				},
				RendererWrapper.RendererOneofCase.WatchCardCompactVideoRenderer => new RendererContainer
				{
					Type = "video",
					OriginalType = "watchCardCompactVideoRenderer",
					Data = new VideoRendererData
					{
						VideoId = renderer.WatchCardCompactVideoRenderer.NavigationEndpoint.WatchEndpoint.VideoId,
						Title = ReadRuns(renderer.WatchCardCompactVideoRenderer.Title),
						Thumbnails = [],
						Author = Channel.From(renderer.WatchCardCompactVideoRenderer.Byline) ??
						         Channel.From(renderer.WatchCardCompactVideoRenderer.Subtitle),
						Duration = ParseDuration(ReadRuns(renderer.WatchCardCompactVideoRenderer.LengthText)),
						PublishedText = ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle).Contains(" • ")
							? ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle).Split(" • ")[1]
							: null,
						RelativePublishedDate =
							ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle).Contains(" • ")
								? ValueParser.ParseRelativeDate(parserLanguage,
									ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle).Split(" • ")[1])
								: null,
						ViewCountText = ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle).Split(" • ")[0],
						ViewCount = ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle).Contains(" • ")
							? ValueParser.ParseViewCount(parserLanguage,
								ReadRuns(renderer.WatchCardCompactVideoRenderer.Subtitle).Split(" • ")[0])
							: 0,
						Badges = [],
						Description = null,
						PremiereStartTime = null
					}
				},
				RendererWrapper.RendererOneofCase.ChannelRenderer => new RendererContainer
				{
					Type = "channel",
					OriginalType = "channelRenderer",
					Data = new ChannelRendererData(renderer.ChannelRenderer, parserLanguage)
				},
				RendererWrapper.RendererOneofCase.GridChannelRenderer => new RendererContainer
				{
					Type = "channel",
					OriginalType = "gridChannelRenderer",
					Data = new ChannelRendererData
					{
						ChannelId = renderer.GridChannelRenderer.ChannelId,
						Title = ReadRuns(renderer.GridChannelRenderer.Title),
						Handle = Channel.TryGetHandle(renderer.GridChannelRenderer.NavigationEndpoint.BrowseEndpoint
							.CanonicalBaseUrl),
						Avatar = renderer.GridChannelRenderer.Thumbnail.Thumbnails_.ToArray(),
						VideoCountText = ReadRuns(renderer.GridChannelRenderer.VideoCountText),
						VideoCount = ValueParser.ParseVideoCount(parserLanguage,
							ReadRuns(renderer.GridChannelRenderer.VideoCountText)),
						SubscriberCountText = ReadRuns(renderer.GridChannelRenderer.SubscriberCountText),
						SubscriberCount = ValueParser.ParseSubscriberCount(parserLanguage,
							ReadRuns(renderer.GridChannelRenderer.SubscriberCountText))
					}
				},
				RendererWrapper.RendererOneofCase.PlaylistRenderer => new RendererContainer
				{
					Type = "playlist",
					OriginalType = "playlistRenderer",
					Data = new PlaylistRendererData
					{
						PlaylistId = renderer.PlaylistRenderer.PlaylistId,
						Thumbnails = renderer.PlaylistRenderer.Thumbnails[0].Thumbnails_.ToArray(),
						Title = ReadRuns(renderer.PlaylistRenderer.Title),
						VideoCountText = ReadRuns(renderer.PlaylistRenderer.VideoCountText),
						VideoCount = renderer.PlaylistRenderer.VideoCount,
						SidebarThumbnails = renderer.PlaylistRenderer.Thumbnails.ToArray()[1..]
							.Select(x => x.Thumbnails_.ToArray()).ToArray(),
						Author = Channel.From(renderer.PlaylistRenderer.ShortBylineText,
							renderer.PlaylistRenderer.OwnerBadges.Select(x => x.MetadataBadgeRenderer).ToArray()),
						ChildVideos = ConvertRenderers(renderer.PlaylistRenderer.Videos, parserLanguage),
						FirstVideoId = renderer.PlaylistRenderer.NavigationEndpoint.WatchEndpoint?.VideoId ??
						               renderer.PlaylistRenderer.NavigationEndpoint.ReelWatchEndpoint?.VideoId ??
						               renderer.PlaylistRenderer.Videos.FirstOrDefault()?.ChildVideoRenderer.VideoId
					}
				},
				RendererWrapper.RendererOneofCase.GridPlaylistRenderer => new RendererContainer
				{
					Type = "playlist",
					OriginalType = "gridPlaylistRenderer",
					Data = new PlaylistRendererData
					{
						PlaylistId = renderer.GridPlaylistRenderer.PlaylistId,
						Thumbnails = renderer.GridPlaylistRenderer.Thumbnail.Thumbnails_.ToArray(),
						Title = ReadRuns(renderer.GridPlaylistRenderer.Title),
						VideoCountText = ReadRuns(renderer.GridPlaylistRenderer.VideoCountText),
						VideoCount = ValueParser.ParseVideoCount(parserLanguage,
							ReadRuns(renderer.GridPlaylistRenderer.VideoCountText)),
						SidebarThumbnails = renderer.GridPlaylistRenderer.SidebarThumbnails
							.Select(x => x.Thumbnails_.ToArray()).ToArray(),
						Author = null,
						FirstVideoId = renderer.GridPlaylistRenderer.NavigationEndpoint.WatchEndpoint?.VideoId ??
						               renderer.GridPlaylistRenderer.NavigationEndpoint.ReelWatchEndpoint?.VideoId
					}
				},
				RendererWrapper.RendererOneofCase.CompactPlaylistRenderer => new RendererContainer
				{
					Type = "playlist",
					OriginalType = "compactPlaylistRenderer",
					Data = new PlaylistRendererData
					{
						PlaylistId = renderer.CompactPlaylistRenderer.PlaylistId,
						Thumbnails = renderer.CompactPlaylistRenderer.Thumbnail.Thumbnails_.ToArray(),
						Title = ReadRuns(renderer.CompactPlaylistRenderer.Title),
						VideoCountText = ReadRuns(renderer.CompactPlaylistRenderer.VideoCountText),
						VideoCount = ValueParser.ParseVideoCount(parserLanguage,
							ReadRuns(renderer.CompactPlaylistRenderer.VideoCountText)),
						SidebarThumbnails = renderer.CompactPlaylistRenderer.SidebarThumbnails
							.Select(x => x.Thumbnails_.ToArray()).ToArray(),
						Author = Channel.From(renderer.CompactPlaylistRenderer.ShortBylineText),
						FirstVideoId = renderer.CompactPlaylistRenderer.NavigationEndpoint.WatchEndpoint?.VideoId ??
						               renderer.CompactPlaylistRenderer.NavigationEndpoint.ReelWatchEndpoint?.VideoId
					}
				},
				RendererWrapper.RendererOneofCase.RadioRenderer => new RendererContainer
				{
					Type = "playlist",
					OriginalType = "radioRenderer",
					Data = new PlaylistRendererData
					{
						PlaylistId = renderer.RadioRenderer.PlaylistId,
						Thumbnails = renderer.RadioRenderer.Thumbnails[0].Thumbnails_.ToArray(),
						Title = ReadRuns(renderer.RadioRenderer.Title),
						VideoCountText = ReadRuns(renderer.RadioRenderer.VideoCountText),
						// this will always fail i think
						VideoCount = ValueParser.ParseVideoCount(parserLanguage,
							ReadRuns(renderer.RadioRenderer.VideoCountText)),
						SidebarThumbnails = null,
						Author = Channel.From(renderer.RadioRenderer.LongBylineText),
						FirstVideoId = renderer.RadioRenderer.NavigationEndpoint.WatchEndpoint?.VideoId ??
						               renderer.RadioRenderer.NavigationEndpoint.ReelWatchEndpoint?.VideoId
					}
				},
				RendererWrapper.RendererOneofCase.CompactRadioRenderer => new RendererContainer
				{
					Type = "playlist",
					OriginalType = "compactRadioRenderer",
					Data = new PlaylistRendererData
					{
						PlaylistId = renderer.CompactRadioRenderer.PlaylistId,
						Thumbnails = renderer.CompactRadioRenderer.Thumbnail.Thumbnails_.ToArray(),
						Title = ReadRuns(renderer.CompactRadioRenderer.Title),
						VideoCountText = ReadRuns(renderer.CompactRadioRenderer.VideoCountText),
						// this will always fail i think
						VideoCount = ValueParser.ParseVideoCount(parserLanguage,
							ReadRuns(renderer.CompactRadioRenderer.VideoCountText)),
						SidebarThumbnails = null,
						Author = Channel.From(renderer.CompactRadioRenderer.LongBylineText),
						FirstVideoId = renderer.CompactRadioRenderer.NavigationEndpoint.WatchEndpoint?.VideoId ??
						               renderer.CompactRadioRenderer.NavigationEndpoint.ReelWatchEndpoint?.VideoId
					}
				},
				RendererWrapper.RendererOneofCase.ContinuationItemRenderer => new RendererContainer
				{
					Type = "continuation",
					OriginalType = "continuationItemRenderer",
					Data = new ContinuationRendererData
					{
						ContinuationToken = renderer.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand
							.Token
					}
				},
				RendererWrapper.RendererOneofCase.RecognitionShelfRenderer => new RendererContainer
				{
					Type = "recognitionShelf",
					OriginalType = "recognitionShelfRenderer",
					Data = new RecognitionShelfRendererData
					{
						Title = ReadRuns(renderer.RecognitionShelfRenderer.Title),
						Subtitle = ReadRuns(renderer.RecognitionShelfRenderer.Subtitle),
						Avatars = renderer.RecognitionShelfRenderer.Avatars
							.SelectMany(x => x.Thumbnails_.Select(y => y.Url)).ToArray()
					}
				},
				RendererWrapper.RendererOneofCase.BackstagePostThreadRenderer => ConvertRenderer(
					renderer.BackstagePostThreadRenderer.Post, parserLanguage),
				RendererWrapper.RendererOneofCase.BackstagePostRenderer => new RendererContainer
				{
					Type = "communityPost",
					OriginalType = "backstagePostRenderer",
					Data = new CommunityPostRendererData
					{
						PostId = renderer.BackstagePostRenderer.PostId,
						Author = Channel.From(renderer.BackstagePostRenderer.AuthorText,
							avatar: renderer.BackstagePostRenderer.AuthorThumbnail)!,
						Content = ReadRuns(renderer.BackstagePostRenderer.ContentText),
						LikeCountText = ReadRuns(renderer.BackstagePostRenderer.VoteCount),
						LikeCount = ValueParser.ParseLikeCount(parserLanguage,
							ReadRuns(renderer.BackstagePostRenderer.VoteCount)),
						CommentsCountText = ReadRuns(renderer.BackstagePostRenderer.ActionButtons
							.CommentActionButtonsRenderer.ReplyButton.ButtonViewModel.Title),
						CommentCount = ValueParser.ParseLikeCount(parserLanguage, ReadRuns(renderer
							.BackstagePostRenderer.ActionButtons
							.CommentActionButtonsRenderer.ReplyButton.ButtonViewModel.Title)),
						PublishedText = ReadRuns(renderer.BackstagePostRenderer.PublishedTimeText),
						RelativePublishedDate = ValueParser.ParseRelativeDate(parserLanguage,
							ReadRuns(renderer.BackstagePostRenderer.PublishedTimeText)),
						Attachment = renderer.BackstagePostRenderer.BackstageAttachment != null
							? ConvertRenderer(renderer.BackstagePostRenderer.BackstageAttachment, parserLanguage)
							: null
					}
				},
				RendererWrapper.RendererOneofCase.PostRenderer => new RendererContainer
				{
					Type = "communityPost",
					OriginalType = "postRenderer",
					Data = new CommunityPostRendererData
					{
						PostId = renderer.PostRenderer.PostId,
						Author = Channel.From(renderer.PostRenderer.AuthorText,
							avatar: renderer.PostRenderer.AuthorThumbnail)!,
						Content = ReadRuns(renderer.PostRenderer.ContentText),
						LikeCountText = ReadRuns(renderer.PostRenderer.VoteCount),
						LikeCount = ValueParser.ParseLikeCount(parserLanguage,
							ReadRuns(renderer.PostRenderer.VoteCount)),
						CommentsCountText = ReadRuns(renderer.PostRenderer.ActionButtons
							.CommentActionButtonsRenderer.ReplyButton.ButtonViewModel.Title),
						CommentCount = ValueParser.ParseLikeCount(parserLanguage, ReadRuns(renderer.PostRenderer
							.ActionButtons
							.CommentActionButtonsRenderer.ReplyButton.ButtonViewModel.Title)),
						PublishedText = ReadRuns(renderer.PostRenderer.PublishedTimeText),
						RelativePublishedDate = ValueParser.ParseRelativeDate(parserLanguage,
							ReadRuns(renderer.PostRenderer.PublishedTimeText)),
						Attachment = renderer.PostRenderer.BackstageAttachment != null
							? ConvertRenderer(renderer.PostRenderer.BackstageAttachment, parserLanguage)
							: null
					}
				},
				RendererWrapper.RendererOneofCase.BackstageImageRenderer => new RendererContainer
				{
					Type = "communityPostImage",
					OriginalType = "backstageImageRenderer",
					Data = new CommunityPostImageRendererData
					{
						Images = [renderer.BackstageImageRenderer.Image.Thumbnails_.ToArray()]
					}
				},
				RendererWrapper.RendererOneofCase.PostMultiImageRenderer => new RendererContainer
				{
					Type = "communityPostImage",
					OriginalType = "PostMultiImageRenderer",
					Data = new CommunityPostImageRendererData
					{
						Images = renderer.PostMultiImageRenderer.Images
							.Select(x => x.BackstageImageRenderer.Image.Thumbnails_.ToArray()).ToArray()
					}
				},
				RendererWrapper.RendererOneofCase.UniversalWatchCardRenderer => new RendererContainer
				{
					Type = "searchSidebar",
					OriginalType = "universalWatchCardRenderer",
					Data = new SearchSidebarRendererData
					{
						Title = ReadRuns(renderer.UniversalWatchCardRenderer.Header.WatchCardRichHeaderRenderer.Title),
						Subtitle = ReadRuns(renderer.UniversalWatchCardRenderer.Header.WatchCardRichHeaderRenderer
							.Subtitle),
						Avatar = renderer.UniversalWatchCardRenderer.Header.WatchCardRichHeaderRenderer.Avatar?
							.Thumbnails_.ToArray(),
						TitleBadge = SimplifyBadges([
							renderer.UniversalWatchCardRenderer.Header.WatchCardRichHeaderRenderer.TitleBadge
						]).FirstOrDefault(),
						CallToAction =
							ConvertRenderer(renderer.UniversalWatchCardRenderer.CallToAction, parserLanguage),
						Sections = ConvertRenderers(renderer.UniversalWatchCardRenderer.Sections, parserLanguage),
						Type = renderer.UniversalWatchCardRenderer.CallToAction.WatchCardHeroVideoRenderer.HeroImage
								.RendererCase switch
							{
								RendererWrapper.RendererOneofCase.CollageHeroImageRenderer => SearchSidebarType.Artist,
								RendererWrapper.RendererOneofCase.SingleHeroImageRenderer => renderer
									.UniversalWatchCardRenderer.CallToAction.WatchCardHeroVideoRenderer
									.NavigationEndpoint.CommandMetadata.WebCommandMetadata.Url.Contains("/show/")
									? SearchSidebarType.TvShow
									: SearchSidebarType.Album,
								_ => SearchSidebarType.Unknown
							}
					}
				},
				RendererWrapper.RendererOneofCase.WatchCardHeroVideoRenderer => new RendererContainer
				{
					Type = "heroVideo",
					OriginalType = "watchCardHeroVideoRenderer",
					Data = new WatchCardHeroVideoRendererData
					{
						Title = renderer.WatchCardHeroVideoRenderer.Accessibility.AccessibilityData.Label,
						VideoId = renderer.WatchCardHeroVideoRenderer.NavigationEndpoint.WatchEndpoint?.VideoId,
						HeroImages = renderer.WatchCardHeroVideoRenderer.HeroImage.RendererCase ==
						             RendererWrapper.RendererOneofCase.CollageHeroImageRenderer
							?
							[
								renderer.WatchCardHeroVideoRenderer.HeroImage.CollageHeroImageRenderer.LeftThumbnail
									.Thumbnails_.ToArray(),
								renderer.WatchCardHeroVideoRenderer.HeroImage.CollageHeroImageRenderer.TopRightThumbnail
									.Thumbnails_.ToArray(),
								renderer.WatchCardHeroVideoRenderer.HeroImage.CollageHeroImageRenderer
									.BottomRightThumbnail.Thumbnails_.ToArray()
							]
							:
							[
								renderer.WatchCardHeroVideoRenderer.HeroImage.SingleHeroImageRenderer.Thumbnail
									.Thumbnails_.ToArray()
							]
					}
				},
				RendererWrapper.RendererOneofCase.WatchCardSectionSequenceRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "watchCardSectionSequenceRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.WatchCardSectionSequenceRenderer.Lists[0].RendererCase switch
						{
							RendererWrapper.RendererOneofCase.VerticalWatchCardListRenderer => renderer.WatchCardSectionSequenceRenderer.Lists[0]
								.VerticalWatchCardListRenderer.Items,
							RendererWrapper.RendererOneofCase.HorizontalCardListRenderer => renderer.WatchCardSectionSequenceRenderer.Lists[0]
								.HorizontalCardListRenderer.Cards,
							_ =>
							[
								new RendererWrapper
								{
									MessageRenderer = new MessageRenderer
									{
										Text = new Text
										{
											SimpleText =
												$"INNERTUBE: Unexpected RendererCase: {renderer.WatchCardSectionSequenceRenderer.Lists[0].RendererCase}"
										}
									}
								}
							]
						}, parserLanguage),
						Style = renderer.WatchCardSectionSequenceRenderer.Lists[0].RendererCase switch
						{
							RendererWrapper.RendererOneofCase.VerticalWatchCardListRenderer => "searchSidebar;vertical",
							RendererWrapper.RendererOneofCase.HorizontalCardListRenderer => "searchSidebar;cards",
							_ => "vertical"
						},
						Title = ReadRuns(renderer.WatchCardSectionSequenceRenderer.ListTitles.FirstOrDefault()),
						Subtitle = null,
						Destination = null,
						ShownItemCount = null
					}
				},
				RendererWrapper.RendererOneofCase.WatchCardSectionDropdownRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "watchCardSectionDropdownRenderer",
					Data = new ContainerRendererData
					{
						Items = renderer.WatchCardSectionDropdownRenderer.Lists
							.Where(x => x.RendererCase ==
							            RendererWrapper.RendererOneofCase.VerticalWatchCardListRenderer)
							.Select((x, i) => new RendererContainer
							{
								Type = "container",
								OriginalType = "verticalWatchCardListRenderer",
								Data = new ContainerRendererData
								{
									Items = ConvertRenderers(x.VerticalWatchCardListRenderer.Items, parserLanguage),
									Style = "dropdown",
									Title = ReadRuns(renderer.WatchCardSectionDropdownRenderer.DropdownTitles[i]),
									Subtitle = ReadRuns(renderer.WatchCardSectionDropdownRenderer.DropdownSubtitles[i]),
									Destination = null,
									ShownItemCount = null
								}
						}).ToArray(),
						Style = "searchSidebar;vertical",
						Title = null,
						Subtitle = null,
						Destination = null,
						ShownItemCount = null
					}
				},
				RendererWrapper.RendererOneofCase.SearchRefinementCardRenderer => new RendererContainer
				{
					Type = "searchRefinementCard",
					OriginalType = "searchRefinementCardRenderer",
					Data = new SearchRefinementCardRendererData
					{
						Thumbnail = renderer.SearchRefinementCardRenderer.Thumbnail.Thumbnails_[0].Url,
						Title = ReadRuns(renderer.SearchRefinementCardRenderer.Query),
						PlaylistId = renderer.SearchRefinementCardRenderer.SearchEndpoint.WatchPlaylistEndpoint?.PlaylistId,
						SearchQuery = renderer.SearchRefinementCardRenderer.SearchEndpoint.SearchEndpoint?.Query
					}
				},
				RendererWrapper.RendererOneofCase.ItemSectionRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "itemSectionRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.ItemSectionRenderer.Contents, parserLanguage)
					}
				},
				RendererWrapper.RendererOneofCase.HorizontalCardListRenderer => new RendererContainer {
					Type = "container",
					OriginalType = "horizontalCardListRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.HorizontalCardListRenderer.Cards, parserLanguage),
						Title = ReadRuns(renderer.HorizontalCardListRenderer.Header?.RichListHeaderRenderer?.Title),
						Style = "shelf;horizontal;cards"
					}
				},
				RendererWrapper.RendererOneofCase.ShelfRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "shelfRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.ShelfRenderer.Content.RendererCase switch
						{
							RendererWrapper.RendererOneofCase.VerticalListRenderer => renderer.ShelfRenderer.Content
								.VerticalListRenderer.Items,
							RendererWrapper.RendererOneofCase.HorizontalListRenderer => renderer.ShelfRenderer.Content
								.HorizontalListRenderer.Items,
							RendererWrapper.RendererOneofCase.HorizontalCardListRenderer => renderer.ShelfRenderer.Content
								.HorizontalCardListRenderer.Cards,
							RendererWrapper.RendererOneofCase.ExpandedShelfContentsRenderer => renderer.ExpandedShelfContentsRenderer.Items,
							_ =>
							[
								new RendererWrapper
								{
									MessageRenderer = new MessageRenderer
									{
										Text = new Text
										{
											SimpleText =
												$"INNERTUBE: Unknown RendererCase: {renderer.ShelfRenderer.Content.RendererCase}"
										}
									}
								}
							]
						}, parserLanguage),
						Style = "shelf;" + renderer.ShelfRenderer.Content.RendererCase switch
						{
							RendererWrapper.RendererOneofCase.VerticalListRenderer => "vertical",
							RendererWrapper.RendererOneofCase.HorizontalListRenderer => "horizontal",
							RendererWrapper.RendererOneofCase.HorizontalCardListRenderer => "horizontal;cards",
							RendererWrapper.RendererOneofCase.ExpandableTabRenderer => "expanded",
							_ => "vertical"
						},
						Title = ReadRuns(renderer.ShelfRenderer.Title),
						Subtitle = ReadRuns(renderer.ShelfRenderer.Subtitle),
						// TODO: Destination = Utils.ParseEndpoint(),
						ShownItemCount = renderer.ShelfRenderer.Content.VerticalListRenderer?.CollapsedItemCount
					}
				},
				RendererWrapper.RendererOneofCase.ReelShelfRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "reelShelfRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.ReelShelfRenderer.Items, parserLanguage),
						Style = "shelf;reel",
						Title = ReadRuns(renderer.ReelShelfRenderer.Title)
					}
				},
				RendererWrapper.RendererOneofCase.SearchPyvRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "searchPyvRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.SearchPyvRenderer.Ads, parserLanguage),
						Style = "ad"
					}
				},
				RendererWrapper.RendererOneofCase.GridRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "gridRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.GridRenderer.Items, parserLanguage),
						Style = "grid"
					}
				},
				RendererWrapper.RendererOneofCase.RichGridRenderer => new RendererContainer
				{
					Type = "container",
					OriginalType = "richGridRenderer",
					Data = new ContainerRendererData
					{
						Items = ConvertRenderers(renderer.RichGridRenderer.Contents, parserLanguage),
						Style = "grid"
					}
				},
				RendererWrapper.RendererOneofCase.RichItemRenderer => ConvertRenderer(renderer.RichItemRenderer.Content,
					parserLanguage),
				RendererWrapper.RendererOneofCase.ChannelFeaturedContentRenderer => ConvertRenderer(
					renderer.ChannelFeaturedContentRenderer.Items[0], parserLanguage),
				RendererWrapper.RendererOneofCase.AdSlotRenderer => ConvertRenderer(renderer.AdSlotRenderer
					.FulfillmentContent.FulfilledLayout.InFeedAdLayoutRenderer.RenderingContent, parserLanguage),
				RendererWrapper.RendererOneofCase.MessageRenderer => new RendererContainer
				{
					Type = "message",
					OriginalType = "messageRenderer",
					Data = new MessageRendererData(ReadRuns(renderer.MessageRenderer.Text ?? renderer.MessageRenderer.Subtext?.MessageSubtextRenderer.Text))
				},
				RendererWrapper.RendererOneofCase.BackgroundPromoRenderer => new RendererContainer
				{
					Type = "message",
					OriginalType = "backgroundPromoRenderer",
					Data = new MessageRendererData(ReadRuns(renderer.BackgroundPromoRenderer.Text))
				},
				RendererWrapper.RendererOneofCase.ChipCloudChipRenderer => new RendererContainer
				{
					Type = "chip",
					OriginalType = "chipCloudChipRenderer",
					Data = new ChipRendererData
					{
						Title = ReadRuns(renderer.ChipCloudChipRenderer.Text),
						ContinuationToken = renderer.ChipCloudChipRenderer.NavigationEndpoint?.ContinuationCommand
							?.Token,
						Params = renderer.ChipCloudChipRenderer.NavigationEndpoint?.BrowseEndpoint?.Params,
						IsSelected = renderer.ChipCloudChipRenderer.IsSelected
					}
				},
				RendererWrapper.RendererOneofCase
					.PromotedSparklesWebRenderer => null, // ad, doesn't fit into any IRendererData's
				RendererWrapper.RendererOneofCase
					.TextImageNoButtonLayoutRenderer => null, // ad, doesn't fit into any IRendererData's
				_ => new RendererContainer
				{
					Type = "unknown",
					OriginalType = renderer.RendererCase.ToString(),
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
		catch (Exception e)
		{
			return new RendererContainer
			{
				Type = "exception",
				OriginalType = e.GetType().Name,
				Data = new ExceptionRendererData
				{
					Message = e.Message,
					RendererCase = renderer.RendererCase.ToString()
				}
			};
		}
	}

	public static Badge[] SimplifyBadges(IEnumerable<RendererWrapper> renderers) =>
		renderers.Where(x => x != null && x.RendererCase == RendererWrapper.RendererOneofCase.MetadataBadgeRenderer)
			.Select(x => new Badge(x.MetadataBadgeRenderer))
			.ToArray();

	public static Badge[] SimplifyBadges(IEnumerable<MetadataBadgeRenderer> renderers) =>
		renderers.Select(x => new Badge(x)).ToArray();

	public static string? NullIfEmpty(this string input) => string.IsNullOrWhiteSpace(input) ? null : input;

	[GeneratedRegex(@"\D")]
    private static partial Regex GeneratedNotDigitsRegex();
}