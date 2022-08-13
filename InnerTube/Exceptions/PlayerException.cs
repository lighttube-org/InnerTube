namespace InnerTube.Exceptions;

public class PlayerException : InnerTubeException
{
	public string Code { get; }
	public string Error { get; }
	public string Description { get; }
	public PlayerException(string code, string error, string description) : base($"[{code}] {error}")
	{
		Code = code;
		Error = error;
		Description = description;
	}
}