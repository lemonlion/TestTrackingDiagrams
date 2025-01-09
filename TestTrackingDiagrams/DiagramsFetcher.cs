using TestTrackingDiagrams.PlantUml;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

public static class DiagramsFetcher
{
    private static DiagramAsCode[]? _diagrams;

    public static Func<DiagramAsCode[]> GetDiagramsFetcher(string plantUmlServerBaseUrl, Func<string, string>? processor = null)
    {
        if (_diagrams is not null)
            return () => _diagrams;

        return () =>
        {
            var perTestId = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
                RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
                requestPostFormattingProcessor: processor,
                responsePostFormattingProcessor: processor).ToArray();
            return _diagrams = perTestId
                .SelectMany(test => test.PlantUmls.Select(plantUml =>
                    new DiagramAsCode(test.TestId,
                        $"{plantUmlServerBaseUrl}/png/{plantUml.PlantUmlEncoded}",
                        plantUml.PlainText)))
                .ToArray();
        };
    }

    public record DiagramAsCode(string TestRuntimeId, string ImgSrc, string CodeBehind);
}