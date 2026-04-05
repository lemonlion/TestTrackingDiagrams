using LightBDD.Core.Configuration;
using System.Reflection;
using LightBDD.Contrib.ReportingEnhancements.Reports;
using LightBDD.Framework.Configuration;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD.xUnit3
{
    public static class ReportWritersConfigurationExtensions
    {
        public static ReportWritersConfiguration CreateStandardReportsWithDiagrams(this ReportWritersConfiguration configuration, Assembly testAssembly, ReportConfigurationOptions options)
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

            return configuration
                .Clear()
                .AddFileWriter<CustomisableHtmlReportFormatter>($"Reports/{options.HtmlSpecificationsFileName}.html",
            formatter =>
                {
                    formatter.Options = SpecificationsOptions.GetSpecificationsHtmlReportAdvancedOptions(testAssembly, o =>
                    {
                        o.Title = options.SpecificationsTitle;
                        o.ExampleDiagramsAsCode = diagramsFetcher;
                        o.LazyLoadDiagramImages = options.LazyLoadDiagramImages;
                        o.DiagramFormat = options.DiagramFormat;
                        o.PlantUmlRendering = options.PlantUmlRendering;
                        o.DiagramsAsCodeCodeBehindTitle = options.DiagramFormat == DiagramFormat.Mermaid ? "Raw Mermaid" : "Raw Plant UML";
                        ConfigureInternalFlow(o, options);
                    });
                    if(options.HtmlSpecificationsCustomStyleSheet is not null)
                        formatter.WithCustomCss(options.HtmlSpecificationsCustomStyleSheet);
                })
                .AddFileWriter<YamlReportFormatter>($"{reportsFilePath}/{options.YamlSpecificationsFileName}.yml",
                formatter =>
                {
                    formatter.Options = new YamlReportOptions
                    {
                        Title = options.SpecificationsTitle,
                        OnlyCreateReportOnFullySuccessfulTestRun = true
                    }.SetOnlyCreateReportOnFullTestRun(testAssembly);
                })
                .AddFileWriter<CustomisableHtmlReportFormatter>($"{reportsFilePath}/{options.HtmlTestRunReportFileName}.html",
                formatter =>
                {
                    var advancedOptions = new HtmlReportAdvancedOptions
                    {
                        ExampleDiagramsAsCode = diagramsFetcher,
                        LazyLoadDiagramImages = options.LazyLoadDiagramImages,
                        DiagramFormat = options.DiagramFormat,
                        PlantUmlRendering = options.PlantUmlRendering,
                        DiagramsAsCodeCodeBehindTitle = options.DiagramFormat == DiagramFormat.Mermaid ? "Raw Mermaid" : "Raw Plant UML"
                    };
                    ConfigureInternalFlow(advancedOptions, options);
                    formatter.Options = advancedOptions;
                });
        }

        private static void ConfigureInternalFlow(HtmlReportAdvancedOptions advancedOptions, ReportConfigurationOptions options)
        {
            if (!options.InternalFlowTracking)
                return;

            advancedOptions.InternalFlowTracking = true;

            var trackedLogs = RequestResponseLogger.RequestAndResponseLogs
                .Where(x => !(x?.TrackingIgnore ?? true))
                .ToArray();

            var spans = InternalFlowSpanCollector.CollectSpans(
                options.InternalFlowSpanGranularity,
                options.InternalFlowActivitySources);

            var perBoundarySegments = InternalFlowSegmentBuilder.BuildSegments(trackedLogs, spans);

            advancedOptions.InternalFlowDataScript = DiagramContextMenu.GetInternalFlowConfigScript(options.InternalFlowHasDataBehavior)
                + InternalFlowHtmlGenerator.GenerateSegmentDataScript(
                perBoundarySegments,
                options.InternalFlowDiagramStyle,
                options.InternalFlowShowFlameChart,
                options.InternalFlowFlameChartPosition,
                options.InternalFlowNoDataBehavior);

            if (options.WholeTestFlowVisualization != WholeTestFlowVisualization.None)
            {
                var wholeTestSegments = InternalFlowSegmentBuilder.BuildWholeTestSegments(trackedLogs, spans);

                advancedOptions.WholeTestFlowHtmlProvider = testRuntimeId =>
                {
                    var testId = testRuntimeId.ToString();
                    var boundaryLogs = trackedLogs
                        .Where(l => l.TestId == testId && l.Type == RequestResponseType.Request && l.Timestamp.HasValue)
                        .OrderBy(l => l.Timestamp!.Value)
                        .Select(l => ($"{l.Method.Value}: {l.Uri.PathAndQuery}", l.Timestamp!.Value))
                        .ToArray();

                    return InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
                        wholeTestSegments, testId, boundaryLogs, options.WholeTestFlowVisualization);
                };
            }
        }
    }
}
