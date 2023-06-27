using System.Text;

namespace InnerTube.Renderers;

public class ExceptionRenderer : IRenderer
{
	public string Type => "InnerTubeExceptionRenderer";

	public string Message { get; }
	public string? StackTrace { get; }
	public string? Renderer { get; }

	public ExceptionRenderer(Exception e, string? renderer)
	{
		e = e.GetBaseException();
		Message = e.Message;
		StackTrace = e.StackTrace;
		Renderer = renderer;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Message} on {Renderer}")
			.AppendLine(StackTrace);

		return sb.ToString();
	}
}