namespace InnerTube.Renderers;

public class RendererContainer
{
	public string Type { get; set; }
	public string OriginalType { get; set; }
	public IRendererData Data { get; set; }
}