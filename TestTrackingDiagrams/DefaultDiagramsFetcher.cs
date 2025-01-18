using TestTrackingDiagrams.PlantUml;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

public static class DefaultDiagramsFetcher 
{
    private static DiagramAsCode[]? _diagrams;

    public static Func<DiagramAsCode[]> GetDiagramsFetcher(DiagramsFetcherOptions? options = null)
    {
        options ??= new DiagramsFetcherOptions();

        if (_diagrams is not null)
            return () => _diagrams;

        return () =>
        {
            var perTestId = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
                RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
                requestPostFormattingProcessor: options.RequestPostFormattingProcessor,
                responsePostFormattingProcessor: options.ResponsePostFormattingProcessor,
                requestPreFormattingProcessor: options.RequestPreFormattingProcessor,
                responsePreFormattingProcessor: options.ResponsePreFormattingProcessor,
                excludedHeaders: options.ExcludedHeaders.ToArray()).ToArray();
            return _diagrams = perTestId
                .SelectMany(test => test.PlantUmls.Select(plantUml =>
                    new DiagramAsCode(test.TestId,
                        $"{options.PlantUmlServerBaseUrl}/png/{plantUml.PlantUmlEncoded}",
                        plantUml.PlainText)))
                .ToArray();
        };
    }

    public record DiagramAsCode(string TestRuntimeId, string ImgSrc, string CodeBehind);
}