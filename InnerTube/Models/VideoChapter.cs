using InnerTube.Protobuf;

namespace InnerTube.Models;

public class VideoChapter
{
	public float StartSeconds { get; set; }
	public string Title { get; set; }
	public Thumbnail[] Thumbnails { get; set; }
}