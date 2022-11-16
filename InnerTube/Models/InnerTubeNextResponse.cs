using InnerTube.Exceptions;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeNextResponse
{
	public string Id { get; }
	public string Title { get; }
	public string Description { get; }
	public string DateText { get; }
	public string ViewCount { get; }
	public string LikeCount { get; }
	public Channel Channel { get; }
	public string? CommentsContinuation { get; }
	public string? CommentCount { get; }
	public IEnumerable<IRenderer> Recommended { get; }
	public InnerTubePlaylistInfo? Playlist { get; }

	public InnerTubeNextResponse(JObject playerResponse)
	{
		JToken resultsArray =
			playerResponse.GetFromJsonPath<JToken>("contents.twoColumnWatchNextResults.results.results")!;
		if (resultsArray is null || !resultsArray.Any(x => x.Path.EndsWith("contents")))
			throw new InnerTubeException("Cannot get information about this video");

		int index = 0;

		if (resultsArray.GetFromJsonPath<JToken>("contents[0].itemSectionRenderer") != null)
			index = 1;

		JToken? errorObject = resultsArray.GetFromJsonPath<JToken>(
			"contents[0].itemSectionRenderer.contents[0].backgroundPromoRenderer");
		if (errorObject is not null)
			throw new NotFoundException(Utils.ReadText(errorObject["title"]!.ToObject<JObject>()!));

		Id = playerResponse.GetFromJsonPath<string>("currentVideoEndpoint.watchEndpoint.videoId")!;
		Title = Utils.ReadText(resultsArray.GetFromJsonPath<JObject>(
			$"contents[{index}].videoPrimaryInfoRenderer.title")!);
		JObject? descriptionArray = resultsArray.GetFromJsonPath<JObject>(
			$"contents[{index + 1}].videoSecondaryInfoRenderer.description");
		Description = descriptionArray != null ? Utils.ReadText(descriptionArray) : "";
		DateText = resultsArray.GetFromJsonPath<string>(
				$"contents[{index}].videoPrimaryInfoRenderer.dateText.simpleText")
			!;
		ViewCount = resultsArray.GetFromJsonPath<string>(
				$"contents[{index}].videoPrimaryInfoRenderer.viewCount.videoViewCountRenderer.viewCount.simpleText")
			!;
		LikeCount = resultsArray.GetFromJsonPath<string>(
				$"contents[{index}].videoPrimaryInfoRenderer.videoActions.menuRenderer.topLevelButtons[0].toggleButtonRenderer.defaultText.simpleText")
			!;
		JObject channelObject = resultsArray.GetFromJsonPath<JObject>(
				$"contents[{index + 1}].videoSecondaryInfoRenderer.owner.videoOwnerRenderer")
			!;
		Channel = new Channel
		{
			Id = channelObject.GetFromJsonPath<string>("navigationEndpoint.browseEndpoint.browseId")!,
			Title = channelObject.GetFromJsonPath<string>("title.runs[0].text")!,
			Avatar = Utils.GetThumbnails(channelObject.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray())
				.LastOrDefault()?.Url,
			Subscribers = channelObject.GetFromJsonPath<string>("subscriberCountText.simpleText")!,
			Badges = channelObject.GetFromJsonPath<JArray>("badges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};

		JObject? commentObject = resultsArray.GetFromJsonPath<JObject>(
			$"contents[{index + 2}].itemSectionRenderer.contents[0].commentsEntryPointHeaderRenderer");
		CommentCount = commentObject != null
			? commentObject["commentCount"]?["simpleText"]?.ToString()
			: null;

		CommentsContinuation = resultsArray.GetFromJsonPath<string>(
			$"contents[{index + 3}].itemSectionRenderer.contents[0].continuationItemRenderer.continuationEndpoint.continuationCommand.token");

		JArray? recommendedList =
			playerResponse.GetFromJsonPath<JArray>(
				"contents.twoColumnWatchNextResults.secondaryResults.secondaryResults.results");
		Recommended = recommendedList != null
			? RendererManager.ParseRenderers(recommendedList)
			: Array.Empty<IRenderer>();

		JObject? playlistObject =
			playerResponse.GetFromJsonPath<JObject>("contents.twoColumnWatchNextResults.playlist.playlist");
		if (playlistObject != null)
			Playlist = new InnerTubePlaylistInfo(playlistObject);
	}
}