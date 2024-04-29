using Google.Protobuf;
using Google.Protobuf.Collections;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;

namespace InnerTube;

public class SimpleInnerTubeClient(InnerTubeConfiguration? config = null)
{
	public InnerTube InnerTube = new(config);

	public async Task<InnerTubePlayer> GetVideoPlayerAsync(string videoId, bool contentCheckOk, string language, string region)
	{
		PlayerResponse player = await InnerTube.GetPlayerAsync(videoId, contentCheckOk, language, region);
		return new InnerTubePlayer(player);
	}

	public async Task<InnerTubeVideo> GetVideoDetailsAsync(string videoId, bool contentCheckOk, string? playlistId,
		int? playlistIndex, string? playlistParams, string language, string region)
	{
		NextResponse next = await InnerTube.GetNextAsync(videoId, contentCheckOk, true, playlistId, playlistIndex,
			playlistParams, language, region);
		return new InnerTubeVideo(next);
	}

	public async Task<ContinuationResponse> ContinueVideoRecommendationsAsync(string continuationKey, string language,
		string region)
	{
		NextResponse next = await InnerTube.ContinueNextAsync(continuationKey, language, region);
		RepeatedField<RendererWrapper> allItems = next.OnResponseReceivedEndpoints[0].AppendContinuationItemsAction
			.ContinuationItems;
		IEnumerable<RendererWrapper> items = allItems.Where(x =>
			x.RendererCase != RendererWrapper.RendererOneofCase.ContinuationItemRenderer);
		ContinuationItemRenderer? continuation = allItems.LastOrDefault(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)?.ContinuationItemRenderer;
		return new ContinuationResponse
		{
			ContinuationToken = continuation?.ContinuationEndpoint.ContinuationCommand.Token,
			Results = Utils.ConvertRenderers(items)
		};
	}

	// doesn't take language/region because comments don't have anything to localize server side
	public async Task<ContinuationResponse> GetVideoCommentsAsync(string videoId,
		CommentsContext.Types.SortOrder sortOrder) =>
		await ContinueVideoCommentsAsync(Utils.PackCommentsContinuation(videoId, sortOrder));

	public async Task<ContinuationResponse> ContinueVideoCommentsAsync(string continuationToken)
	{
		NextResponse next = await InnerTube.ContinueNextAsync(continuationToken);
		RepeatedField<RendererWrapper>? continuationItems =
			next.OnResponseReceivedEndpoints.LastOrDefault()?.ReloadContinuationItemsCommand?.ContinuationItems ?? 
			next.OnResponseReceivedEndpoints.LastOrDefault()?.AppendContinuationItemsAction?.ContinuationItems;
		Dictionary<string, Payload> mutations =
			next.FrameworkUpdates.EntityBatchUpdate.Mutations.ToDictionary(x => x.EntityKey, x => x.Payload);
		if (continuationItems == null) return new ContinuationResponse
		{
			ContinuationToken = null,
			Results = []
		};
		return new ContinuationResponse
		{
			ContinuationToken = continuationItems
				.LastOrDefault(x => x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)
				?.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token,
			Results = continuationItems
				.Where(x => x.RendererCase == RendererWrapper.RendererOneofCase.CommentThreadRenderer)
				.Select(x => new RendererContainer
				{
					Type = "comment",
					OriginalType = "commentThreadRenderer",
					Data = new CommentRendererData(
						x.CommentThreadRenderer,
						mutations[x.CommentThreadRenderer.CommentViewModel.CommentViewModel.CommentKey]
							.CommentEntityPayload,
						mutations[x.CommentThreadRenderer.CommentViewModel.CommentViewModel.ToolbarStateKey]
							.EngagementToolbarStateEntityPayload)
				}).ToArray()
		};
	}
}