using LightBDD.Core.Results;
using System.Reflection;

namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public class HtmlReportAdvancedOptions
{
    public bool OnlyCreateReportOnFullTestRun { get; private set; }
    public bool OnlyCreateReportOnFullySuccessfulTestRun { get; set; }
    public Assembly TestAssembly { get; private set; }
    public string Title { get; set; } = "Feature details";
    public Func<IEnumerable<DiagramAsCode>>? ExampleDiagramsAsCode { get; set; }
    public string DiagramsAsCodeCodeBehindTitle { get; set; } = "Raw Plant UML";
    public bool WriteRuntimeIds { get; set; } = true;
    public bool IncludeExecutionSummary { get; set; } = true;
    public bool IncludeFeatureSummary { get; set; } = true;
    public bool IncludeDurations { get; set; } = true;
    public bool ShowStatusFilterToggles { get; set; } = true;
    public bool ShowHappyPathToggle { get; set; } = true;
    public bool ShowExampleDiagramsToggle { get; set; } = true;
    public bool IncludeIgnoredTests { get; set; } = true;
    public bool StepsHiddenInitially { get; set; } = true;
    public bool FormatResult { get; set; }
    public Func<IScenarioResult, bool>? TreatScenariosAsPassed { get; set; }
    public bool LazyLoadDiagramImages { get; set; } = true;

    public HtmlReportAdvancedOptions SetOnlyCreateReportOnFullTestRun(Assembly testAssembly)
    {
        OnlyCreateReportOnFullTestRun = true;
        TestAssembly = testAssembly;
        return this;
    }
}