namespace InnerTube;

public class Channel
{
	public string Id { get; set; }
	public string Title { get; set; }
	public Uri? Avatar { get; set; }
	public string? Subscribers { get; set; }
	public IEnumerable<Badge> Badges { get; set; } = Array.Empty<Badge>();

	public override string ToString()
	{
		string res = $"[{Id}] {Title}";
		if (Avatar is not null)
			res += " | Avatar: " + Avatar;
		if (Subscribers is not null)
			res += " | Subscribers: " + Subscribers;
		if (Badges.Any())
			res += " | Badges: " + string.Join(", ", Badges.Select(x => x.ToString()));
		return res;
	}
}