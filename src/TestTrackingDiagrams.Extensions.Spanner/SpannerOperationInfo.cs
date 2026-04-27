namespace TestTrackingDiagrams.Extensions.Spanner;

public record SpannerOperationInfo(
    SpannerOperation Operation,
    string? TableName = null,
    string? DatabaseId = null,
    string? SqlText = null);
