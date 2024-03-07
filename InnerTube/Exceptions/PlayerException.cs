using InnerTube.Protobuf.Responses;

namespace InnerTube.Exceptions;

/// <summary>
/// Thrown when the received player response isn't playable
/// </summary>
public class PlayerException : InnerTubeException
{
	/// <summary>
	/// Code of the error, from YouTube
	/// </summary>
	public PlayabilityStatus.Types.Status Code { get; }
	/// <summary>
	/// Error message, from YouTube
	/// </summary>
	public string Reason { get; }
	/// <summary>
	/// Error description, from YouTube
	/// </summary>
	public string Subreason { get; }

	internal PlayerException(PlayabilityStatus.Types.Status code, string reason, string subreason) : base($"[{code}] {reason}\n{subreason}")
	{
		Code = code;
		Reason = reason;
		Subreason = subreason;
	}
}