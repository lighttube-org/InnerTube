using InnerTube.Protobuf;

namespace InnerTube.Models;

public class ChannelTab
{
	public ChannelTabs Tab { get; }
	public string Title { get; }
	public string Params { get; }
	public bool Selected { get; }

	public ChannelTab(TabRenderer tab)
	{
		Tab = Utils.GetTabFromChannelParams(tab.Endpoint.BrowseEndpoint.Params);
		Title = tab.Title;
		Params = Utils.GetNameFromChannelParams(tab.Endpoint.BrowseEndpoint.Params);
		Selected = tab.Selected;
	}

	public ChannelTab(ExpandableTabRenderer tab)
	{
		Tab = Utils.GetTabFromChannelParams(tab.Endpoint.BrowseEndpoint.Params);
		Title = tab.Title;
		Params = Utils.GetNameFromChannelParams(tab.Endpoint.BrowseEndpoint.Params);
		Selected = tab.Selected;
	}

	public ChannelTab(string rendererName)
	{
		Tab = ChannelTabs.Unknown;
		Params = "";
		Title = $"UnexpectedRenderer[{rendererName}]";
		Selected = false;
	}
}