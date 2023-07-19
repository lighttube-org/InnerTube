namespace InnerTube;

public class PlaylistContinuationInfo
{
	public string InternalPlaylistId { get; set; }
	public string PlaylistId { get; set; }
	public int ContinueFrom { get; set; }

	public override string ToString() => $"{InternalPlaylistId} | {PlaylistId} | {ContinueFrom}";
}