namespace TestTrackingDiagrams.Reports;

public record ParameterizedGroup(
    string GroupDisplayName,
    string[] ParameterNames,
    ParameterDisplayRule Rule,
    Scenario[] Scenarios,
    bool AllDiagramsIdentical
);
