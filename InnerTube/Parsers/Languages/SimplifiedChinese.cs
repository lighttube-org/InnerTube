using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("zh-CN")]
public partial class SimplifiedChinese: IValueParser
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
			"分钟前" => $"-{amount}m",
			"小时前" => $"-{amount}h",
			"天前" => $"-{amount}D",
			"周前" => $"-{amount}W",
			"个月前" => $"-{amount}M",
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
		if (type.Contains("首播开始于")) return VideoUploadType.Premiered;
		if (type.Contains("直播开始日期")) return VideoUploadType.Streamed;
		if (type.Contains("首播时间")) return VideoUploadType.FuturePremiere;
		if (type.Contains("预定发布时间")) return VideoUploadType.ScheduledStream;
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
		int.Parse(digitRegex.Match(viewCountText).Groups[1].Value, NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("zh-CN"));

	public long ParseVideoCount(string videoCountText) =>
		!videoCountText.Contains('无')
			? ParseShortNumber(videoCountText)
			: 0;

	public DateTimeOffset ParseLastUpdated(string lastUpdatedText) => 
		ParseFullDate(lastUpdatedText);

	private long ParseShortNumber(string part)
	{
		try
		{
			Match match = shortNumberRegex.Match(part);
			float value = float.Parse(match.Groups[1].Value,
				NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("zh-CN"));
			return (long)(match.Groups[2].Value.ToUpper() switch
			{
				"亿" => value * 100000000,
				"万" => value * 10000,
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
	[GeneratedRegex("([\\d.,]+)")]
	private static partial Regex DigitRegex();

    [GeneratedRegex("([\\d.,]+)([亿万]?)")]
    private static partial Regex ShortNumberRegex();
}