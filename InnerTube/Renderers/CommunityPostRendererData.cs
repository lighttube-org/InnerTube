using System.Text;
using Google.Protobuf.WellKnownTypes;
using InnerTube.Models;
using InnerTube.Protobuf;

namespace InnerTube.Renderers;

public class CommunityPostRendererData : IRendererData
{
	public string PostId { get; set; }
	public Channel Author { get; set; }
	public string Content { get; set; }
	public string LikeCountText { get; set; }
	public string CommentsCountText { get; set; }
	public RendererContainer? Attachment { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{PostId}]");
		sb.AppendLine("Author: " + (Author?.ToString() ?? "<null>"));
		sb.AppendLine("LikeCountText: " + LikeCountText);
		sb.AppendLine("CommentsCountText: " + CommentsCountText);
		sb.AppendLine("Content: " + Content);
		sb.AppendLine("Attachment:");
		if (Attachment != null)
			sb.AppendLine($"-> [{Attachment.Type} ({Attachment.OriginalType})] [{Attachment.Data.GetType().Name}]\n\t" +
			              string.Join("\n\t", Attachment.Data.ToString()!.Split("\n")));
		else
			sb.AppendLine("-> [null]");
		return sb.ToString();
	}
}