using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD.xUnit2
{
    public static class ReportWritersConfigurationExtensions
    {
        public static ReportWritersConfiguration CreateStandardReportsWithDiagrams(this ReportWritersConfiguration configuration, ReportConfigurationOptions options)
        {
            var fetcherOptions = new DiagramsFetcherOptions
            {
                PlantUmlServerBaseUrl = options.PlantUmlServerBaseUrl,
                RequestPostFormattingProcessor = options.RequestResponsePostProcessor,
                ResponsePostFormattingProcessor = options.RequestResponsePostProcessor,
                RequestMidFormattingProcessor = options.RequestResponseMidProcessor,
                ResponseMidFormattingProcessor = options.RequestResponseMidProcessor,
                ExcludedHeaders = options.ExcludedHeaders,
                SeparateSetup = options.SeparateSetup,
                HighlightSetup = options.HighlightSetup,
                LazyLoadDiagramImages = options.LazyLoadDiagramImages,
                FocusEmphasis = options.FocusEmphasis,
                FocusDeEmphasis = options.FocusDeEmphasis,
                PlantUmlTheme = options.PlantUmlTheme,
                PlantUmlImageFormat = options.PlantUmlImageFormat,
                DiagramFormat = options.DiagramFormat,
                PlantUmlRendering = options.PlantUmlRendering,
                LocalDiagramRenderer = options.LocalDiagramRenderer,
                LocalDiagramImageDirectory = options.LocalDiagramImageDirectory
            };
            var diagramsFetcher = LightBddDiagramsFetcher.GetDiagramsFetcher(fetcherOptions);
            var reportsFilePath = options.ReportsFolderPath.Trim().TrimEnd('/');

            // Build internal flow data once, shared across formatters
            var internalFlowDataScript = "";
            Dictionary<string, InternalFlowSegment>? wholeTestSegments = null;
            RequestResponseLog[]? trackedLogs = null;
            if (options.InternalFlowTracking)
            {
                trackedLogs = RequestResponseLogger.RequestAndResponseLogs
                    .Where(x => !(x?.TrackingIgnore ?? true))
                    .ToArray();

                var spans = InternalFlowSpanCollector.CollectSpans(
                    options.InternalFlowSpanGranularity,
                    options.InternalFlowActivitySources);

                var perBoundarySegments = InternalFlowSegmentBuilder.BuildSegments(trackedLogs, spans);

                internalFlowDataScript = DiagramContextMenu.GetInternalFlowConfigScript(options.InternalFlowHasDataBehavior)
                    + InternalFlowHtmlGenerator.GenerateSegmentDataScript(
                    perBoundarySegments,
                    options.InternalFlowDiagramStyle,
                    options.InternalFlowShowFlameChart,
                    options.InternalFlowFlameChartPosition,
                    options.InternalFlowNoDataBehavior);

                if (options.WholeTestFlowVisualization != WholeTestFlowVisualization.None)
                {
                    wholeTestSegments = InternalFlowSegmentBuilder.BuildWholeTestSegments(trackedLogs, spans);
                }
            }

            return configuration
                .Clear()
                .AddFileWriter<UnifiedReportFormatter>($"Reports/{options.HtmlSpecificationsFileName}.html",
                formatter =>
                {
                    formatter.Title = options.SpecificationsTitle;
                    formatter.IncludeTestRunData = false;
                    formatter.GenerateBlankOnFailedTests = true;
                    formatter.DiagramsFetcher = diagramsFetcher;
                    formatter.LazyLoadImages = options.LazyLoadDiagramImages;
                    formatter.DiagramFormat = options.DiagramFormat;
                    formatter.PlantUmlRendering = options.PlantUmlRendering;
                    formatter.InlineSvgRendering = options.InlineSvgRendering;
                    formatter.Stylesheet = options.HtmlSpecificationsCustomStyleSheet;
                    formatter.InternalFlowTracking = options.InternalFlowTracking;
                    formatter.InternalFlowDataScript = internalFlowDataScript;
                    formatter.WholeTestSegments = wholeTestSegments;
                    formatter.TrackedLogs = trackedLogs;
                    formatter.WholeTestVisualization = options.WholeTestFlowVisualization;
                })
                .AddFileWriter<UnifiedYamlFormatter>($"{reportsFilePath}/{options.YamlSpecificationsFileName}.yml",
                formatter =>
                {
                    formatter.Title = options.SpecificationsTitle;
                    formatter.GenerateBlankOnFailedTests = true;
                    formatter.DiagramsFetcher = diagramsFetcher;
                })
                .AddFileWriter<UnifiedReportFormatter>($"{reportsFilePath}/{options.HtmlTestRunReportFileName}.html",
                formatter =>
                {
                    formatter.DiagramsFetcher = diagramsFetcher;
                    formatter.LazyLoadImages = options.LazyLoadDiagramImages;
                    formatter.DiagramFormat = options.DiagramFormat;
                    formatter.PlantUmlRendering = options.PlantUmlRendering;
                    formatter.InlineSvgRendering = options.InlineSvgRendering;
                    formatter.InternalFlowTracking = options.InternalFlowTracking;
                    formatter.InternalFlowDataScript = internalFlowDataScript;
                    formatter.WholeTestSegments = wholeTestSegments;
                    formatter.TrackedLogs = trackedLogs;
                    formatter.WholeTestVisualization = options.WholeTestFlowVisualization;
                });
        }
    }
}
