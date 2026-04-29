namespace TestTrackingDiagrams.Reports;

/// <summary>
/// A group of parameterized test scenarios that share the same base test name
/// but differ in their parameter values. Displayed as a table in the HTML report.
/// </summary>
public record ParameterizedGroup(
    string GroupDisplayName,
    string[] ParameterNames,
    ParameterDisplayRule Rule,
    Scenario[] Scenarios,
    bool AllDiagramsIdentical
);
