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
	public CommentThreadRenderer? TeaserComment { get; }
	public string? CommentsContinuation { get; }
	public string? CommentCount { get; }
	public IEnumerable<IRenderer> Recommended { get; }

	public InnerTubeNextResponse(JObject playerResponse)
	{
		Id = playerResponse.GetFromJsonPath<string>("currentVideoEndpoint.watchEndpoint.videoId")!;
		Title = Utils.ReadRuns(playerResponse.GetFromJsonPath<JArray>(
			"contents.twoColumnWatchNextResults.results.results.contents[0].videoPrimaryInfoRenderer.title.runs")!);
		JArray? descriptionArray = playerResponse.GetFromJsonPath<JArray>(
				"contents.twoColumnWatchNextResults.results.results.contents[1].videoSecondaryInfoRenderer.description.runs");
		Description = descriptionArray != null ? Utils.ReadRuns(descriptionArray) : "";
		DateText = playerResponse.GetFromJsonPath<string>(
				"contents.twoColumnWatchNextResults.results.results.contents[0].videoPrimaryInfoRenderer.dateText.simpleText")
			!;
		ViewCount = playerResponse.GetFromJsonPath<string>(
				"contents.twoColumnWatchNextResults.results.results.contents[0].videoPrimaryInfoRenderer.viewCount.videoViewCountRenderer.viewCount.simpleText")
			!;
		LikeCount = playerResponse.GetFromJsonPath<string>(
				"contents.twoColumnWatchNextResults.results.results.contents[0].videoPrimaryInfoRenderer.videoActions.menuRenderer.topLevelButtons[0].toggleButtonRenderer.defaultText.simpleText")
			!;
		JObject channelObject = playerResponse.GetFromJsonPath<JObject>(
				"contents.twoColumnWatchNextResults.results.results.contents[1].videoSecondaryInfoRenderer.owner.videoOwnerRenderer")
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

		JObject? commentObject = playerResponse.GetFromJsonPath<JObject>(
			"contents.twoColumnWatchNextResults.results.results.contents[2].itemSectionRenderer.contents[0].commentsEntryPointHeaderRenderer");
		TeaserComment = commentObject != null
			? new CommentThreadRenderer(
				"",
				commentObject.GetFromJsonPath<string>("teaserContent.simpleText")!,
				new Channel
				{
					Id = null,
					Title =
						commentObject.GetFromJsonPath<string>("teaserAvatar.accessibility.accessibilityData.label")!,
					Avatar = Utils.GetThumbnails(commentObject.GetFromJsonPath<JArray>("teaserAvatar.thumbnails")!)
						.Last()
						.Url,
					Subscribers = null,
					Badges = Array.Empty<Badge>()
				},
				null,
				false,
				null,
				null)
			: null;
		CommentCount = commentObject != null
			? commentObject["commentCount"]!["simpleText"]!.ToString()
			: null;

		CommentsContinuation = playerResponse.GetFromJsonPath<string>(
			"contents.twoColumnWatchNextResults.results.results.contents[3].itemSectionRenderer.contents[0].continuationItemRenderer.continuationEndpoint.continuationCommand.token");

		JArray? recommendedList = playerResponse.GetFromJsonPath<JArray>("contents.twoColumnWatchNextResults.secondaryResults.secondaryResults.results");
		Recommended = recommendedList != null
			? Utils.ParseRenderers(recommendedList)
			: Array.Empty<IRenderer>();
	}
}