namespace InnerTube.Renderers;

public class MessageRendererData(string message): IRendererData
{
	public string Message { get; } = message;

	public override string ToString() => Message;
}