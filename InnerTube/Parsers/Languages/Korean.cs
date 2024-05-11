using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("ko")]
public partial class Korean : IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex digitRegex = DigitRegex();
	private Regex shortNumberRegex = ShortNumberRegex();

	public string ParseRelativeDate(string date)
	{
		string[] parts = date.ToLower().Split(" ");
		string importantPart = parts[0];
		string digits = digitRegex.Match(importantPart).Groups[1].Value;
		string metric = importantPart.Replace(digits, "");
		int amount = int.Parse(digits);
		return metric switch
		{
			"초" => $"-{amount}s",
			"분" => $"-{amount}m",
			"시간" => $"-{amount}h",
			"일" => $"-{amount}D",
			"주" => $"-{amount}W",
			"개월" => $"-{amount}M",
			"년" => $"-{amount}Y",
			_ => $"!Unknown metric;{metric};{amount};{date}"
		};
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact(match.Groups[1].Value, "yyyy. M. d", null);
	}

	public VideoUploadType ParseVideoUploadType(string type)
	{
		if (type.Contains("최초 공개")) return VideoUploadType.Premiered;
		if (type.Contains("실시간 스트리밍 시작일")) return VideoUploadType.Streaming;
		if (type.Contains("스트리밍 시작일")) return VideoUploadType.Streamed;
		return VideoUploadType.Published;
	}

	public long ParseSubscriberCount(string subscriberCountText)
	{
		string digitsPart = subscriberCountText.Split(" ")[1].TrimEnd('명');
		return ParseShortNumber(digitsPart);
	}

	public long ParseLikeCount(string likeCountText) => 
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) =>
		int.Parse(digitRegex.Match(viewCountText).Groups[1].Value, NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("ko"));

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
				"억" => value * 100000000,
				"만" => value * 10000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

	[GeneratedRegex("(\\d{4}\\. \\d{1,2}\\. \\d{1,2})")]
	private static partial Regex FullDatePatternRegex();
	[GeneratedRegex("([\\d,]+)")]
	private static partial Regex DigitRegex();

    [GeneratedRegex("([\\d.]+)([억만]?)")]
    private static partial Regex ShortNumberRegex();
}