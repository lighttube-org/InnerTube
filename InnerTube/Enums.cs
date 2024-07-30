﻿using InnerTube.Models;

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
	IOS = 5,
	TVAPPLE = 16,
	MWEB_TIER_2 = 27,
	TV_EMBEDDED = 85
}

/// <summary>
/// Type of the end screen item 
/// </summary>
public enum EndscreenItemType
{
	/// <summary>
	/// Video item.
	/// </summary>
	Video = 1,
	/// <summary>
	/// Playlist item.
	/// </summary>
	Playlist = 2,
	/// <summary>
	/// Channel icon.
	/// </summary>
	Subscribe,
	/// <summary>
	/// Channel icon.
	/// </summary>
	Channel = 3,
	/// <summary>
	/// Link to an external source.
	/// </summary>
	Link = 4
}

/// <summary>
/// Tabs of a channel's page
/// </summary>
public enum ChannelTabs
{
	/// <summary>
	/// Unknown tab. Use <see cref="ChannelTab.Params"/>
	/// </summary>
	Unknown,
	/// <summary>
	/// Home tab.
	/// </summary>
	Featured,
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
	Streams,
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
	[Obsolete("Doesn't seem to work anymore.")]
	Channels,
	/// <summary>
	/// Store tab.
	/// </summary>
	Store,
	/// <summary>
	/// About tab.
	/// </summary>
	[Obsolete("Doesn't seem to work anymore.")]
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
	Vertical,
	/// <summary>
	/// Grid
	/// </summary>
	Grid
}

public enum PlaylistFilter
{
	All = 0,
	Videos = 1,
	Shorts = 2
}

public enum VideoUploadType
{
	Published = 0,
	Premiered = 1,
	Streamed = 2,
	Streaming = 3,
	FuturePremiere = 4,
	ScheduledStream = 5
}

public enum SearchSidebarType
{
	Unknown = 0,
	Artist = 1,
	Album = 2,
	TvShow = 3
}