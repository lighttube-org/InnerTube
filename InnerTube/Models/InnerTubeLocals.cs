using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeLocals
{
	public Dictionary<string, string> Languages { get; set; }
	public Dictionary<string, string> Regions { get; set; }

	public InnerTubeLocals(JObject localsResponse)
	{
		JArray sections = localsResponse.GetFromJsonPath<JArray>(
			"actions[0].openPopupAction.popup.multiPageMenuRenderer.sections[0].multiPageMenuSectionRenderer.items")!;

		JArray languagesArray = sections
			                        .First(x => x["compactLinkRenderer"]?["icon"]?["iconType"]?.ToObject<string>() == "TRANSLATE")
			                        .GetFromJsonPath<JArray>(
				                        "compactLinkRenderer.serviceEndpoint.signalServiceEndpoint.actions[0].getMultiPageMenuAction.menu.multiPageMenuRenderer.sections[0].multiPageMenuSectionRenderer.items")
		                        ?? new JArray();
		JArray regionsArray = sections
			                      .First(x => x["compactLinkRenderer"]?["icon"]?["iconType"]?.ToObject<string>() == "LANGUAGE")
			                      .GetFromJsonPath<JArray>(
				                      "compactLinkRenderer.serviceEndpoint.signalServiceEndpoint.actions[0].getMultiPageMenuAction.menu.multiPageMenuRenderer.sections[0].multiPageMenuSectionRenderer.items")
		                      ?? new JArray();

		Languages = languagesArray.ToDictionary(
			x => x.GetFromJsonPath<string>(
				"compactLinkRenderer.serviceEndpoint.signalServiceEndpoint.actions[0].selectLanguageCommand.hl")!,
			x => x.GetFromJsonPath<string>("compactLinkRenderer.title.simpleText")!
		);
		Regions = regionsArray.ToDictionary(
			x => x.GetFromJsonPath<string>(
				"compactLinkRenderer.serviceEndpoint.signalServiceEndpoint.actions[0].selectCountryCommand.gl")!,
			x => x.GetFromJsonPath<string>("compactLinkRenderer.title.simpleText")!
		);
	}
}