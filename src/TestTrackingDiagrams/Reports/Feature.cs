namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Represents a test feature (a logical group of scenarios) in the report.
/// </summary>
public record Feature
{
    public required string DisplayName { get; set; }
    public string? Endpoint { get; set; }
    public Scenario[] Scenarios { get; set; } = [];
    public string? Description { get; set; }
    public string[]? Labels { get; set; }
}