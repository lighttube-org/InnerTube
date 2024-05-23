using InnerTube.Protobuf;

namespace InnerTube.Models;

public class ChannelMetadata(ChannelMetadataRenderer metadata)
{
	public string Id { get; set; } = metadata.ExternalId;
	public string Title { get; set; } = metadata.Title;
	public string Description { get; set; } = metadata.Description;
	public string Handle { get; set; } = metadata.VanityChannelUrl;
	public string RssUrl { get; set; } = metadata.RssUrl;
	public string ChannelUrl { get; set; } = metadata.ChannelUrl;
	public string Keywords { get; set; } = metadata.Keywords;
	public string[] AvailableCountryCodes { get; set; } = metadata.AvailableCountryCodes.ToArray();
	public string AvatarUrl { get; set; } = metadata.Avatar.Thumbnails_.Last().Url;
}