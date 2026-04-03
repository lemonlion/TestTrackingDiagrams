using TestTrackingDiagrams.Mermaid;
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
            if (options.DiagramFormat == DiagramFormat.Mermaid)
                return _diagrams = GetMermaidDiagrams(options);

            if (options.DiagramFormat == DiagramFormat.PlantUmlBrowser)
                return _diagrams = GetPlantUmlBrowserDiagrams(options);

            var perTestId = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
                RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
                requestPostFormattingProcessor: options.RequestPostFormattingProcessor,
                responsePostFormattingProcessor: options.ResponsePostFormattingProcessor,
                requestPreFormattingProcessor: options.RequestPreFormattingProcessor,
                responsePreFormattingProcessor: options.ResponsePreFormattingProcessor,
                requestMidFormattingProcessor: options.RequestMidFormattingProcessor,
                responseMidFormattingProcessor: options.ResponseMidFormattingProcessor,
                excludedHeaders: options.ExcludedHeaders.ToArray(),
                separateSetup: options.SeparateSetup,
                highlightSetup: options.HighlightSetup,
                lazyLoadImages: options.LazyLoadDiagramImages,
                focusEmphasis: options.FocusEmphasis,
                focusDeEmphasis: options.FocusDeEmphasis,
                plantUmlTheme: options.PlantUmlTheme).ToArray();

            if (options.LocalDiagramRenderer is not null)
                return _diagrams = RenderLocally(perTestId, options);

            if (options.PlantUmlImageFormat is PlantUmlImageFormat.Base64Png or PlantUmlImageFormat.Base64Svg)
                throw new InvalidOperationException(
                    $"PlantUmlImageFormat.{options.PlantUmlImageFormat} requires a LocalDiagramRenderer to be configured. " +
                    "Install the TestTrackingDiagrams.PlantUml.Ikvm package and use IkvmPlantUmlRenderer.Render.");

            return _diagrams = perTestId
                .SelectMany(test => test.PlantUmls.Select(plantUml =>
                    new DiagramAsCode(test.TestId,
                        $"{options.PlantUmlServerBaseUrl}/{options.PlantUmlImageFormat.ToString().ToLowerInvariant()}/{plantUml.PlantUmlEncoded}",
                        plantUml.PlainText)))
                .ToArray();
        };
    }

    private static DiagramAsCode[] RenderLocally(PlantUmlCreator.PlantUmlForTest[] perTestId, DiagramsFetcherOptions options)
    {
        var isBase64 = options.PlantUmlImageFormat is PlantUmlImageFormat.Base64Png or PlantUmlImageFormat.Base64Svg;
        var isFile = !isBase64;

        if (isFile && string.IsNullOrWhiteSpace(options.LocalDiagramImageDirectory))
            throw new InvalidOperationException(
                "LocalDiagramImageDirectory must be set when using LocalDiagramRenderer with Png or Svg format. " +
                "Set it to a directory path where diagram images should be saved (e.g. the 'images' subfolder next to your reports).");

        if (isFile)
            Directory.CreateDirectory(options.LocalDiagramImageDirectory!);

        var renderFormat = options.PlantUmlImageFormat switch
        {
            PlantUmlImageFormat.Base64Png => PlantUmlImageFormat.Png,
            PlantUmlImageFormat.Base64Svg => PlantUmlImageFormat.Svg,
            _ => options.PlantUmlImageFormat
        };

        var extension = renderFormat == PlantUmlImageFormat.Png ? ".png" : ".svg";
        var mimeType = renderFormat == PlantUmlImageFormat.Png ? "image/png" : "image/svg+xml";
        var imagesFolderName = isFile ? Path.GetFileName(options.LocalDiagramImageDirectory!) : null;
        var counter = 0;

        return perTestId
            .SelectMany(test => test.PlantUmls.Select(plantUml =>
            {
                var imageBytes = options.LocalDiagramRenderer!(plantUml.PlainText, renderFormat);

                string imgSrc;
                if (isBase64)
                {
                    imgSrc = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
                }
                else
                {
                    var fileName = $"diagram_{Interlocked.Increment(ref counter)}{extension}";
                    var filePath = Path.Combine(options.LocalDiagramImageDirectory!, fileName);
                    File.WriteAllBytes(filePath, imageBytes);
                    imgSrc = $"{imagesFolderName}/{fileName}";
                }

                return new DiagramAsCode(test.TestId, imgSrc, plantUml.PlainText);
            }))
            .ToArray();
    }

    private static DiagramAsCode[] GetPlantUmlBrowserDiagrams(DiagramsFetcherOptions options)
    {
        var perTestId = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
            requestPostFormattingProcessor: options.RequestPostFormattingProcessor,
            responsePostFormattingProcessor: options.ResponsePostFormattingProcessor,
            requestPreFormattingProcessor: options.RequestPreFormattingProcessor,
            responsePreFormattingProcessor: options.ResponsePreFormattingProcessor,
            requestMidFormattingProcessor: options.RequestMidFormattingProcessor,
            responseMidFormattingProcessor: options.ResponseMidFormattingProcessor,
            excludedHeaders: options.ExcludedHeaders.ToArray(),
            separateSetup: options.SeparateSetup,
            highlightSetup: options.HighlightSetup,
            lazyLoadImages: false,
            focusEmphasis: options.FocusEmphasis,
            focusDeEmphasis: options.FocusDeEmphasis,
            plantUmlTheme: options.PlantUmlTheme).ToArray();

        return perTestId
            .SelectMany(test => test.PlantUmls.Select(plantUml =>
                new DiagramAsCode(test.TestId, string.Empty, plantUml.PlainText)))
            .ToArray();
    }

    private static DiagramAsCode[] GetMermaidDiagrams(DiagramsFetcherOptions options)
    {
        var perTestId = MermaidCreator.GetMermaidDiagramsPerTestId(
            RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
            requestPostFormattingProcessor: options.RequestPostFormattingProcessor,
            responsePostFormattingProcessor: options.ResponsePostFormattingProcessor,
            requestPreFormattingProcessor: options.RequestPreFormattingProcessor,
            responsePreFormattingProcessor: options.ResponsePreFormattingProcessor,
            requestMidFormattingProcessor: options.RequestMidFormattingProcessor,
            responseMidFormattingProcessor: options.ResponseMidFormattingProcessor,
            excludedHeaders: options.ExcludedHeaders.ToArray(),
            separateSetup: options.SeparateSetup,
            highlightSetup: options.HighlightSetup,
            focusEmphasis: options.FocusEmphasis,
            focusDeEmphasis: options.FocusDeEmphasis).ToArray();

        return perTestId
            .SelectMany(test => test.Diagrams.Select(diagram =>
                new DiagramAsCode(test.TestId, string.Empty, diagram)))
            .ToArray();
    }

    public record DiagramAsCode(string TestRuntimeId, string ImgSrc, string CodeBehind);
}