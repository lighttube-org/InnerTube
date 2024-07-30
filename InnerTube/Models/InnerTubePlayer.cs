using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;

namespace InnerTube.Models;

public class InnerTubePlayer(PlayerResponse player, bool isFallback, string parserLanguage)
{
	public VideoDetails Details { get; } = new(player, isFallback, parserLanguage);

	public VideoEndscreen? Endscreen { get; } =
		player.Endscreen?.EndscreenRenderer != null ? new VideoEndscreen(player.Endscreen.EndscreenRenderer) : null;

	public VideoStoryboard? Storyboard { get; } = player.Storyboards != null
		? new(player.Storyboards, player.VideoDetails.LengthSeconds)
		: null;

	public VideoCaption[] Captions { get; } =
		player.Captions?.CaptionsTrackListRenderer.Captions.Select(x => new VideoCaption(x)).ToArray() ??
		[];

	// TODO: complete formats
	public Format[] Formats { get; } = player.StreamingData?.Formats.ToArray() ?? [];
	public Format[] AdaptiveFormats { get; } = player.StreamingData?.AdaptiveFormats.ToArray() ?? [];
	public Format[] HlsFormats { get; } = player.StreamingData?.HlsFormats.ToArray() ?? [];
	public DateTimeOffset ExpiryTimeStamp { get; } = DateTimeOffset.UtcNow.AddSeconds(player.StreamingData?.ExpiresInSeconds ?? -1);
	public string? HlsManifestUrl { get; } = player.StreamingData?.HlsManifestUrl; 
	public string? DashManifestUrl { get; } = player.StreamingData?.DashManifestUrl; 

	public class VideoDetails(PlayerResponse player, bool isFallback, string parserLanguage)
	{
		public string Id { get; } = player.VideoDetails!.VideoId;
		public string Title { get; } = player.VideoDetails!.Title;
		public string[] Keywords { get; } = player.VideoDetails.Keywords.ToArray();
		public string ShortDescription { get; } = player.VideoDetails.ShortDescription;
		public string Category { get; } = player.Microformat.PlayerMicroformatRenderer.Category;

		public bool IsLive { get; } = player.Microformat.PlayerMicroformatRenderer.LiveBroadcastDetails?.IsLiveNow ??
		                              player.VideoDetails.IsLiveContent;
		
		public bool IsFallback { get; } = isFallback;
		public bool AllowRatings { get; } = player.VideoDetails.AllowRatings;
		public bool IsFamilySafe { get; } = player.Microformat.PlayerMicroformatRenderer.IsFamilySafe;
		public Thumbnail[] Thumbnails { get; } = player.VideoDetails.Thumbnail.Thumbnails_.ToArray();

		public DateTimeOffset? PublishDate { get; } =
			DateTimeOffset.Parse(player.Microformat.PlayerMicroformatRenderer.PublishDate);

		public DateTimeOffset? UploadDate { get; } =
			DateTimeOffset.Parse(player.Microformat.PlayerMicroformatRenderer.UploadDate);

		public DateTimeOffset? LiveStreamStartDate { get; } =
			player.Microformat.PlayerMicroformatRenderer.LiveBroadcastDetails != null
				? DateTimeOffset.Parse(player.Microformat.PlayerMicroformatRenderer.LiveBroadcastDetails.StartTimestamp)
				: null;

		public TimeSpan? Length { get; } = TimeSpan.FromSeconds(player.VideoDetails.LengthSeconds);

		public Channel Author { get; } = new(
			parserLanguage,
			player.VideoDetails!.ChannelId,
			player.VideoDetails!.Author,
			player.Microformat.PlayerMicroformatRenderer.OwnerProfileUrl.Contains('@') 
				? "@" + player.Microformat.PlayerMicroformatRenderer.OwnerProfileUrl.Split('@')[1]
				: null,
			null,
			null,
			null);
	}

	public class VideoEndscreen(EndscreenRenderer endscreen)
	{
		public IEnumerable<EndscreenItem> Items { get; } =
			endscreen.Elements.Select(x => new EndscreenItem(x.EndscreenElementRenderer));
		public long StartMs { get; } = endscreen.StartMs;
	}

	public class VideoStoryboard(RendererWrapper wrapper, long videoDuration)
	{
		public int RecommendedLevel { get; } = wrapper.RendererCase switch
		{
			RendererWrapper.RendererOneofCase.PlayerStoryboardSpecRenderer => wrapper.PlayerStoryboardSpecRenderer.RecommendedLevel,
			RendererWrapper.RendererOneofCase.PlayerLiveStoryboardSpecRenderer => 0,
			_ => 0
		};
		public Dictionary<int, Uri> Levels { get; } = wrapper.RendererCase switch
		{
			RendererWrapper.RendererOneofCase.PlayerStoryboardSpecRenderer => Utils.ParseStoryboardSpec(wrapper
				.PlayerStoryboardSpecRenderer.Spec, videoDuration),
			RendererWrapper.RendererOneofCase.PlayerLiveStoryboardSpecRenderer => new Dictionary<int, Uri>
			{
				[0] = Utils.ParseLiveStoryboardSpec(wrapper.PlayerLiveStoryboardSpecRenderer.Spec)!
			},
			_ => new Dictionary<int, Uri>()
		};
	}

	public class VideoCaption(PlayerCaptionsTracklistRenderer.Types.Caption caption)
	{
		public string VssId { get; } = caption.VssId;
		public string LanguageCode { get; } = caption.Language;
		public string Label { get; } = Utils.ReadRuns(caption.Name);
		public Uri BaseUrl { get; } = new(caption.BaseUrl);
		public bool IsAutomaticCaption => VssId[0] == 'a';
	}
}