using System.Net;

namespace TestTrackingDiagrams.ComponentDiagram;

/// <summary>
/// Aggregated performance statistics for a single (Caller → Service) relationship.
/// Computed from request/response timestamp pairs across all tests.
/// </summary>
public record RelationshipStats(
    int CallCount,
    int TestCount,
    double MeanMs,
    double MedianMs,
    double P95Ms,
    double P99Ms,
    double MinMs,
    double MaxMs,
    double ErrorRate,
    Dictionary<HttpStatusCode, int> StatusCodeDistribution,
    EndpointStats[] EndpointBreakdown,
    PayloadSizeStats? PayloadSizes,
    ConcurrencyInfo? Concurrency,
    bool IsLowCoverage,
    double CoefficientOfVariation,
    Dictionary<string, int> MethodDistribution,
    OutlierInfo? Outliers,
    double LatencyContributionPct);

/// <summary>
/// Contains information about statistical outliers in relationship call durations.
/// </summary>
public record OutlierInfo(int OutlierCount, double ThresholdMs, OutlierDetail[] TopOutliers);
public record OutlierDetail(string TestName, double DurationMs, double DeviationsFromMean);

/// <summary>
/// Describes the ordering pattern between two services, indicating how frequently one is called before the other.
/// </summary>
public record CallOrderingPattern(string FirstService, string SecondService, double PctFirstBeforeSecond, int SampleCount);

/// <summary>
/// Describes the correlation between errors in two relationships, indicating how often they fail together.
/// </summary>
public record ErrorCorrelation(string RelationshipA, string RelationshipB, double CoOccurrencePct, int CoOccurrenceCount, int TotalErrorTests);

/// <summary>
/// Contains aggregated statistics for an individual endpoint in a relationship.
/// </summary>
public record EndpointStats(
    string Method,
    string Path,
    int CallCount,
    double MeanMs,
    double MedianMs,
    double P95Ms,
    double P99Ms,
    double MinMs,
    double MaxMs,
    double ErrorRate);

/// <summary>
/// Contains statistics about request and response payload sizes for a relationship.
/// </summary>
public record PayloadSizeStats(
    double RequestMeanBytes,
    double RequestP95Bytes,
    double ResponseMeanBytes,
    double ResponseP95Bytes);

/// <summary>
/// Contains concurrency metrics for a relationship, including maximum concurrent calls and average concurrency.
/// </summary>
public record ConcurrencyInfo(
    int ConcurrentCallCount,
    double ConcurrencyPercentage,
    string[] ConcurrentPairs);

/// <summary>
/// Represents a single call in the ordering sequence, capturing its timestamp, duration, and test context.
/// </summary>
public record CallOrderEntry(
    int Position,
    string Caller,
    string Service,
    string Method,
    string Path);

/// <summary>
/// Contains the complete call ordering sequence for a specific test execution.
/// </summary>
public record TestCallOrdering(
    string TestId,
    string TestName,
    CallOrderEntry[] Entries);