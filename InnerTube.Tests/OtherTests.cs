using System.Text;

namespace InnerTube.Tests;

public class OtherTests
{
	private InnerTube _innerTube;

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[Test]
	public async Task GetLocals()
	{
		StringBuilder sb = new();
		InnerTubeLocals locals = await _innerTube.GetLocalsAsync();

		sb.AppendLine("== LANGUAGES");
		foreach ((string id, string title) in locals.Languages)
			sb.AppendLine($"{RightPad($"[{id}]", 9)} {title}");

		sb.AppendLine()
			.AppendLine("== REGIONS");
		foreach ((string id, string title) in locals.Regions)
			sb.AppendLine($"{RightPad($"[{id}]", 4)} {title}");
		
		Assert.Pass(sb.ToString());
	}

	private string RightPad(string input, int length, char appendChar = ' ')
	{
		while (input.Length < length)
			input += appendChar;
		return input;
	}
}