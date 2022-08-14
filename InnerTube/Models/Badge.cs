using Newtonsoft.Json.Linq;

namespace InnerTube;

public class Badge
{
	public string Style { get; }
	public string Label { get; }

	public Badge(JToken metadataBadgeRenderer)
	{
		Style = metadataBadgeRenderer["style"]!.ToString()!;
		Label = metadataBadgeRenderer["label"]!.ToString()!;
	}

	public override string ToString() => $"[{Style}] {Label}";
}