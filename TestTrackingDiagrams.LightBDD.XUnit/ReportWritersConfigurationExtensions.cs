using LightBDD.Core.Configuration;
using System.Reflection;
using LightBDD.Contrib.ReportingEnhancements.Reports;
using LightBDD.Framework.Configuration;

namespace TestTrackingDiagrams.LightBDD.XUnit
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
                ExcludedHeaders = options.ExcludedHeaders
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
                        ExampleDiagramsAsCode = diagramsFetcher
                    };
                });
        }
    }
}
