using InnerTube.Protobuf;

namespace InnerTube.Models;

public class Badge(MetadataBadgeRenderer badge)
{
	public string Icon { get; set; } = badge.Icon.IconType.ToString();
	public string? Label { get; set; } = badge.Label;
	public string? Tooltip { get; set; } = badge.Tooltip;

	public override string ToString() =>
		$"[Badge] {Label ?? "<no label>"}\nIcon: {Icon}\nTooltip: {Tooltip ?? "<no tooltip>"}";
}