using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll.TUnit;

public static class ReqNRollReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(ReportConfigurationOptions options)
    {
        var scenarios = ReqNRollScenarioCollector.GetAll();
        var startRunTime = ReqNRollScenarioCollector.StartRunTime == default ? DateTime.UtcNow : ReqNRollScenarioCollector.StartRunTime;
        var endRunTime = ReqNRollScenarioCollector.EndRunTime == default ? DateTime.UtcNow : ReqNRollScenarioCollector.EndRunTime;
        CreateStandardReportsWithDiagrams(scenarios, startRunTime, endRunTime, options);
    }

    public static void CreateStandardReportsWithDiagrams(IEnumerable<ReqNRollScenarioInfo> scenarios, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        var features = scenarios.ToArray().ToFeatures();
        ReportGenerator.CreateStandardReportsWithDiagrams(features, startRunTime, endRunTime, options);

        var fetcherOptions = new DiagramsFetcherOptions
        {
            PlantUmlServerBaseUrl = options.PlantUmlServerBaseUrl,
            PlantUmlRendering = options.PlantUmlRendering,
            PlantUmlImageFormat = options.PlantUmlImageFormat,
            PlantUmlTheme = options.PlantUmlTheme,
            DiagramFormat = options.DiagramFormat,
            ExcludedHeaders = options.ExcludedHeaders,
            SeparateSetup = options.SeparateSetup,
            HighlightSetup = options.HighlightSetup,
            LazyLoadDiagramImages = options.LazyLoadDiagramImages,
            FocusEmphasis = options.FocusEmphasis,
            FocusDeEmphasis = options.FocusDeEmphasis,
            LocalDiagramRenderer = options.LocalDiagramRenderer,
            LocalDiagramImageDirectory = options.LocalDiagramImageDirectory,
            RequestPostFormattingProcessor = options.RequestResponsePostProcessor,
            ResponsePostFormattingProcessor = options.RequestResponsePostProcessor,
            RequestMidFormattingProcessor = options.RequestResponseMidProcessor,
            ResponseMidFormattingProcessor = options.RequestResponseMidProcessor
        };
        ReqNRollReportEnhancer.RegisterForEnhancement(fetcherOptions);
    }
}
