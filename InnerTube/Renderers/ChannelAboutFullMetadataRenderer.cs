using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ChannelAboutFullMetadataRenderer : IRenderer
{
	public string Type { get; }

	public string Id { get; }
	public string Title { get; }
	public Thumbnail[] Avatar { get; }
	public string Description { get; }
	public string ViewCount { get; }
	public string JoinedDate { get; }
	public IEnumerable<ChannelLink> PrimaryLinks { get; }
	public string Country { get; }

	public ChannelAboutFullMetadataRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();

		Id = renderer.GetFromJsonPath<string>("channelId")!;
		Title = renderer.GetFromJsonPath<string>("title.simpleText")!;
		Avatar = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("avatar.thumbnails")!);
		Description = renderer.GetFromJsonPath<string>("description.simpleText")!;
		ViewCount = renderer.GetFromJsonPath<string>("viewCountText.simpleText")!;
		JoinedDate = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("joinedDateText.runs")!);
		PrimaryLinks = renderer.GetFromJsonPath<JArray>("primaryLinks")!.Select(x => new ChannelLink(x));
		Country = renderer.GetFromJsonPath<string>("country.simpleText")!;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Id}] {Title}")
			.AppendLine($"Description: {Description}")
			.AppendLine($"AvatarCount: {Avatar.Length}")
			.AppendLine("Stats:")
			.AppendLine($"JoinedDate: {JoinedDate}")
			.AppendLine($"ViewCount: {ViewCount}")
			.AppendLine("Details:")
			.AppendLine($"Country: {Country}")
			.AppendLine("Links:");

		foreach (ChannelLink link in PrimaryLinks)
			sb.AppendLine(link.ToString());

		return sb.ToString();
	}
}