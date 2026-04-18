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

public record OutlierInfo(int OutlierCount, double ThresholdMs, OutlierDetail[] TopOutliers);
public record OutlierDetail(string TestName, double DurationMs, double DeviationsFromMean);

public record CallOrderingPattern(string FirstService, string SecondService, double PctFirstBeforeSecond, int SampleCount);

public record ErrorCorrelation(string RelationshipA, string RelationshipB, double CoOccurrencePct, int CoOccurrenceCount, int TotalErrorTests);

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

public record PayloadSizeStats(
    double RequestMeanBytes,
    double RequestP95Bytes,
    double ResponseMeanBytes,
    double ResponseP95Bytes);

public record ConcurrencyInfo(
    int ConcurrentCallCount,
    double ConcurrencyPercentage,
    string[] ConcurrentPairs);

public record CallOrderEntry(
    int Position,
    string Caller,
    string Service,
    string Method,
    string Path);

public record TestCallOrdering(
    string TestId,
    string TestName,
    CallOrderEntry[] Entries);
