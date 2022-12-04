namespace InnerTube.Formatters;

public class MarkdownFormatter : IFormatter
{
	public string FormatBold(string text) => $"**{text}**";

	public string FormatItalics(string text) => $"_{text}_";

	public string FormatUrl(string text, string url) => $"[{text}]({url})";

	public string HandleLineBreaks(string text) => text;
}