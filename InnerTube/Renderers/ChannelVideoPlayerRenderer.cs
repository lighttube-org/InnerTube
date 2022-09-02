using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ChannelVideoPlayerRenderer : IRenderer
{
	public string Type => "channelVideoPlayerRenderer";

	public string Id { get; }
	public string Title { get; }
	public string Description { get; }
	public string? Published { get; }
	public string ViewCount { get; }

	public ChannelVideoPlayerRenderer(JToken renderer)
	{
		Id = renderer["videoId"]!.ToString();
		Title = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("title.runs") ?? new JArray(), false);
		Description = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("description.runs") ?? new JArray());
		Published = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("publishedTimeText.runs") ?? new JArray());
		ViewCount = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("viewCountText.runs") ?? new JArray());
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Published: {Published}")
			.AppendLine($"- ViewCount: {ViewCount}")
			.AppendLine(Description);

		return sb.ToString();
	}
}