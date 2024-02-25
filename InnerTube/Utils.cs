using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Google.Protobuf;
using InnerTube.Formatters;
using InnerTube.Protobuf.Endpoints;
using InnerTube.Protobuf.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public static class Utils
{
	public static IFormatter Formatter = new HtmlFormatter();
	private static readonly Regex NotDigitsRegex = new(@"\D");

	public static T? GetFromJsonPath<T>(this JToken json, string jsonPath)
	{
		try
		{
			string[] properties = jsonPath.Split(".");
			JToken? current = json;
			foreach (string key in properties)
			{
				Match match = Regex.Match(key, @"\[([0-9]*)\]");
				current = match.Success ? current[key.Split("[")[0]]?[int.Parse(match.Groups[1].Value)] : current[key];
				if (current is null) break;
			}

			return current is null ? default : current.ToObject<T>();
		}
		catch
		{
			return default;
		}
	}

	public static string ReadRuns(Text? text, bool includeFormatting = false)
	{
		if (text == null) return "";

		if (text.HasSimpleText) return text.SimpleText;

		// TODO: check for .label

		if (text.Runs.Count > 0)
		{
			string str = "";

			foreach (Text.Types.Run run in text.Runs)
			{
				if (!includeFormatting)
				{
					str += run.Text;
					continue;
				}

				// todo: apply formatting
			}

			return str;
		}

		return text.ToString();
	}

	public static string ReadText(JObject? richText, bool includeFormatting = false)
	{
		if (richText is null)
			return "";

		if (richText.ContainsKey("simpleText"))
			return richText["simpleText"]!.ToString();

		if (richText.ContainsKey("label"))
			return richText["label"]!.ToString();

		if (richText.ContainsKey("runs"))
		{
			JArray runs = richText["runs"]!.ToObject<JArray>()!;
			string str = "";
			foreach (JToken runToken in runs)
			{
				JObject run = (JObject)runToken;

				if (!includeFormatting)
				{
					str += run["text"];
					continue;
				}

				string currentString = Formatter.Sanitize(run["text"]!.ToString());

				if (run.ContainsKey("bold"))
					currentString = Formatter.FormatBold(currentString);
				else if (run.ContainsKey("bold"))
					currentString = Formatter.FormatItalics(currentString);
				else if (run.ContainsKey("navigationEndpoint"))
				{
					if (run["navigationEndpoint"]?["urlEndpoint"] is not null)
					{
						string url = run["navigationEndpoint"]?["urlEndpoint"]?["url"]?.ToString() ?? "";

						currentString = Formatter.FormatUrl(currentString, UnwrapRedirectUrl(url));
					}
					else if (run["navigationEndpoint"]?["commandMetadata"] is not null)
					{
						string url = run["navigationEndpoint"]?["commandMetadata"]?["webCommandMetadata"]?["url"]
							?.ToString() ?? "";
						if (url.StartsWith('/'))
							url = "https://youtube.com" + url;
						currentString = Formatter.FormatUrl(currentString, url);
					}
				}

				str += currentString;
			}

			return Formatter.HandleLineBreaks(str);
		}

		return "";
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

	public static string ReadAttributedDescription(
		VideoSecondaryInfoRenderer.Types.AttributedDescription? attributedDescription, bool includeFormatting = false)
	{
		if (string.IsNullOrEmpty(attributedDescription?.Content)) return "";

		string text = attributedDescription.Content ?? "";

		if (!includeFormatting) return text;
		if (attributedDescription.CommandRuns.Count == 0) return text;

		foreach (VideoSecondaryInfoRenderer.Types.AttributedDescription.Types.CommandRun run in attributedDescription
			         .CommandRuns.Reverse())
		{
			string replacement = text.Substring(run.StartIndex, run.Length);
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
					replacement = Formatter.FormatUrl(replacement, url);
					break;
			}

			text = text
				.Remove(run.StartIndex, run.Length)
				.Insert(run.StartIndex, replacement);
		}

		return Formatter.HandleLineBreaks(text);
	}

	public static string? ReadAttributedText(JObject attributedText, bool includeFormatting = false)
	{
		if (!attributedText.ContainsKey("content")) return null;

		string text = attributedText["content"]?.ToString() ?? "";

		if (!includeFormatting) return text;
		if (!attributedText.ContainsKey("commandRuns")) return text;

		foreach (JToken run in (attributedText["commandRuns"]?.ToObject<JArray>() ?? new JArray()).Reverse())
		{
			int startIndex = run["startIndex"]?.ToObject<int>() ?? 0;
			int length = run["length"]?.ToObject<int>() ?? 0;
			string replacement = text.Substring(startIndex, length);
			JObject command = run.GetFromJsonPath<JObject>("onTap.innertubeCommand") ?? new JObject();

			if (command.ContainsKey("urlEndpoint"))
			{
				string url = UnwrapRedirectUrl(command.GetFromJsonPath<string>("urlEndpoint.url") ?? "");
				replacement = Formatter.FormatUrl(replacement, url);
			}

			if (command.ContainsKey("watchEndpoint"))
			{
				string url = $"https://youtube.com/watch?v={command.GetFromJsonPath<string>("watchEndpoint.videoId")}";
				if (command.GetFromJsonPath<bool>("watchEndpoint.continuePlayback"))
					url += $"&t={command.GetFromJsonPath<int>("watchEndpoint.startTimeSeconds")}";
				replacement = Formatter.FormatUrl(replacement, url);
			}

			if (command.ContainsKey("browseEndpoint"))
			{
				string url = $"https://youtube.com{command.GetFromJsonPath<string>("browseEndpoint.canonicalBaseUrl")}";
				replacement = Formatter.FormatUrl(replacement, url);
			}

			text = text
				.Remove(startIndex, length)
				.Insert(startIndex, replacement);
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
		return new PlaylistContinuationInfo
		{
			InternalPlaylistId = container.Continuation.InternalPlaylistId,
			PlaylistId = container.Continuation.PlaylistId,
			ContinueFrom = UnpackProtobufInt(info.SkipAmountEncoded.Split(":").Last())
		};
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
					.AppendLine($"OwnerBadges: ({video.OwnerBadges.Count})" + string.Join("",
						video.OwnerBadges.Select(x => $"\n- {SerializeRenderer(x)}")))
					.AppendLine("Duration: " + video.LengthText.SimpleText)
					.AppendLine("ViewCount: " + video.ViewCountText.SimpleText)
					.AppendLine("ShortViewCount: " + video.ShortViewCountText.SimpleText)
					.AppendLine("PublishDate: " + video.PublishedTimeText.SimpleText)
					.AppendLine($"Badges: ({video.Badges.Count})" +
					            string.Join("", video.Badges.Select(x => $"\n- {SerializeRenderer(x)}")));
				return sb.ToString();
			}
			default:
				return $"[Unknown RendererCase={renderer.RendererCase}]";
		}
	}
}