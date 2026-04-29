namespace TestTrackingDiagrams.Extensions.Grpc;

/// <summary>
/// The result of classifying a Grpc operation, containing the operation type and metadata.
/// </summary>
public record GrpcOperationInfo(
    GrpcOperation Operation,
    string? ServiceName,
    string? MethodName,
    string? FullMethodName);
