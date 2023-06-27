using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class VerticalProductCardRenderer : IRenderer
{
	public string Type => "verticalProductCardRenderer";

	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public string Price { get; }
	public string Merchant { get; }
	public string AdditionalFeesText { get; }
	public string Url { get; }

	public VerticalProductCardRenderer(JObject renderer)
	{
		Title = renderer["title"]!.ToObject<string>()!;
		Price = renderer["price"]!.ToObject<string>()!;
		Merchant = renderer["merchantName"]!.ToObject<string>()!;
		AdditionalFeesText = renderer["additionalFeesText"]!.ToObject<string>()!;
		Url = Utils.UnwrapRedirectUrl(renderer.GetFromJsonPath<string>("navigationEndpoint.urlEndpoint.url")!);
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails")!);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Price: {Price}")
			.AppendLine($"- AdditionalFeesText: {AdditionalFeesText}")
			.AppendLine($"- Merchant: {Merchant}")
			.AppendLine($"- Url: {Url}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}");

		return sb.ToString();
	}
}