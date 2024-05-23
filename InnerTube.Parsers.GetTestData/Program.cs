using System.Text.Json;
using Google.Protobuf.Collections;
using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;

string[] languages =
[
	"af", "az", "id", "ms", "bs", "ca", "cs", "da", "de", "et", "en-IN", "en-GB", "en", "es", "es-419", "es-US",
	"eu", "fil", "fr", "fr-CA", "gl", "hr", "zu", "is", "it", "sw", "lv", "lt", "hu", "nl", "no", "uz", "pl",
	"pt-PT", "pt", "ro", "sq", "sk", "sl", "sr-Latn", "fi", "sv", "vi", "tr", "be", "bg", "ky", "kk", "mk", "mn",
	"ru", "sr", "uk", "el", "hy", "iw", "ur", "ar", "fa", "ne", "mr", "hi", "as", "bn", "pa", "gu", "or", "ta",
	"te", "kn", "ml", "si", "th", "lo", "my", "ka", "am", "km", "zh-CN", "zh-TW", "zh-HK", "ja", "ko"
];

string[] channels =
[
	"UCX6OQ3DkcsbYNE6H8uQQuVA", // million subscribers
	"UCbKWv2x9t6u8yZoB3KcPtnw", // million subscribers, 1 decimal
	"UCS0N5baNlQWJCUrhCEo8WlA", // million subscribers, 2 decimal
	"UCB5zZAm0b5-EqWkOEwHBE_A", // thousand subscribers
	"UCjdHbo8_vh3rxQ-875XGkvw", // thousand subscribers, 1 decimal
	// TODO: "", // thousand subscribers, 2 decimal
	"UCRS3ZUNqkEyTd9XZEphFRMA", // hundred subscribers
];

string[] videos =
[
	"Atvsg_zogxo", // Premiered
	"jfKfPfyJRdk", // Streaming
	"dv_YFDzCw2s", // Streamed
	"7DKv5H5Frt0", // Published
	"GfDXqY-V0EY", // Premiere. Update before every run
	"Hr2Lm6oEo3c" // Scheduled livestream. Update before every run
];

string[] playlists =
[
	"VLPLiDvcIUGEFPv2K8h3SRrpc7FN7Ks0Z_A7",
	"VLPLWA4fx92eWNstZbKK52BK9Ox-I4KvxdkF"
];

// todo: remember to borrow dates from https://github.com/TeamNewPipe/NewPipeExtractor/blob/dev/timeago-parser/raw/overview.json

InnerTube.InnerTube client = new();

Dictionary<string, Dictionary<string, List<string>>> finalResult = [];

for (int i = 0; i < languages.Length; i++)
{
	string hl = languages[i];
	Console.Write($"[{i + 1,2}/{languages.Length}] Getting data for {$"'{hl}'",-9}");

	List<string> subscriberCounts = [];
	List<string> videoDates = [];
	List<string> viewCounts = [];
	List<string> likeCounts = [];
	List<string> lastUpdatedDates = [];

	Task[] channelTasks = channels.Select(channelId => GetSubscriptionCount(hl, channelId)).Cast<Task>().ToArray();
	await Task.WhenAll(channelTasks);
	subscriberCounts.AddRange(from Task<string> res in channelTasks select res.Result);

	Task[] videoTasks = videos.Select(videoId => GetVideoStrings(hl, videoId)).Cast<Task>().ToArray();
	await Task.WhenAll(videoTasks);
	videoDates.AddRange(from Task<(string, string, string)> res in videoTasks select res.Result.Item1);
	viewCounts.AddRange(from Task<(string, string, string)> res in videoTasks select res.Result.Item2);
	likeCounts.AddRange(from Task<(string, string, string)> res in videoTasks select res.Result.Item3);

	Task[] playlistTasks = playlists.Select(playlistId => GetLastUpdated(hl, playlistId)).Cast<Task>().ToArray();
	await Task.WhenAll(playlistTasks);
	lastUpdatedDates.AddRange(from Task<string> res in playlistTasks select res.Result);

	Console.Write("\n");
	Dictionary<string, List<string>> languageResult = new()
	{
		["subscriberCounts"] = subscriberCounts,
		["videoDates"] = videoDates,
		["viewCounts"] = viewCounts,
		["likeCounts"] = likeCounts,
		["lastUpdatedDates"] = lastUpdatedDates,
	};

	finalResult.Add(hl, languageResult);
}

string json = JsonSerializer.Serialize(finalResult);
File.WriteAllText($"out.{DateTimeOffset.Now:s}.json", json);

return;

async Task<string> GetSubscriptionCount(string hl, string channelId)
{
	BrowseResponse channel = await client.BrowseAsync(channelId, language: hl);
	string subscriberCountText = (channel.Header.RendererCase switch
	{
		RendererWrapper.RendererOneofCase.C4TabbedHeaderRenderer => new ChannelHeader(channel.Header
			.C4TabbedHeaderRenderer),
		RendererWrapper.RendererOneofCase.PageHeaderRenderer => new ChannelHeader(
			channel.Header.PageHeaderRenderer,
			channel.Metadata.ChannelMetadataRenderer.ExternalId),
		_ => null
	})!.SubscriberCountText;
	Console.Write(".");
	return subscriberCountText;
}

async Task<(string, string, string)> GetVideoStrings(string hl, string videoId)
{
	NextResponse next = await client.GetNextAsync(videoId, language: hl);
	RepeatedField<RendererWrapper> firstColumnResults =
		next.Contents.TwoColumnWatchNextResults.Results.ResultsContainer.Results;
	VideoPrimaryInfoRenderer videoPrimaryInfoRenderer = firstColumnResults.First(x =>
		x.RendererCase == RendererWrapper.RendererOneofCase.VideoPrimaryInfoRenderer).VideoPrimaryInfoRenderer;
	string dateText = Utils.ReadRuns(videoPrimaryInfoRenderer.DateText);
	string viewCountText = Utils.ReadRuns(videoPrimaryInfoRenderer.ViewCount?.VideoViewCountRenderer.ViewCount);
	string likeCountText = videoPrimaryInfoRenderer.VideoActions.MenuRenderer.TopLevelButtons
		.First(x => x.RendererCase == RendererWrapper.RendererOneofCase.SegmentedLikeDislikeButtonViewModel)
		.SegmentedLikeDislikeButtonViewModel.LikeButtonViewModel.LikeButtonViewModel.ToggleButtonViewModel
		.ToggleButtonViewModel.DefaultButtonViewModel.ButtonViewModel2.Title;
	Console.Write(",");
	return (dateText, viewCountText, likeCountText);
}

async Task<string> GetLastUpdated(string hl, string playlistId)
{
	BrowseResponse browse = await client.BrowseAsync(playlistId, language: hl);
	string lastUpdated = Utils.ReadRuns(browse.Header.PlaylistHeaderRenderer.Byline.PlaylistBylineRenderer.Text.Last());
	Console.Write(";");
	return lastUpdated;
}