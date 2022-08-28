namespace InnerTube.Exceptions;

public class NotFoundException : InnerTubeException
{
	internal NotFoundException(string message) : base(message)
	{ }
}