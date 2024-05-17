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

	public static string ParseRelativeDate(string languageCode, string date) =>
		GetParserForLocale(languageCode).ParseRelativeDate(date);

	public static DateTimeOffset ParseFullDate(string languageCode, string date) =>
		GetParserForLocale(languageCode).ParseFullDate(date);

	public static VideoUploadType ParseVideoUploadType(string languageCode, string type) =>
		GetParserForLocale(languageCode).ParseVideoUploadType(type);

	public static long ParseSubscriberCount(string languageCode, string subscriberCountText) =>
		GetParserForLocale(languageCode).ParseSubscriberCount(subscriberCountText);

	public static long ParseLikeCount(string languageCode, string likeCountText) =>
		GetParserForLocale(languageCode).ParseLikeCount(likeCountText);

	public static long ParseViewCount(string languageCode, string viewCountText) =>
		GetParserForLocale(languageCode).ParseViewCount(viewCountText);

	public static DateTimeOffset ParseLastUpdated(string languageCode, string lastUpdatedText) =>
		GetParserForLocale(languageCode).ParseLastUpdated(lastUpdatedText);
}