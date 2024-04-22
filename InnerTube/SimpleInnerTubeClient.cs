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
}