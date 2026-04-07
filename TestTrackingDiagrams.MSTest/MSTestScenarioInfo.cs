using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestTrackingDiagrams.MSTest;

public record MSTestScenarioInfo
{
    public required string TestClassSimpleName { get; init; }
    public required string TestMethodName { get; init; }
    public required string TestId { get; init; }
    public required UnitTestOutcome Outcome { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorStackTrace { get; init; }
    public string? Endpoint { get; init; }
    public bool IsHappyPath { get; init; }
    public TimeSpan? Duration { get; init; }
}
