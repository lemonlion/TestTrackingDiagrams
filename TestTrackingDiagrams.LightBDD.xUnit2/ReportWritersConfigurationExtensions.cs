using LightBDD.Core.Configuration;
using System.Reflection;
using LightBDD.Contrib.ReportingEnhancements.Reports;
using LightBDD.Framework.Configuration;

namespace TestTrackingDiagrams.LightBDD.xUnit2
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
                    formatter.Options = new HtmlReportAdvancedOptions
                    {
                        ExampleDiagramsAsCode = diagramsFetcher,
                        LazyLoadDiagramImages = options.LazyLoadDiagramImages,
                        DiagramFormat = options.DiagramFormat,
                        PlantUmlRendering = options.PlantUmlRendering,
                        DiagramsAsCodeCodeBehindTitle = options.DiagramFormat == DiagramFormat.Mermaid ? "Raw Mermaid" : "Raw Plant UML"
                    };
                });
        }
    }
}
