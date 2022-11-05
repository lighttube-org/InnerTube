namespace InnerTube;

public enum RequestClient
{
	WEB = 1,
	ANDROID = 3,
	IOS = 5
}

public enum EndScreenItemType
{
	Video,
	Playlist,
	Subscribe,
	Channel,
	Link
}

public enum ChannelTabs
{
	Home,
	Videos,
	Shorts,
	Live,
	Playlists,
	Community,
	Channels,
	About,
	Search
}

public enum ShelfDirection
{
	None,
	Horizontal,
	Vertical
}

public enum FormattingType
{
	None,
	Html,
	Markdown
}