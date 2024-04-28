using Google.Protobuf.Collections;
using InnerTube.Models;
using InnerTube.Protobuf;
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
}