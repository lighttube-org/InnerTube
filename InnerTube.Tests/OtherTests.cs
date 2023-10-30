using System.Diagnostics;
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
		Stopwatch sp = new();
		StringBuilder sb = new();
		long[] times = new long[3];

		for (int i = 0; i < times.Length; i++)
		{
			sp.Restart();
			InnerTubeLocals locals = await _innerTube.GetLocalsAsync();
			sp.Stop();
			times[i] = sp.ElapsedMilliseconds;
			
			if (i != 0) continue;
			sb.AppendLine("== LANGUAGES");
			foreach ((string id, string title) in locals.Languages)
				sb.AppendLine($"{RightPad($"[{id}]", 9)} {title}");

			sb.AppendLine()
				.AppendLine("== REGIONS");
			foreach ((string id, string title) in locals.Regions)
				sb.AppendLine($"{RightPad($"[{id}]", 4)} {title}");
		}

		Assert.Pass($"Times: {string.Join(", ", times)}" + "\n\n" + sb);
	}

	private string RightPad(string input, int length, char appendChar = ' ')
	{
		while (input.Length < length)
			input += appendChar;
		return input;
	}
}