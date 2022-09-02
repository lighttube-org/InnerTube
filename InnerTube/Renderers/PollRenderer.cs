using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class PollRenderer : IRenderer
{
	public string Type => "pollRenderer";

	public IEnumerable<Choice> Choices { get; }
	public string TotalVotes { get; }

	public PollRenderer(JToken renderer)
	{
		Choices = renderer.GetFromJsonPath<JArray>("choices")!.Select(x => new Choice(x));
		TotalVotes = renderer.GetFromJsonPath<string>("totalVotes.simpleText")!;
	}

	public class Choice
	{
		public string Text { get; }

		public Choice(JToken choice)
		{
			Text = Utils.ReadRuns(choice.GetFromJsonPath<JArray>("text.runs")!);
		}

		public override string ToString() => $"[Choice] {Text}";
	}

	public override string ToString()
	{
		StringBuilder sb = new();
		sb.AppendLine($"[{Type}] {TotalVotes}");

		foreach (Choice choice in Choices)
			sb.AppendLine(choice.ToString());

		return sb.ToString();
	}
}