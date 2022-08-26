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
			Utils.ParseRenderers(response.GetFromJsonPath<JArray>("onResponseReceivedCommands[0].appendContinuationItemsAction.continuationItems[0].itemSectionRenderer.contents")!),
			response.GetFromJsonPath<string>("onResponseReceivedCommands[0].appendContinuationItemsAction.continuationItems[1].continuationItemRenderer.continuationEndpoint.continuationCommand.token")
		);
	}

	public static InnerTubeContinuationResponse GetFromComments(JObject nextResponse)
	{
		JToken response = nextResponse["onResponseReceivedEndpoints"]!.ToObject<JArray>()!.Last!;
		JArray comments =
			(response["reloadContinuationItemsCommand"] ?? response["appendContinuationItemsAction"])![
				"continuationItems"]!.ToObject<JArray>()!;
		return new InnerTubeContinuationResponse(
			Utils.ParseRenderers(new JArray(comments.Where(x => x["commentThreadRenderer"] != null))),
			comments.Last!.GetFromJsonPath<string>("continuationItemRenderer.continuationEndpoint.continuationCommand.token")
		);	}
}
