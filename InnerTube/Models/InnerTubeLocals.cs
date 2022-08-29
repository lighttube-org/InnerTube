using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeLocals
{
	public Dictionary<string, string> Languages { get; set; }
	public Dictionary<string, string> Regions { get; set; }

	public InnerTubeLocals(JObject localsResponse)
	{
		JArray languagesArray = localsResponse.GetFromJsonPath<JArray>(
			"actions[0].openPopupAction.popup.multiPageMenuRenderer.sections[1].multiPageMenuSectionRenderer.items[1].compactLinkRenderer.serviceEndpoint.signalServiceEndpoint.actions[0].getMultiPageMenuAction.menu.multiPageMenuRenderer.sections[0].multiPageMenuSectionRenderer.items")!;
		JArray regionsArray = localsResponse.GetFromJsonPath<JArray>(
			"actions[0].openPopupAction.popup.multiPageMenuRenderer.sections[1].multiPageMenuSectionRenderer.items[3].compactLinkRenderer.serviceEndpoint.signalServiceEndpoint.actions[0].getMultiPageMenuAction.menu.multiPageMenuRenderer.sections[0].multiPageMenuSectionRenderer.items")!;

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
		;
	}
}