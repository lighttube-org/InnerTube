using InnerTube.Exceptions;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeContinuationResponse
{
	public IEnumerable<IRenderer> Contents { get; }
	public string? Continuation { get; }

	internal InnerTubeContinuationResponse(IEnumerable<IRenderer> contents, string? continuation)
	{
		Contents = contents;
		Continuation = continuation;
	}

	public static InnerTubeContinuationResponse GetFromSearchResponse(JObject response)
	{
		return new InnerTubeContinuationResponse(
			RendererManager.ParseRenderers(response.GetFromJsonPath<JArray>(
					"onResponseReceivedCommands[0].appendContinuationItemsAction.continuationItems[0].itemSectionRenderer.contents")
				!),
			response.GetFromJsonPath<string>(
				"onResponseReceivedCommands[0].appendContinuationItemsAction.continuationItems[1].continuationItemRenderer.continuationEndpoint.continuationCommand.token")
		);
	}

	public static InnerTubeContinuationResponse GetFromComments(JObject nextResponse)
	{
		// error handling
		// cant believe that they send a normal response instead of a continuation response when the continuation key is invalid
		// find me the person who works at youtube that made this function like this, we will have a friendly chat
		JToken? errorObject = nextResponse.GetFromJsonPath<JToken>(
			"contents.twoColumnWatchNextResults.results.results.contents[0].itemSectionRenderer.contents[0].backgroundPromoRenderer");
		if (errorObject is not null)
			throw new NotFoundException(Utils.ReadText(errorObject["title"]!.ToObject<JObject>()!));

		if (!nextResponse.ContainsKey("onResponseReceivedEndpoints"))
			throw new ArgumentException("Invalid continuation key");

		JToken response = nextResponse["onResponseReceivedEndpoints"]!.ToObject<JArray>()!.Last!;
		JArray comments =
			(response["reloadContinuationItemsCommand"] ?? response["appendContinuationItemsAction"])![
				"continuationItems"]!.ToObject<JArray>()!;
		return new InnerTubeContinuationResponse(
			RendererManager.ParseRenderers(new JArray(comments.Where(x => x["commentThreadRenderer"] != null))),
			comments.Last!.GetFromJsonPath<string>(
				"continuationItemRenderer.continuationEndpoint.continuationCommand.token")
		);
	}

	public static InnerTubeContinuationResponse GetFromBrowse(JObject browseResponse)
	{
		// i think this is being a/b tested? idk the payload i
		// receive in the website and in the code are different
		IEnumerable<IRenderer> contents;
		try
		{
			contents = RendererManager.ParseRenderers(browseResponse.GetFromJsonPath<JArray>(
				"continuationContents.richGridContinuation.contents")!).ToArray();
		}
		catch
		{
			contents = RendererManager.ParseRenderers(browseResponse.GetFromJsonPath<JArray>(
				"onResponseReceivedActions[0].appendContinuationItemsAction.continuationItems")!).ToArray();
		}
		return new InnerTubeContinuationResponse(contents.Where(x => x is not ContinuationItemRenderer),
			((ContinuationItemRenderer?)contents.FirstOrDefault(x => x is ContinuationItemRenderer))?.Token);
	}
}