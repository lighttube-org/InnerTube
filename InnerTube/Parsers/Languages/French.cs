using System.Globalization;
using System.Text.RegularExpressions;

namespace InnerTube.Parsers.Languages;

[ValueParser("fr")]
public partial class French : IValueParser
{
	private Regex fullDatePattern = FullDatePatternRegex();
	private Regex shortNumberRegex = ShortNumberRegex();

	public string ParseRelativeDate(string date)
	{
		string[] parts = date.ToLower().Split(' ');
		string[] nbspParts = parts[3].Split('\u00a0');
		string metric = nbspParts[1].TrimEnd('s');
		int amount = int.Parse(nbspParts[0]);
		return metric switch
		{
			"seconde" => $"-{amount}s",
			"minute" => $"-{amount}m",
			"heure" => $"-{amount}h",
			"jour" => $"-{amount}D",
			"semaine" => $"-{amount}W",
			"moi" => $"-{amount}M", // < not "mois" because of .TrimEnd
			"an" => $"-{amount}Y",
			_ => $"!Unknown metric;{metric};{amount};{date}"
		};
	}

	public DateTimeOffset ParseFullDate(string date)
	{
		Match match = fullDatePattern.Match(date);
		return DateTimeOffset.ParseExact(match.Groups[1].Value, "d MMM yyyy", new CultureInfo("fr"));
	}

	public VideoUploadType ParseVideoUploadType(string type)
	{
		return type.Split(' ')[0] switch
		{
			"Sortie" => VideoUploadType.Premiered,
			"Diffusion" => VideoUploadType.Streaming,
			"Diffusée" => VideoUploadType.Streamed,
			"Première" => VideoUploadType.FuturePremiere,
			"Planifié" => VideoUploadType.ScheduledStream,
			_ => VideoUploadType.Published
		};
	}

	public long ParseSubscriberCount(string subscriberCountText) => 
		// "d'abbonés" makes the regex confuse "M d'abonnés" wish "Md abonnés"
		ParseShortNumber(subscriberCountText.Replace("d’", ""));

	public long ParseLikeCount(string likeCountText) => 
		ParseShortNumber(likeCountText);

	public long ParseViewCount(string viewCountText) =>
		int.Parse(viewCountText.Split(" ")[0], NumberStyles.AllowThousands, new CultureInfo("fr"));

	public long ParseVideoCount(string videoCountText) =>
		!videoCountText.Contains("Aucune")
			? ParseShortNumber(videoCountText)
			: 0;

	public DateTimeOffset ParseLastUpdated(string lastUpdatedText) => 
		ParseFullDate(lastUpdatedText.Split(" le ")[1]);

	private long ParseShortNumber(string part)
	{
		try
		{
			Match match = shortNumberRegex.Match(part.Replace('\u00a0', ' ').Replace(" ", ""));
			float value = float.Parse(match.Groups[1].Value,
				NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, new CultureInfo("fr"));
			return (long)(match.Groups[2].Value switch
			{
				"k" => value * 1000,
				"M" => value * 1000000,
				"Md" => value * 1000000000,
				_ => value
			});
		}
		catch (Exception)
		{
			return -1;
		}
	}

    [GeneratedRegex(@"(\d{1,2} \w+\.? \d{4})")]
    private static partial Regex FullDatePatternRegex();

    [GeneratedRegex(@"([\d.,]+)(k|Md|M)?")]
    private static partial Regex ShortNumberRegex();
}