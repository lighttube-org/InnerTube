using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubePlayer
{
	public VideoDetails Details { get; }
	public VideoEndscreen Endscreen { get; }
	public VideoStoryboard Storyboard { get; }
	public IEnumerable<VideoCaption> Captions { get; }
	public IEnumerable<Format> Formats { get; }
	public IEnumerable<Format> AdaptiveFormats { get; }
	public int ExpiresInSeconds { get; }
	public string? HlsManifestUrl { get; }
	public string? DashManifestUrl { get; }

	public InnerTubePlayer(JObject playerResponse, JObject metadataResponse)
	{
		Details = new VideoDetails
		{
			Id = metadataResponse.GetFromJsonPath<string>("videoDetails.videoId")!,
			Title = metadataResponse.GetFromJsonPath<string>("videoDetails.title")!,
			Author = new Channel
			{
				Id = metadataResponse.GetFromJsonPath<string>("videoDetails.channelId")!,
				Title = metadataResponse.GetFromJsonPath<string>("videoDetails.author")!
			},
			Keywords = metadataResponse.GetFromJsonPath<string[]>("videoDetails.keywords")!,
			ShortDescription = metadataResponse.GetFromJsonPath<string>("videoDetails.shortDescription")!,
			Length = TimeSpan.FromSeconds(
				long.Parse(metadataResponse.GetFromJsonPath<string>("videoDetails.lengthSeconds")!)),
			IsLive = metadataResponse.GetFromJsonPath<bool>("videoDetails.isLiveContent")!,
			AllowRatings = metadataResponse.GetFromJsonPath<bool>("videoDetails.allowRatings")
		};
		Endscreen = new VideoEndscreen
		{
			Items = metadataResponse.GetFromJsonPath<JArray>("endscreen.endscreenRenderer.elements")
				?.Select(x => new EndScreenItem(x["endscreenElementRenderer"]!)) ?? Array.Empty<EndScreenItem>(),
			StartMs = long.Parse(metadataResponse.GetFromJsonPath<string>("endscreen.endscreenRenderer.startMs") ?? "0")
		};
		Storyboard = new VideoStoryboard
		{
			RecommendedLevel =
				metadataResponse.GetFromJsonPath<int>("storyboards.playerStoryboardSpecRenderer.recommendedLevel"),
			Levels = Utils.GetLevelsFromStoryboardSpec(
				metadataResponse.GetFromJsonPath<string>("storyboards.playerStoryboardSpecRenderer.spec"),
				long.Parse(metadataResponse.GetFromJsonPath<string>("videoDetails.lengthSeconds")!))
		};
		Captions = metadataResponse.GetFromJsonPath<JArray>("captions.playerCaptionsTracklistRenderer.captionTracks")?
			.Select(x => new VideoCaption
			{
				LanguageCode = x["languageCode"]!.ToString(),
				Label = Utils.ReadText(x["name"]!.ToObject<JObject>()!),
				BaseUrl = new Uri(x["baseUrl"]!.ToString())
			}) ?? Array.Empty<VideoCaption>();
		Formats = playerResponse.GetFromJsonPath<JArray>("streamingData.formats")?.Select(x => new Format(x)) ??
		          Array.Empty<Format>();
		AdaptiveFormats =
			playerResponse.GetFromJsonPath<JArray>("streamingData.adaptiveFormats")?.Select(x => new Format(x)) ??
			Array.Empty<Format>();
		ExpiresInSeconds = playerResponse["streamingData"]?["expiresInSeconds"]?.ToObject<int>() ?? 0;
		HlsManifestUrl = playerResponse["streamingData"]?["hlsManifestUrl"]?.ToString();
		DashManifestUrl = playerResponse["streamingData"]?["dashManifestUrl"]?.ToString();
	}

	public class VideoDetails
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public Channel Author { get; set; }
		public string[] Keywords { get; set; }
		public string ShortDescription { get; set; }
		public TimeSpan Length { get; set; }
		public bool IsLive { get; set; }
		public bool AllowRatings { get; set; }
	}

	public class VideoCaption
	{
		public string LanguageCode { get; set; }
		public string Label { get; set; }
		public Uri BaseUrl { get; set; }
	}

	public class VideoEndscreen
	{
		public IEnumerable<EndScreenItem> Items { get; set; }
		public long StartMs { get; set; }
	}

	public class VideoStoryboard
	{
		public int RecommendedLevel { get; set; }
		public Dictionary<int, Uri> Levels { get; set; }
	}
}