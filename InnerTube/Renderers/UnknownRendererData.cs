namespace InnerTube.Renderers;

public class UnknownRendererData : IRendererData
{
	public byte[]? ProtobufBytes { get; set; }
	public string? Json { get; set; }

	public override string ToString() =>
		ProtobufBytes != null
			? "Unknown Protobuf Renderer:\n" + Convert.ToBase64String(ProtobufBytes)
			: Json != null
				? "Unexpected Renderer:\n" + Json
				: "Unknown or Unexpected renderer";
}