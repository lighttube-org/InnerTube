namespace InnerTube.Renderers;

public class UnknownRendererData : IRendererData
{
	public byte[]? ProtobufBytes { get; set; }
	public string? Json { get; set; }
}