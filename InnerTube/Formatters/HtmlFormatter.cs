using System.Web;

namespace InnerTube.Formatters;

/// <summary>
/// Formatter used to output rich texts in HTML format
/// </summary>
public class HtmlFormatter : IFormatter
{
	/// <inheritdoc />
	public string FormatBold(string text) => $"<b>{text}</b>";

	/// <inheritdoc />
	public string FormatItalics(string text) => $"<i>{text}</i>";

	/// <inheritdoc />
	public string FormatUrl(string text, string url) => $"<a href=\"{url}\">{text}</a>";

	/// <inheritdoc />
	public string HandleLineBreaks(string text) => text.Replace("\n", "<br>");

	/// <inheritdoc />
	public string Sanitize(string text) => HttpUtility.HtmlEncode(text);
}