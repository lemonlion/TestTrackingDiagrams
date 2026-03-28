using System.Reflection;
using LightBDD.Core.Results;

namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public class YamlReportOptions
{
    public bool OnlyCreateReportOnFullTestRun { get; private set; }
    public bool OnlyCreateReportOnFullySuccessfulTestRun { get; set; }
    public Assembly TestAssembly { get; private set; }
    public string Title { get; set; } = "Feature details";

    public YamlReportOptions SetOnlyCreateReportOnFullTestRun(Assembly testAssembly)
    {
        OnlyCreateReportOnFullTestRun = true;
        TestAssembly = testAssembly;
        return this;
    }
}