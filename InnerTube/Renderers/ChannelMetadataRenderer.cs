using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class ChannelMetadataRenderer : IRenderer
{
	public string Type => "channelMetadataRenderer";

	public string Id { get; }
	public string[] AvailableCountryCodes { get; }
	public Thumbnail[] Avatar { get; }
	public string Description { get; }
	public bool IsFamilySafe { get; }
	public string Keywords { get; }
	public string VanityChannelUrl { get; }
	public string Title { get; }
	public string RssUrl { get; }

	public ChannelMetadataRenderer(JToken renderer)
	{
		Id = renderer.GetFromJsonPath<string>("externalId")!;
		AvailableCountryCodes = renderer.GetFromJsonPath<string[]>("availableCountryCodes")!;
		Avatar = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("avatar.thumbnails")!);
		Description = renderer.GetFromJsonPath<string>("description")!;
		IsFamilySafe = renderer.GetFromJsonPath<bool>("isFamilySafe")!;
		Keywords = renderer.GetFromJsonPath<string>("keywords")!;
		VanityChannelUrl = renderer.GetFromJsonPath<string>("vanityChannelUrl")!;
		Title = renderer.GetFromJsonPath<string>("title")!;
		RssUrl = renderer.GetFromJsonPath<string>("rssUrl")!;
	}

	public override string ToString() =>
		new StringBuilder()
			.AppendLine($"[{Id}] {Title}")
			.AppendLine(
				$"AvailableCountryCodes: {string.Join(", ", AvailableCountryCodes.Take(5))} (and {AvailableCountryCodes.Length - 5} more)")
			.AppendLine($"AvatarCount: {Avatar.Length}")
			.AppendLine($"Description: {Description}")
			.AppendLine($"IsFamilySafe: {IsFamilySafe}")
			.AppendLine($"Keywords: {Keywords}")
			.AppendLine($"VanityChannelUrl: {VanityChannelUrl}")
			.AppendLine($"RssUrl: {RssUrl}")
			.ToString();
}