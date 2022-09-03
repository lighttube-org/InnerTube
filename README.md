# InnerTube

A wrapper for the private YouTube API used in the YouTube website and the mobile apps.

# Comparison to the YouTube Data v3 API

The official API requires a Google account to use, while the InnerTube API can do it without the need of one.

The official API has a request quota and a request rate limit, while the InnerTube API does not have any limitations.

The official API is designed to be used by 3rd parties, thus, the data is formatted well. Since the InnerTube API is made to be used in the official YouTube apps, it is formatted to render elements on a screen. This makes it slightly harder to parse the output to a human readable format, which this library tries to do.

# Authentication

There's no straightforward way to authenticate with this library. You will either have to extract cookies from a browser session, or extract a refresh token. See the wiki page for more details.

# Usage

## Getting the video streams of a video

```csharp
InnerTube innerTube = new InnerTube();
InnerTubePlayer player = await innerTube.GetPlayerAsync("VIDEO_ID");
Console.WriteLine(player.Formats.First().Url);
// https://rr4---sn-u0g3uxax3-txpl.googlevideo.com/videoplayback?expire=166...
```

## Getting comments of a video

```csharp
InnerTubeNextResponse next = await _innerTube.GetVideoAsync(videoId);
InnerTubeContinuationResponse comments = await _innerTube.GetVideoCommentsAsync(next.CommentsContinuation!);
foreach (IRenderer renderer in comments.Contents)
    Console.WriteLine(renderer.ToString());
// [COMMENT_ID] [USER_ID] CoolUser: "This is a really cool video and this is definitely a real comment!" | 22.7M likes
```

## Searching for videos

```csharp
InnerTubeSearchResults search = await _innerTube.SearchAsync(query, param);
foreach (IRenderer renderer in results.Results)
    Console.WriteLine(renderer.ToString());
// [videoRenderer] Big Buck Bunny 60fps 4K - Official Blender Foundation Short Film
// - Id: aqz-KE-bpKQ
// - Duration: 00:10:35
// - Published: 7 years ago
// - ViewCount: 15,839,114 views
// - Thumbnail count: 2
// - Channel: [UCSMOQeBJ2RAnuFungnQOxLg] Blender | Avatar: https://yt3.ggpht.com/ytc/AMLnZu-1MGeaE5pvjNQytK6_O9fjbUEcr2xmmM1XBbOHGQ=s68-c-k-c0x00ffffff-no-rj | Badges: [BADGE_STYLE_TYPE_VERIFIED] (CHECK_CIRCLE_THICK) (no label), Verified
// - Badges: [BADGE_STYLE_TYPE_SIMPLE] , 4K
// Enjoy this UHD High Frame rate version of one of the iconic short films produced by Blender Institute! Learn more about the...
// ...
```

More examples can be found in the InnerTube.Tests folder