﻿using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class GridPlaylistRenderer : IRenderer
{
	public string Type => "gridPlaylistRenderer";

	public string Id { get; }
	public string Title { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }
	public int VideoCount { get; }

	public GridPlaylistRenderer(JToken renderer)
	{
		Id = renderer["playlistId"]!.ToString();
		Title = Utils.ReadText(renderer.GetFromJsonPath<JObject>("title")!);
		VideoCount = int.Parse(renderer["videoCountShortText"]!["simpleText"]!.ToString());
		Thumbnails =
			Utils.GetThumbnails(
				renderer.GetFromJsonPath<JArray>(
					"thumbnailRenderer.playlistVideoThumbnailRenderer.thumbnail.thumbnails")!);
		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("longBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = null,
			Subscribers = null,
			Badges = renderer.GetFromJsonPath<JArray>("ownerBadges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- VideoCount: {VideoCount}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}");

		return sb.ToString();
	}
}