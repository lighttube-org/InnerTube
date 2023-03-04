using System.Net;

namespace InnerTube.Exceptions;

/// <summary>
/// Thrown when a request to the InnerTube API fails
/// </summary>
public class RequestException : InnerTubeException
{
	/// <summary>
	/// The raw JSON response
	/// </summary>
	public string JsonResponse { get; }

	internal RequestException(HttpStatusCode statusCode, string jsonResponse) : base(
		$"Request failed (status code: {(int)statusCode} ({statusCode}))")
	{
		JsonResponse = jsonResponse;
	}
}