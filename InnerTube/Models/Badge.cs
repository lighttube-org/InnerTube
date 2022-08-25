using Newtonsoft.Json.Linq;

namespace InnerTube;

public class Badge
{
	public string Style { get; }
	public string? Label { get; }
	public string? Tooltip { get; }
	public string? Icon { get; }

	public Badge(JToken metadataBadgeRenderer)
	{
		Style = metadataBadgeRenderer["style"]!.ToString();
		Label = metadataBadgeRenderer["label"]?.ToString();
		Tooltip = metadataBadgeRenderer["tooltip"]?.ToString();
		Icon = metadataBadgeRenderer["icon"]?["iconType"]?.ToString();
	}

	public override string ToString() => $"[{Style}]{(Icon is not null ? $" ({Icon})" : "")} {(Label is not null ? $", {Tooltip}" : "(no label)")}{(Tooltip is not null ? $", {Tooltip}" : "")}";
}