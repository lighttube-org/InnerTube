namespace InnerTube.Formatters;

/// <summary>
/// Formatter base.<br></br>
/// Formatters are used to convert the rich texts received
/// from YouTube to another format (such as HTML or Markdown) <br></br>
/// See <see cref="HtmlFormatter"/> or  <see cref="MarkdownFormatter"/> for example implementations
/// </summary>
public interface IFormatter
{
	/// <summary>
	/// Format text in bold
	/// </summary>
	/// <param name="text">Text to format in bold</param>
	/// <returns>Bold formatted text</returns>
	public string FormatBold(string text);
	
	/// <summary>
	/// Format text in italics
	/// </summary>
	/// <param name="text">Text to format in italics</param>
	/// <returns>Italic formatted text</returns>
	public string FormatItalics(string text);
	
	/// <summary>
	/// Format an URL
	/// </summary>
	/// <param name="text">The visible text</param>
	/// <param name="url">The URL the link goes to</param>
	/// <returns>URL formatted text</returns>
	public string FormatUrl(string text, string url);
	
	/// <summary>
	/// Handle & fix any line breaks in the text (e.g: converting \n to &lt;br&gt;)
	/// </summary>
	/// <param name="text">Full text to fix line breaks in</param>
	/// <returns>The same text with line breaks fixed</returns>
	public string HandleLineBreaks(string text);
}