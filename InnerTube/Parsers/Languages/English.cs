using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("en")]
public partial class English : IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex shortNumberRegex = ShortNumberRegex();

	public string ParseRelativeDate(string date)
	{
		string[] parts = date.ToLower().Split(" ");
		string metric = parts[1].TrimEnd('s');
		int amount = int.Parse(parts[0]);
		return metric switch
		{
			"second" => $"-{amount}s",
			"minute" => $"-{amount}m",
			"hour" =>   $"-{amount}h",
			"day" =>    $"-{amount}D",
			"week" =>   $"-{amount}W",
			"month" =>  $"-{amount}M",
			"year" =>   $"-{amount}Y",
			_ => $"!Unknown metric;{metric};{amount};{date}"
		};
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact($"{match.Groups[3].Value}/{match.Groups[1].Value}/{match.Groups[2].Value}",
			"yyyy/MMM/d", null);
	}

	public VideoUploadType ParseVideoUploadType(string type)
	{
		return type.Split(' ')[0] switch
		{
			"Premiered" => VideoUploadType.Premiered,
			"Started" => VideoUploadType.Streaming,
			"Streamed" => VideoUploadType.Streamed,
			_ => VideoUploadType.Published
		};
	}

	public long ParseSubscriberCount(string subscriberCountText)
	{
		string digitsPart = subscriberCountText.Split(" ")[0];
		return ParseShortNumber(digitsPart);
	}

	public long ParseLikeCount(string likeCountText) => 
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) =>
		int.Parse(viewCountText.Split(" ")[0], NumberStyles.AllowThousands);

	public DateTimeOffset ParseLastUpdated(string lastUpdatedText) => 
		ParseFullDate(lastUpdatedText.Split(" on ")[1]);

	private long ParseShortNumber(string part)
	{
		try
		{
			Match match = shortNumberRegex.Match(part);
			float value = float.Parse(match.Groups[1].Value);
			return (long)(match.Groups[2].Value.ToUpper() switch
			{
				"K" => value * 1000,
				"M" => value * 1000000,
				"B" => value * 1000000000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

    [GeneratedRegex("(\\w+) (\\d{1,2}), (\\d{4})")]
    private static partial Regex FullDatePatternRegex();

    [GeneratedRegex("([\\d.]+)([KMB]?)")]
    private static partial Regex ShortNumberRegex();
}