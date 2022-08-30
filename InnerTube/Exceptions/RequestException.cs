using System.Net;

namespace InnerTube.Exceptions;

public class RequestException : InnerTubeException
{
	public string JsonResponse { get; }

	internal RequestException(HttpStatusCode statusCode, string jsonResponse) : base(
		$"Request failed (status code: {(int)statusCode} ({statusCode}))")
	{
		JsonResponse = jsonResponse;
	}
}