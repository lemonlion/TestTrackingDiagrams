namespace TestTrackingDiagrams.Extensions.Spanner;

/// <summary>
/// The result of classifying a Spanner operation, containing the operation type and metadata.
/// </summary>
public record SpannerOperationInfo(
    SpannerOperation Operation,
    string? TableName = null,
    string? DatabaseId = null,
    string? SqlText = null);
