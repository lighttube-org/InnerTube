using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("en-IN")]
public partial class IndianEnglish : IValueParser
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
		return DateTimeOffset.ParseExact($"{match.Groups[3].Value}/{match.Groups[2].Value}/{match.Groups[1].Value}",
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
			string number = match.Groups[1].Value.ToUpper();
			float value = number.EndsWith('K')
				? float.Parse(match.Groups[1].Value) * 1000
				: float.Parse(match.Groups[1].Value);
			return (long)(match.Groups[2].Value.ToLower() switch
			{
				"lakh" => value * 100000,
				"crore" => value * 10000000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

	[GeneratedRegex("(\\d{1,2}) (\\w+) (\\d{4})")]
    private static partial Regex FullDatePatternRegex();

    [GeneratedRegex("([\\d.kK]+)\\s?(\\w*)")]
    private static partial Regex ShortNumberRegex();
}