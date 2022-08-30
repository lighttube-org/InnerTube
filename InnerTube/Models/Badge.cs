using Newtonsoft.Json.Linq;

namespace InnerTube;

public class Badge
{
	public string Style { get; }
	public string? Label { get; }
	public string? Tooltip { get; }
	public string? Icon { get; }

	public Badge(string style, string? icon, string? label, string? tooltip)
	{
		Style = style;
		Label = label;
		Tooltip = tooltip;
		Icon = icon;
	}

	public Badge(JToken metadataBadgeRenderer)
	{
		Style = metadataBadgeRenderer["style"]!.ToString();
		Label = metadataBadgeRenderer["label"]?.ToString();
		Tooltip = metadataBadgeRenderer["tooltip"]?.ToString();
		Icon = metadataBadgeRenderer["icon"]?["iconType"]?.ToString();
	}

	public override string ToString() =>
		$"[{Style}]{(Icon is not null ? $" ({Icon})" : "")} {(Label is not null ? $", {Label}" : "(no label)")}{(Tooltip is not null ? $", {Tooltip}" : "")}";

	public static Badge? FromAuthorCommentBadgeRenderer(JObject? authorCommentBadgeRenderer)
	{
		if (authorCommentBadgeRenderer is null) return null;
		return new Badge("COMMENT_AUTHOR_BADGE", authorCommentBadgeRenderer.GetFromJsonPath<string>("icon.iconType"),
			null, authorCommentBadgeRenderer.GetFromJsonPath<string>("iconTooltip"));
	}
}