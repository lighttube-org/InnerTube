using InnerTube.Protobuf;

namespace InnerTube.Models;

public class Badge(MetadataBadgeRenderer badge)
{
	public string Icon { get; } = badge.Icon?.IconType.ToString() ?? "0";
	public string? Label { get; } = badge.Label;
	public string? Tooltip { get; } = badge.Tooltip;

	public override string ToString() =>
		$"[Badge] {Label ?? "<no label>"}\nIcon: {Icon}\nTooltip: {Tooltip ?? "<no tooltip>"}";
}