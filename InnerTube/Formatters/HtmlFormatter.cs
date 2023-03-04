namespace InnerTube.Formatters;

/// <summary>
/// Formatter used to output rich texts in HTML format
/// </summary>
public class HtmlFormatter : IFormatter
{
	/// <inheritdoc />
	public string FormatBold(string text) => $"<b>{Sanitize(text)}</b>";

	/// <inheritdoc />
	public string FormatItalics(string text) => $"<i>{Sanitize(text)}</i>";

	/// <inheritdoc />
	public string FormatUrl(string text, string url) => $"<a href=\"{url}\">{Sanitize(text)}</a>";

	/// <inheritdoc />
	public string HandleLineBreaks(string text) => text.Replace("\n", "<br>");

	private string Sanitize(string text) => text
		.Replace("<", "&lt;")
		.Replace(">", "&gt;");
}