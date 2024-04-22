using InnerTube.Protobuf;

namespace InnerTube.Models;

public readonly struct Channel(
	string id,
	string title,
	string handle,
	Thumbnails? avatar,
	string? subscribersText)
{
	public string Id { get; } = id;
	public string Title { get; } = title;
	public string Handle { get; } = handle;
	public Thumbnails? Avatar { get; } = avatar;
	public string? SubscribersText { get; } = subscribersText;
}