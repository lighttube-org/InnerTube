using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("tr")]
public partial class Turkish : IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex shortNumberRegex = ShortNumberRegex();
	private Regex viewCountRegex = ViewCountRegex();

	public string ParseRelativeDate(string date)
	{
		throw new NotImplementedException();
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact($"{match.Groups[3].Value}/{match.Groups[2].Value}/{match.Groups[1].Value}",
			"yyyy/MMM/d", GetCultureInfo());
	}

	public VideoUploadType ParseVideoUploadType(string type) =>
		type.ToLower(GetCultureInfo()).Contains("yayınlandı")
			? VideoUploadType.Premiered
			: type.ToLower(GetCultureInfo()).Contains("başladı")
				? VideoUploadType.Streaming
				: type.ToLower(GetCultureInfo()).Contains("yapıldı")
					? VideoUploadType.Streamed
					: VideoUploadType.Published;

	public long ParseSubscriberCount(string subscriberCountText)
	{
		string digitsPart = subscriberCountText.Split(" ")[0];
		return ParseShortNumber(digitsPart);
	}

	public long ParseLikeCount(string likeCountText) =>
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) => int.Parse(viewCountRegex.Match(viewCountText).Groups[1].Value,
		NumberStyles.AllowThousands, GetCultureInfo());

	public DateTimeOffset ParseLastUpdated(string lastUpdatedText) =>
		ParseFullDate(lastUpdatedText.Split(" son ")[1].Split(" tarihinde ")[0]);

	private long ParseShortNumber(string part)
	{
		try
		{
			Match match = shortNumberRegex.Match(part);
			float value = float.Parse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint, GetCultureInfo());
			return (long)(match.Groups[2].Value switch
			{
				"B" => value * 1000,
				"Mn" => value * 1000000,
				"Mr" => value * 1000000000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

	private CultureInfo GetCultureInfo() => CultureInfo.GetCultureInfoByIetfLanguageTag("tr");

	[GeneratedRegex("(\\d{1,2}) (\\w+) (\\d{4})")]
	private static partial Regex FullDatePatternRegex();

	[GeneratedRegex("([\\d.,]+)\\s?(\\w*)")]
	private static partial Regex ShortNumberRegex();

	[GeneratedRegex("([\\d.]+)")]
	private static partial Regex ViewCountRegex();
}