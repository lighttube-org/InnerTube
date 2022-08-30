namespace InnerTube;

public class Thumbnail
{
	public int? Width { get; set; }
	public int? Height { get; set; }
	public Uri Url { get; set; }

	public override string ToString() => $"[{Width}x{Height}] {Url}";
}