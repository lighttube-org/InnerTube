using InnerTube.Protobuf;

namespace InnerTube.Models;

public class ShowingResultsFor(ShowingResultsFor.Type type, string correctedQuery, string? originalQuery)
{
	public enum Type
	{
		ShowingResultsFor = 0,
		DidYouMean = 1
	}

	public Type CorrectionType { get; } = type;
	public string? OriginalQuery { get; } = originalQuery;
	public string CorrectedQuery { get; } = correctedQuery;

	public static ShowingResultsFor? GetFromRendererWrapper(RendererWrapper? rendererWrapper)
	{
		if (rendererWrapper == null) return null;
		switch (rendererWrapper.RendererCase)
		{
			case RendererWrapper.RendererOneofCase.DidYouMeanRenderer:
				return new ShowingResultsFor(
					Type.DidYouMean,
					Utils.ReadRuns(rendererWrapper.DidYouMeanRenderer.CorrectedQuery),
					null
				);
			case RendererWrapper.RendererOneofCase.ShowingResultsForRenderer:
				return new ShowingResultsFor(
					Type.ShowingResultsFor,
					Utils.ReadRuns(rendererWrapper.ShowingResultsForRenderer.CorrectedQuery),
					Utils.ReadRuns(rendererWrapper.ShowingResultsForRenderer.OriginalQuery)
				);
			default:
				return null;
		}
	}

	public override string ToString()
	{
		return CorrectionType switch
		{
			Type.ShowingResultsFor => $"Showing results for: '{CorrectedQuery}'. Search for '{OriginalQuery}' instead",
			Type.DidYouMean => $"Did you mean: '{CorrectedQuery}'",
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}