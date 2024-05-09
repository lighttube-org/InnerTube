using System.Text;
using System.Text.Json;
using InnerTube.Parsers;

namespace InnerTube.Tests;

public class ValueParserTests
{
	public Dictionary<string, Dictionary<string, string[]>> testData;

	[OneTimeSetUp]
	public async Task Setup()
	{
		string url =
			"https://gist.githubusercontent.com/kuylar/2cc4abb51a04def25d3914c7bc236424/raw/71c6fba16739bf6e790e42da4a353b02aed71b1a/out.json";
		string json = await new HttpClient().GetStringAsync(url);
		testData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string[]>>>(json);
		ValueParser.Init();
	}

	[TestCase("af", TestName = "Afrikaans")]
	[TestCase("az", TestName = "Azərbaycan")]
	[TestCase("id", TestName = "Bahasa Indonesia")]
	[TestCase("ms", TestName = "Bahasa Malaysia")]
	[TestCase("bs", TestName = "Bosanski")]
	[TestCase("ca", TestName = "Català")]
	[TestCase("cs", TestName = "Čeština")]
	[TestCase("da", TestName = "Dansk")]
	[TestCase("de", TestName = "Deutsch")]
	[TestCase("et", TestName = "Eesti")]
	[TestCase("en-IN", TestName = "English (India)")]
	[TestCase("en-GB", TestName = "English (UK)")]
	[TestCase("en", TestName = "English (US)")]
	[TestCase("es", TestName = "Español (España)")]
	[TestCase("es-419", TestName = "Español (Latinoamérica)")]
	[TestCase("es-US", TestName = "Español (US)")]
	[TestCase("eu", TestName = "Euskara")]
	[TestCase("fil", TestName = "Filipino")]
	[TestCase("fr", TestName = "Français")]
	[TestCase("fr-CA", TestName = "Français (Canada)")]
	[TestCase("gl", TestName = "Galego")]
	[TestCase("hr", TestName = "Hrvatski")]
	[TestCase("zu", TestName = "IsiZulu")]
	[TestCase("is", TestName = "Íslenska")]
	[TestCase("it", TestName = "Italiano")]
	[TestCase("sw", TestName = "Kiswahili")]
	[TestCase("lv", TestName = "Latviešu valoda")]
	[TestCase("lt", TestName = "Lietuvių")]
	[TestCase("hu", TestName = "Magyar")]
	[TestCase("nl", TestName = "Nederlands")]
	[TestCase("no", TestName = "Norsk")]
	[TestCase("uz", TestName = "O‘zbek")]
	[TestCase("pl", TestName = "Polski")]
	[TestCase("pt-PT", TestName = "Português")]
	[TestCase("pt", TestName = "Português (Brasil)")]
	[TestCase("ro", TestName = "Română")]
	[TestCase("sq", TestName = "Shqip")]
	[TestCase("sk", TestName = "Slovenčina")]
	[TestCase("sl", TestName = "Slovenščina")]
	[TestCase("sr-Latn", TestName = "Srpski")]
	[TestCase("fi", TestName = "Suomi")]
	[TestCase("sv", TestName = "Svenska")]
	[TestCase("vi", TestName = "Tiếng Việt")]
	[TestCase("tr", TestName = "Türkçe")]
	[TestCase("be", TestName = "Беларуская")]
	[TestCase("bg", TestName = "Български")]
	[TestCase("ky", TestName = "Кыргызча")]
	[TestCase("kk", TestName = "Қазақ Тілі")]
	[TestCase("mk", TestName = "Македонски")]
	[TestCase("mn", TestName = "Монгол")]
	[TestCase("ru", TestName = "Русский")]
	[TestCase("sr", TestName = "Српски")]
	[TestCase("uk", TestName = "Українська")]
	[TestCase("el", TestName = "Ελληνικά")]
	[TestCase("hy", TestName = "Հայերեն")]
	[TestCase("iw", TestName = "עברית")]
	[TestCase("ur", TestName = "اردو")]
	[TestCase("ar", TestName = "العربية")]
	[TestCase("fa", TestName = "فارسی")]
	[TestCase("ne", TestName = "नेपाली")]
	[TestCase("mr", TestName = "मराठी")]
	[TestCase("hi", TestName = "हिन्दी")]
	[TestCase("as", TestName = "অসমীয়া")]
	[TestCase("bn", TestName = "বাংলা")]
	[TestCase("pa", TestName = "ਪੰਜਾਬੀ")]
	[TestCase("gu", TestName = "ગુજરાતી")]
	[TestCase("or", TestName = "ଓଡ଼ିଆ")]
	[TestCase("ta", TestName = "தமிழ்")]
	[TestCase("te", TestName = "తెలుగు")]
	[TestCase("kn", TestName = "ಕನ್ನಡ")]
	[TestCase("ml", TestName = "മലയാളം")]
	[TestCase("si", TestName = "සිංහල")]
	[TestCase("th", TestName = "ภาษาไทย")]
	[TestCase("lo", TestName = "ລາວ")]
	[TestCase("my", TestName = "ဗမာ")]
	[TestCase("ka", TestName = "ქართული")]
	[TestCase("am", TestName = "አማርኛ")]
	[TestCase("km", TestName = "ខ្មែរ")]
	[TestCase("zh-CN", TestName = "中文 (简体)")]
	[TestCase("zh-TW", TestName = "中文 (繁體)")]
	[TestCase("zh-HK", TestName = "中文 (香港)")]
	[TestCase("ja", TestName = "日本語")]
	[TestCase("ko", TestName = "한국어")]
	public void TestLocale(string language)
	{
		IValueParser parser;
		try
		{
			parser = ValueParser.GetParserForLocale(language);
		}
		catch (Exception)
		{
			Assert.Inconclusive("Failed to get parser");
			return;
		}

		if (!testData.TryGetValue(language, out Dictionary<string, string[]>? testValues))
			Assert.Inconclusive($"Failed to find test values for '{language}'");
		StringBuilder sb = new();
		sb.AppendLine($"Language: {language}");

		sb.AppendLine("\nSubscriber counts:");
		foreach (string value in testValues["subscriberCounts"])
			sb.AppendLine(
				$"{$"{value}".PadLeft(testValues["subscriberCounts"].Max(x => x.Length))}: {parser.ParseSubscriberCount(value)}");

		sb.AppendLine("\nVideo upload dates & video publish types:");
		foreach (string value in testValues["videoDates"])
			sb.AppendLine(
				$"{$"{value}".PadLeft(testValues["videoDates"].Max(x => x.Length))}: {parser.ParseVideoUploadType(value)}, {parser.ParseFullDate(value):dd/MM/yyyy}");

		sb.AppendLine("\nView counts:");
		foreach (string value in testValues["viewCounts"])
			sb.AppendLine(
				$"{$"{value}".PadLeft(testValues["viewCounts"].Max(x => x.Length))}: {parser.ParseViewCount(value)}");

		sb.AppendLine("\nLike counts:");
		foreach (string value in testValues["likeCounts"])
			sb.AppendLine(
				$"{$"{value}".PadLeft(testValues["likeCounts"].Max(x => x.Length))}: {parser.ParseLikeCount(value)}");

		sb.AppendLine("\nLast updated dates:");
		foreach (string value in testValues["lastUpdatedDates"])
			sb.AppendLine(
				$"{$"{value}".PadLeft(testValues["lastUpdatedDates"].Max(x => x.Length))}: {parser.ParseLastUpdated(value):dd/MM/yyyy}");

		Assert.Pass(sb.ToString());
	}
}