using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("de")]
public partial class German : IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex shortNumberRegex = ShortNumberRegex();

	public string ParseRelativeDate(string date)
	{
		string[] parts = date.ToLower().Split(" ");
		string metric = parts[2].TrimEnd('e', 'n');
		int amount = int.Parse(parts[1]);
		return metric switch
		{
			"sekund" => $"-{amount}s",
			"minut" => $"-{amount}m",
			"stund" =>   $"-{amount}h",
			"tag" =>    $"-{amount}D",
			"woch" =>   $"-{amount}W",
			"monat" =>  $"-{amount}M",
			"jahr" =>   $"-{amount}Y",
			_ => $"!Unknown metric;{metric};{amount};{date}"
		};
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact(match.Groups[1].Value, "dd.MM.yyyy", null);
	}

	public VideoUploadType ParseVideoUploadType(string type)
	{
		return type.Split(' ')[0] switch
		{
			"Premiere" => VideoUploadType.Premiered,
			"Livestream" => VideoUploadType.Streaming,
			"Live" => VideoUploadType.Streamed,
			"Geplant" => VideoUploadType.ScheduledStream,
			_ => VideoUploadType.Published
		};
	}

	public long ParseSubscriberCount(string subscriberCountText) => ParseShortNumber(subscriberCountText);

	public long ParseLikeCount(string likeCountText) => 
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) =>
		int.Parse(string.Join("", viewCountText.Where(char.IsDigit)));


	public long ParseVideoCount(string videoCountText) =>
		!videoCountText.Contains("Keine")
			? ParseShortNumber(videoCountText)
			: 0;

	public DateTimeOffset ParseLastUpdated(string lastUpdatedText) => ParseFullDate(lastUpdatedText);

	private long ParseShortNumber(string part)
	{
		try
		{
			Match match = shortNumberRegex.Match(part);
			float value = float.Parse(match.Groups[1].Value,
				NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, new CultureInfo("de"));
			return (long)(match.Groups[2].Value switch
			{
				" Mrd." => value * 1000000000,
				" Mio." => value * 1000000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

    [GeneratedRegex("(\\d{1,2}.\\d{1,2}.\\d{4})")]
    private static partial Regex FullDatePatternRegex();

    [GeneratedRegex(@"([\d.,]+)( (?:Mio|Mrd)\.)?")]
    private static partial Regex ShortNumberRegex();
}