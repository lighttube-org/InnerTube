namespace InnerTube.Renderers;

public class ContinuationRendererData : IRendererData
{
	public string ContinuationToken { get; set; }

	public override string ToString() => ContinuationToken;
}