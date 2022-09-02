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
		Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("title") ?? new JObject());
		Description = Utils.ReadText(renderer.GetFromJsonPath<JObject>("description") ?? new JObject(), true);
		Published = Utils.ReadText(renderer.GetFromJsonPath<JObject>("publishedTimeText") ?? new JObject());
		ViewCount = Utils.ReadText(renderer.GetFromJsonPath<JObject>("viewCountText") ?? new JObject());
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