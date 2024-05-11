using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("zh-TW")]
public partial class TraditionalChinese: IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex digitRegex = DigitRegex();
	private Regex shortNumberRegex = ShortNumberRegex();

	public string ParseRelativeDate(string date)
	{
		string importantPart = date.Replace(" ", "");
		string digits = digitRegex.Match(importantPart).Groups[1].Value;
		string metric = importantPart.Replace(digits, "");
		int amount = int.Parse(digits);
		return metric switch
		{
			"秒前" => $"-{amount}s",
			"分鐘前" => $"-{amount}m",
			"小時前" => $"-{amount}h",
			"天前" => $"-{amount}D",
			"週前" => $"-{amount}W",
			"個月前" => $"-{amount}M",
			"年前" => $"-{amount}Y",
			_ => $"!Unknown metric;{metric};{amount};{date}"
		};
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact(match.Groups[1].Value, "yyyy年M月d", null);
	}

	public VideoUploadType ParseVideoUploadType(string type)
	{
		if (type.Contains("首播日期")) return VideoUploadType.Premiered;
		if (type.Contains("開始直播日期")) return VideoUploadType.Streaming;
		if (type.Contains("串流直播日期")) return VideoUploadType.Streamed;
		return VideoUploadType.Published;
	}

	public long ParseSubscriberCount(string subscriberCountText)
	{
		string digitsPart = subscriberCountText.Replace("位订阅者", "");
		return ParseShortNumber(digitsPart);
	}

	public long ParseLikeCount(string likeCountText) => 
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) =>
		int.Parse(digitRegex.Match(viewCountText).Groups[1].Value, NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("zh-TW"));

	public DateTimeOffset ParseLastUpdated(string lastUpdatedText) => 
		ParseFullDate(lastUpdatedText);

	private long ParseShortNumber(string part)
	{
		try
		{
			Match match = shortNumberRegex.Match(part);
			float value = float.Parse(match.Groups[1].Value);
			return (long)(match.Groups[2].Value.ToUpper() switch
			{
				"億" => value * 100000000,
				"萬" => value * 10000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

	[GeneratedRegex("(\\d{4}.\\d{1,2}.\\d{1,2})")]
	private static partial Regex FullDatePatternRegex();
	[GeneratedRegex("([\\d,]+)")]
	private static partial Regex DigitRegex();

    [GeneratedRegex("([\\d.]+)([萬億]?)")]
    private static partial Regex ShortNumberRegex();
}