namespace TestTrackingDiagrams.Reports;

public record Feature
{
    public string DisplayName { get; set; }
    public string? Endpoint { get; set; }
    public Scenario[] Scenarios { get; set; } = [];
}