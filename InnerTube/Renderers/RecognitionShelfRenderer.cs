using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class RecognitionShelfRenderer : IRenderer
{
	public string Type => "recognitionShelfRenderer";

	public string Title { get; }
	public string Subtitle { get; }
	public IEnumerable<Thumbnail> Avatars { get; }


	public RecognitionShelfRenderer(JToken renderer)
	{
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		Subtitle = renderer.GetFromJsonPath<string>("subtitle.simpleText")!;
		Avatars = renderer.GetFromJsonPath<JArray>("avatars")!.Select(x =>
			Utils.GetThumbnails(x["thumbnails"]!.ToObject<JArray>()!)[0]);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Subtitle: {Subtitle}")
			.AppendLine("- Avatars:")
			.AppendLine("\t" + string.Join("\n\t", Avatars.Select(x => x.Url)));
		return sb.ToString();
	}
}