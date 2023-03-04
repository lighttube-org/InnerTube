namespace InnerTube.Exceptions;

/// <summary>
/// Thrown when the received player response isn't playable
/// </summary>
public class PlayerException : InnerTubeException
{
	/// <summary>
	/// Code of the error, from YouTube
	/// </summary>
	public string Code { get; }
	/// <summary>
	/// Error message, from YouTube
	/// </summary>
	public string Error { get; }
	/// <summary>
	/// Error description, from YouTube
	/// </summary>
	public string Description { get; }

	internal PlayerException(string code, string error, string description) : base($"[{code}] {error}")
	{
		Code = code;
		Error = error;
		Description = description;
	}
}