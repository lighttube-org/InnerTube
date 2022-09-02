namespace InnerTube.Tests;

public class AuthenticationTests
{
	[Test]
	public async Task GetAuthorizedPlayerWithSapisid()
	{
		InnerTube tube = new(new InnerTubeConfiguration()
		{
			Authorization = InnerTubeAuthorization.SapisidAuthorization(
				Environment.GetEnvironmentVariable("INNERTUBE_SAPISID") ??
				throw new ArgumentNullException("INNERTUBE_SAPISID",
					"Please set the INNERTUBE_SAPISID environment variable."),
				Environment.GetEnvironmentVariable("INNERTUBE_PSID") ??
				throw new ArgumentNullException("INNERTUBE_PSID",
					"Please set the INNERTUBE_PSID environment variable.")
			)
		});

		InnerTubePlayer player = await tube.GetPlayerAsync("V6kJKxvbgZ0", true, false);
		Assert.Pass($"Received player with {player.Formats.Count()} muxed formats & {player.AdaptiveFormats.Count()} muxed formats");
	}

	[Test]
	public async Task GetAuthorizedPlayerWithRefreshToken()
	{
		InnerTube tube = new(new InnerTubeConfiguration()
		{
			Authorization = InnerTubeAuthorization.RefreshTokenAuthorization(
				Environment.GetEnvironmentVariable("INNERTUBE_REFRESH_TOKEN") ??
				throw new ArgumentNullException("INNERTUBE_REFRESH_TOKEN",
					"Please set the INNERTUBE_REFRESH_TOKEN environment variable.")
			)
		});

		InnerTubePlayer player = await tube.GetPlayerAsync("V6kJKxvbgZ0", true, false);
		Assert.Pass($"Received player with {player.Formats.Count()} muxed formats & {player.AdaptiveFormats.Count()} muxed formats");
	}
}