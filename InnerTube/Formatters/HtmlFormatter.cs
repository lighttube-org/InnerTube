namespace InnerTube.Formatters;

public class HtmlFormatter : IFormatter
{
	public string FormatBold(string text) => $"<b>{text}</b>";

	public string FormatItalics(string text) => $"<i>{text}</i>";

	public string FormatUrl(string text, string url) => $"<a href=\"{url}\">{text}</a>";
}