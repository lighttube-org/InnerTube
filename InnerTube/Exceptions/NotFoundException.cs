namespace InnerTube.Exceptions;

/// <summary>
/// Thrown when the specified content is not found
/// </summary>
public class NotFoundException : InnerTubeException
{
	internal NotFoundException(string message) : base(message)
	{
	}
}