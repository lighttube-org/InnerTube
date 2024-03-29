﻿using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

// adding this and PromotedVideoRenderer kinda hurt me ngl
public class SearchPyvRenderer : IRenderer
{
	public string Type => "searchPyvRenderer";

	public IEnumerable<IRenderer> Ads { get; }

	public SearchPyvRenderer(JToken renderer)
	{
		Ads = RendererManager.ParseRenderers(renderer["ads"]!.ToObject<JArray>()!);
	}

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{Type}] ADS");
		foreach (IRenderer renderer in Ads)
			sb.AppendLine("->\t" + string.Join("\n\t",
				(renderer.ToString() ?? "UNKNOWN RENDERER " + renderer.Type).Split("\n")));

		return sb.ToString();
	}
}