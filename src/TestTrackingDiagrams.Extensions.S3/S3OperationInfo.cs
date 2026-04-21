namespace TestTrackingDiagrams.Extensions.S3;

public record S3OperationInfo(
    S3Operation Operation,
    string? BucketName,
    string? KeyName = null);
