using System.Reflection;

namespace InnerTube.Parsers;

public static class ValueParser
{
	private static Dictionary<string, IValueParser> languages = new();

	public static void Init()
	{
		if (languages.Count == 0)
			languages = Assembly.GetAssembly(typeof(ValueParser))!.GetTypes()
				.Where(x => x.IsAssignableTo(typeof(IValueParser)))
				.Select(x => (x.GetCustomAttribute<ValueParserAttribute>(), x))
				.Where(x => x.Item1 != null)
				.ToDictionary(x => x.Item1!.LanguageCode, x => (IValueParser)Activator.CreateInstance(x.x)!);
	}

	public static IValueParser GetParserForLocale(string language) =>
		!languages.TryGetValue(language, out IValueParser? parser)
			? throw new Exception($"Unknown language code '{language}'")
			: parser;

	public static string ParseRelativeDate(string languageCode, string date)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseRelativeDate(date);
		}
		catch
		{
			return $"!Failed to parse;;;{date}";
		}
	}

	public static DateTimeOffset ParseFullDate(string languageCode, string date)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseFullDate(date);
		}
		catch
		{
			return DateTimeOffset.UnixEpoch;
		}
	}

	public static VideoUploadType ParseVideoUploadType(string languageCode, string type)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseVideoUploadType(type);
		}
		catch
		{
			return VideoUploadType.Published;
		}
	}

	public static long ParseSubscriberCount(string languageCode, string subscriberCountText)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseSubscriberCount(subscriberCountText);
		}
		catch
		{
			return -1;
		}
	}

	public static long ParseLikeCount(string languageCode, string likeCountText)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseLikeCount(likeCountText);
		}
		catch
		{
			return -1;
		}
	}

	public static long ParseViewCount(string languageCode, string viewCountText)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseViewCount(viewCountText);
		}
		catch
		{
			return -1;
		}
	}

	public static long ParseVideoCount(string languageCode, string videoCountText)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseVideoCount(videoCountText);
		}
		catch
		{
			return -1;
		}
	}

	public static DateTimeOffset ParseLastUpdated(string languageCode, string lastUpdatedText)
	{
		try
		{
			return GetParserForLocale(languageCode).ParseLastUpdated(lastUpdatedText);
		}
		catch
		{
			return DateTimeOffset.UnixEpoch;
		}
	}
}