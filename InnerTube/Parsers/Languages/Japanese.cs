using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("ja")]
public partial class Japanese : IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex shortNumberRegex = ShortNumberRegex();

	public string ParseRelativeDate(string date)
	{
		string[] parts = date.ToLower().Split(" ");
		string metric = parts[1].TrimEnd('前');
		int amount = int.Parse(parts[0]);
		return metric switch
		{
			"秒" => $"-{amount}s",
			"分" => $"-{amount}m",
			"時間" =>   $"-{amount}h",
			"日" =>    $"-{amount}D",
			"週間" =>   $"-{amount}W",
			"か月" =>  $"-{amount}M",
			"年" =>   $"-{amount}Y",
			_ => $"!Unknown metric;{metric};{amount};{date}"
		};
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact(match.Groups[1].Value, "yyyy/MM/d", null);
	}

	public VideoUploadType ParseVideoUploadType(string type)
	{
		
		if (type.Contains("に公開済み")) return VideoUploadType.Premiered;
		if (type.Contains("ライブ配信開始日")) return VideoUploadType.Streaming;
		if (type.Contains("にライブ配信")) return VideoUploadType.Streamed;
		return VideoUploadType.Published;
	}

	public long ParseSubscriberCount(string subscriberCountText)
	{
		string digitsPart = subscriberCountText.Split(" ")[1];
		return ParseShortNumber(digitsPart);
	}

	public long ParseLikeCount(string likeCountText) => 
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) =>
		int.Parse(viewCountText.Split(" ")[0], NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("ja"));

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
				"万" => value * 10000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

    [GeneratedRegex("(\\d{4}/\\d{2}/\\d{2})")]
    private static partial Regex FullDatePatternRegex();

    [GeneratedRegex("([\\d.]+)([億万]?)")]
    private static partial Regex ShortNumberRegex();
}