using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class UnknownRenderer : IRenderer
{
	public string Type { get; }
	public JToken Json { get; }

	public UnknownRenderer(JToken renderer)
	{
		Json = renderer;
		Type = renderer.Path.Split(".").Last();
	}

	public UnknownRenderer(JToken renderer, string type)
	{
		Json = renderer;
		Type = type;
	}

	public override string ToString()
	{
		return $"Unknown renderer of type: {Type}. JSON:\n\t{Json.ToString(Formatting.None)}";
	}
}