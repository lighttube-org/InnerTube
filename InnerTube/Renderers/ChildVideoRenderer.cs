using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ChildVideoRenderer : IRenderer
{
	public string Type => "childVideoRenderer";

	public string Id { get; }
	public string Title { get; }
	public TimeSpan Duration { get; }

	public ChildVideoRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = renderer["title"]!["simpleText"]!.ToString();
		Duration = Utils.ParseDuration(renderer["lengthText"]!["simpleText"]!.ToString());
	}

	public override string ToString() => $"[{Id}] {Title} | {Duration}";
}