using System.Reflection;
using System.Runtime.CompilerServices;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD
{
    public static class ReportWritersConfigurationExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ReportWritersConfiguration CreateStandardReportsWithDiagrams(
            this ReportWritersConfiguration configuration,
            ReportConfigurationOptions options,
            Func<Assembly, int> testCountResolver)
        {
            return CreateStandardReportsWithDiagramsInternal(configuration, options, Assembly.GetCallingAssembly(), testCountResolver);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ReportWritersConfiguration CreateStandardReportsWithDiagramsInternal(
            ReportWritersConfiguration configuration, ReportConfigurationOptions options, Assembly testAssembly, Func<Assembly, int> testCountResolver)
        {
            if (options.InternalFlowTracking && options.DiagramFormat == DiagramFormat.PlantUml)
            {
                if (options.PlantUmlRendering is PlantUmlRendering.Server or PlantUmlRendering.Local or PlantUmlRendering.NodeJs)
                {
                    options.InlineSvgRendering = true;
                    options.PlantUmlImageFormat = PlantUmlImageFormat.Svg;
                }
            }

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
                LocalDiagramImageDirectory = options.LocalDiagramImageDirectory,
                InlineSvgRendering = options.InlineSvgRendering,
                InternalFlowTracking = options.InternalFlowTracking
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
                    options.InternalFlowNoDataBehavior,
                    options.InternalFlowSpanGranularity,
                    options.InternalFlowActivitySources);

                if (options.WholeTestFlowVisualization != WholeTestFlowVisualization.None)
                {
                    wholeTestSegments = InternalFlowSegmentBuilder.BuildWholeTestSegments(trackedLogs, spans);
                }
            }

            var ciMetadata = CiMetadataDetector.Detect();

            var specsDataExtension = GetDataFormatExtension(options.SpecificationsDataFormat);
            var testRunDataExtension = GetDataFormatExtension(options.TestRunReportDataFormat);

            var testRunReportTitle = ReportGenerator.GetTestRunReportTitle(options);

            // Generate the static schema file eagerly (it doesn't depend on test results)
            if (options.GenerateTestRunReportSchema)
            {
                var schemaExtension = options.TestRunReportDataFormat == DataFormat.Xml ? "xsd" : "json";
                ReportGenerator.GenerateTestRunReportSchema(
                    $"{reportsFilePath}/{options.HtmlTestRunReportFileName}.schema.{schemaExtension}",
                    options.TestRunReportDataFormat);
            }

            configuration.Clear();

            if (options.GenerateSpecificationsReport)
            {
                configuration.AddFileWriter<UnifiedReportFormatter>($"{reportsFilePath}/{options.HtmlSpecificationsFileName}.html",
                formatter =>
                {
                    formatter.Title = options.SpecificationsTitle;
                    formatter.IncludeTestRunData = false;
                    formatter.GenerateBlankOnFailedTests = true;
                    formatter.ExpectedTestCount = () => testCountResolver(testAssembly);
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
                    formatter.ShowStepNumbers = options.SpecificationsShowStepNumbers;
                    formatter.CustomCss = options.CustomCss;
                    formatter.CustomFaviconBase64 = options.CustomFaviconBase64;
                    formatter.CustomLogoHtml = options.CustomLogoHtml;
                    formatter.GroupParameterizedTests = options.GroupParameterizedTests;
                    formatter.MaxParameterColumns = options.MaxParameterColumns;
                    formatter.TitleizeParameterNames = options.TitleizeParameterNames;
                });
            }

            if (options.GenerateSpecificationsData)
            {
                configuration.AddFileWriter<UnifiedSpecificationsDataFormatter>($"{reportsFilePath}/{options.YamlSpecificationsFileName}.{specsDataExtension}",
                formatter =>
                {
                    formatter.Title = options.SpecificationsTitle;
                    formatter.GenerateBlankOnFailedTests = true;
                    formatter.ExpectedTestCount = () => testCountResolver(testAssembly);
                    formatter.DiagramsFetcher = diagramsFetcher;
                    formatter.DataFormat = options.SpecificationsDataFormat;
                });
            }

            if (options.GenerateTestRunReport)
            {
                configuration.AddFileWriter<UnifiedReportFormatter>($"{reportsFilePath}/{options.HtmlTestRunReportFileName}.html",
                formatter =>
                {
                    formatter.Title = testRunReportTitle;
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
                    formatter.CiMetadata = ciMetadata;
                    formatter.ShowStepNumbers = options.FeaturesReportShowStepNumbers;
                    formatter.CustomCss = options.CustomCss;
                    formatter.CustomFaviconBase64 = options.CustomFaviconBase64;
                    formatter.CustomLogoHtml = options.CustomLogoHtml;
                    formatter.GroupParameterizedTests = options.GroupParameterizedTests;
                    formatter.MaxParameterColumns = options.MaxParameterColumns;
                    formatter.TitleizeParameterNames = options.TitleizeParameterNames;
                });
            }

            if (options.GenerateTestRunReportData)
            {
                configuration.AddFileWriter<UnifiedTestRunDataFormatter>($"{reportsFilePath}/{options.HtmlTestRunReportFileName}.{testRunDataExtension}",
                formatter =>
                {
                    formatter.ExpectedTestCount = () => testCountResolver(testAssembly);
                    formatter.DiagramsFetcher = diagramsFetcher;
                    formatter.DataFormat = options.TestRunReportDataFormat;
                    formatter.TrackedLogs = trackedLogs;
                });
            }

            return configuration;
        }

        private static string GetDataFormatExtension(DataFormat format) => format switch
        {
            DataFormat.Json => "json",
            DataFormat.Xml => "xml",
            DataFormat.Yaml => "yml",
            _ => "json"
        };
    }
}
