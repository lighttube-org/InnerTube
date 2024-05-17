namespace InnerTube.Parsers;

public interface IValueParser
{
	public string ParseRelativeDate(string date);
	public DateTimeOffset ParseFullDate(string date);
	public VideoUploadType ParseVideoUploadType(string type);
	public long ParseSubscriberCount(string subscriberCountText);
	public long ParseLikeCount(string likeCountText);
	public long ParseViewCount(string viewCountText);
	public DateTimeOffset ParseLastUpdated(string lastUpdatedText);
}