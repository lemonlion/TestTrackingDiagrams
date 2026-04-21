namespace TestTrackingDiagrams.Extensions.Grpc;

public record GrpcOperationInfo(
    GrpcOperation Operation,
    string? ServiceName,
    string? MethodName,
    string? FullMethodName);
