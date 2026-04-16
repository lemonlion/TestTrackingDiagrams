namespace TestTrackingDiagrams.Extensions.BlobStorage;

public record BlobOperationInfo(
    BlobOperation Operation,
    string? ContainerName,
    string? BlobName);
