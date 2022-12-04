namespace InnerTube.Formatters;

public class HtmlFormatter : IFormatter
{
	public string FormatBold(string text) => $"<b>{Sanitize(text)}</b>";

	public string FormatItalics(string text) => $"<i>{Sanitize(text)}</i>";

	public string FormatUrl(string text, string url) => $"<a href=\"{url}\">{Sanitize(text)}</a>";
	public string HandleLineBreaks(string text) => text.Replace("\n", "<br>");

	private string Sanitize(string text) => text
		.Replace("<", "&lt;")
		.Replace(">", "&gt;");
}