namespace InnerTube.Formatters;

/// <summary>
/// Formatter used to output rich texts in markdown format
/// </summary>
public class MarkdownFormatter : IFormatter
{
	/// <inheritdoc />
	public string FormatBold(string text) => $"**{text}**";

	/// <inheritdoc />
	public string FormatItalics(string text) => $"_{text}_";

	/// <inheritdoc />
	public string FormatUrl(string text, string url) => $"[{text}]({url})";

	/// <inheritdoc />
	public string HandleLineBreaks(string text) => text;
}