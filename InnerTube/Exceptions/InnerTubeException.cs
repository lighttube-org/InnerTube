namespace InnerTube.Exceptions;

/// <summary>
/// An InnerTube-related exception.
/// </summary>
public class InnerTubeException : Exception
{
	internal InnerTubeException(string message) : base(message)
	{
	}
}