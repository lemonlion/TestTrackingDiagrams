namespace TestTrackingDiagrams.Extensions.CloudStorage;

public record CloudStorageOperationInfo(
    CloudStorageOperation Operation,
    string? BucketName,
    string? ObjectName = null);
