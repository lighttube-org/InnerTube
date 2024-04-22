namespace InnerTube;

public readonly struct PlaylistContinuationInfo(string internalPlaylistId, string playlistId, int continueFrom)
{
	public string InternalPlaylistId { get; } = internalPlaylistId;
	public string PlaylistId { get; } = playlistId;
	public int ContinueFrom { get; } = continueFrom;

	public override string ToString() => $"{InternalPlaylistId} | {PlaylistId} | {ContinueFrom}";
}