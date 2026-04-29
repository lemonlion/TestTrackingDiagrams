namespace TestTrackingDiagrams.Extensions.S3;

/// <summary>
/// The result of classifying a S3 operation, containing the operation type and metadata.
/// </summary>
public record S3OperationInfo(
    S3Operation Operation,
    string? BucketName,
    string? KeyName = null);
