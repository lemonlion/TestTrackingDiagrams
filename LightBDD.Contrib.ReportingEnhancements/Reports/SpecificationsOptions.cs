using System.Reflection;

namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public static class SpecificationsOptions
    {
        public static HtmlReportAdvancedOptions GetSpecificationsHtmlReportAdvancedOptions(Assembly testAssembly, Action<HtmlReportAdvancedOptions>? overrides = null)
        {
            var options = new HtmlReportAdvancedOptions
            {
                IncludeIgnoredTests = false,
                IncludeFeatureSummary = false,
                ShowHappyPathToggle = true,
                ShowExampleDiagramsToggle = true,
                Title = "Specifications",
                WriteRuntimeIds = false,
                IncludeDurations = false,
                IncludeExecutionSummary = false,
                OnlyCreateReportOnFullySuccessfulTestRun = true,
                ShowStatusFilterToggles = false,
                StepsHiddenInitially = true
            }.SetOnlyCreateReportOnFullTestRun(testAssembly);

            overrides?.Invoke(options);

            return options;
        }
    }
}
