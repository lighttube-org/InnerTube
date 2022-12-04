namespace InnerTube.Formatters;

public interface IFormatter
{
	public string FormatBold(string text);
	public string FormatItalics(string text);
	public string FormatUrl(string text, string url);
	public string HandleLineBreaks(string text);
}