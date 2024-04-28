namespace InnerTube.Renderers;

public class ContinuationRenderer : IRendererData
{
	public string ContinuationToken { get; set; }

	public override string ToString() => ContinuationToken;
}