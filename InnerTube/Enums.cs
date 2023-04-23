namespace InnerTube;

/// <summary>
/// The client to make the request from
/// </summary>
public enum RequestClient
{
	/// <summary>
	/// Web HTML client.
	/// </summary>
	WEB = 1,
	/// <summary>
	/// Android client. Useful for deciphered playback URLs
	/// </summary>
	ANDROID = 3,
	/// <summary>
	/// iOS client. Only useful for HLS manifests and nothing else.
	/// </summary>
	IOS = 5
}

/// <summary>
/// Type of the end screen item 
/// </summary>
public enum EndScreenItemType
{
	/// <summary>
	/// Video item.
	/// </summary>
	Video,
	/// <summary>
	/// Playlist item.
	/// </summary>
	Playlist,
	/// <summary>
	/// Channel icon.
	/// </summary>
	Subscribe,
	/// <summary>
	/// Channel icon.
	/// </summary>
	Channel,
	/// <summary>
	/// Link to an external source.
	/// </summary>
	Link
}

/// <summary>
/// Tabs of a channel's page
/// </summary>
public enum ChannelTabs
{
	/// <summary>
	/// Home tab.
	/// </summary>
	Home,
	/// <summary>
	/// Videos tab.
	/// </summary>
	Videos,
	/// <summary>
	/// Shorts tab.
	/// </summary>
	Shorts,
	/// <summary>
	/// Past live streams tab.
	/// </summary>
	Live,
	/// <summary>
	/// Playlists tab.
	/// </summary>
	Playlists,
	/// <summary>
	/// Podcasts tab.
	/// Not available on all channels.
	/// </summary>
	Podcasts,
	/// <summary>
	/// Releases tab.
	/// Only available in music channels [citation needed]
	/// </summary>
	Releases,
	/// <summary>
	/// Community tab.
	/// </summary>
	Community,
	/// <summary>
	/// Related channels tab.
	/// </summary>
	Channels,
	/// <summary>
	/// About tab.
	/// </summary>
	About,
	/// <summary>
	/// Search tab.
	/// </summary>
	Search
}

/// <summary>
/// Direction of the shelf
/// </summary>
public enum ShelfDirection
{
	/// <summary>
	/// Direction unknown
	/// </summary>
	None,
	/// <summary>
	/// Horizontal (card) shelf.
	/// </summary>
	Horizontal,
	/// <summary>
	/// Vertical shelf
	/// </summary>
	Vertical
}