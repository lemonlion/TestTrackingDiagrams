namespace TestTrackingDiagrams.Reports;

public record Feature
{
    public required string DisplayName { get; set; }
    public string? Endpoint { get; set; }
    public Scenario[] Scenarios { get; set; } = [];
    public string? Description { get; set; }
    public string[]? Labels { get; set; }
}