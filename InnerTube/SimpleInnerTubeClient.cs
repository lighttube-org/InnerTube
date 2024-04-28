using InnerTube.Models;
using InnerTube.Protobuf.Responses;

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
}