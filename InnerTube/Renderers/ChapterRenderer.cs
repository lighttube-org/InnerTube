using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ChapterRenderer : IRenderer
{
	public string Type => "chapterRenderer";
	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public ulong TimeRangeStartMillis { get; }

	public ChapterRenderer(JToken renderer)
	{
		Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("title"));
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		TimeRangeStartMillis = renderer.GetFromJsonPath<ulong>("timeRangeStartMillis");
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- TimeRangeStartMillis: ({TimeSpan.FromMilliseconds(TimeRangeStartMillis)}) {TimeRangeStartMillis}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}");

		return sb.ToString();
	}
}