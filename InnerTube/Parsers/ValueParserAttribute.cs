namespace InnerTube.Parsers;

public class ValueParserAttribute(string language) : Attribute
{
	public string LanguageCode { get; } = language;
}