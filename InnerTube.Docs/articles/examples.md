# Examples

## Getting the video streams of a video to download or watch

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