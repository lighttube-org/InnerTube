namespace InnerTube;

public class Channel
{
	public string Id { get; set; }
	public string Title { get; set; }
	public Uri? Avatar { get; set; }
	public string? Subscribers { get; set; }

	public override string ToString()
	{
		string res = $"[{Id}] {Title}";
		if (Avatar is not null)
			res += " | Avatar: " + Avatar;
		if (Subscribers is not null)
			res += " | Subscribers: " + Subscribers;
		return res;
	}
}