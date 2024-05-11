using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("az")]
public partial class Azerbaijan : IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex shortNumberRegex = ShortNumberRegex();
	private Regex viewCountRegex = ViewCountRegex();

	public string ParseRelativeDate(string date)
	{
		string[] parts = date.ToLower().Split(" ");
		string metric = parts[1];
		int amount = int.Parse(parts[0]);
		return metric switch
		{
			"saniyə" => $"-{amount}s",
			"dəqiqə" => $"-{amount}m",
			"saat" =>   $"-{amount}h",
			"gün" =>    $"-{amount}D",
			"həftə" =>   $"-{amount}W",
			"ay" =>  $"-{amount}M",
			"il" =>   $"-{amount}Y",
			_ => $"!Unknown metric;{metric};{amount};{date}"
		};
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact($"{match.Groups[3].Value}/{match.Groups[2].Value}/{match.Groups[1].Value}",
			"yyyy/MMM/d", GetCultureInfo());
	}

	public VideoUploadType ParseVideoUploadType(string type)
	{
		// Turkish doesn't have a difference between premieres and streams, defaulting to "streamed" for all
		return type.ToLower(GetCultureInfo()).Contains("yayınlanır")
			? VideoUploadType.Streamed
			: type.ToLower(GetCultureInfo()).Contains("başladı")
				? VideoUploadType.Streaming
				: type.ToLower(GetCultureInfo()).Contains("premyera")
					? VideoUploadType.Premiered
					: VideoUploadType.Published;
	}

	public long ParseSubscriberCount(string subscriberCountText) => 
		ParseShortNumber(subscriberCountText);

	public long ParseLikeCount(string likeCountText) =>
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) => int.Parse(viewCountRegex.Match(viewCountText).Groups[1].Value,
		NumberStyles.AllowThousands, GetCultureInfo());

	public DateTimeOffset ParseLastUpdated(string lastUpdatedText) =>
		ParseFullDate(lastUpdatedText.Split(": ")[1]);

	private long ParseShortNumber(string part)
	{
		try
		{
			Match match = shortNumberRegex.Match(part);
			float value = match.Groups[1].Value.ToUpper().EndsWith('K')
				? float.Parse(match.Groups[1].Value.TrimEnd('K'), GetCultureInfo()) * 1000
				: float.Parse(match.Groups[1].Value, GetCultureInfo());
			return (long)(match.Groups[2].Value switch
			{
				"mln" => value * 1000000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

	private CultureInfo GetCultureInfo() => CultureInfo.GetCultureInfoByIetfLanguageTag("az");

	[GeneratedRegex("(\\d{1,2}) (\\w+) (\\d{4})")]
	private static partial Regex FullDatePatternRegex();

	[GeneratedRegex("([\\d,]+K?)\\s?(\\w+)")]
	private static partial Regex ShortNumberRegex();

	[GeneratedRegex("([\\d.]+)")]
	private static partial Regex ViewCountRegex();
}