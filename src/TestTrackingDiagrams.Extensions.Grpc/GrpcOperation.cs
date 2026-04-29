namespace TestTrackingDiagrams.Extensions.Grpc;

/// <summary>
/// Classified Grpc operation types.
/// </summary>
public enum GrpcOperation
{
    UnaryCall,
    ServerStreamingCall,
    ClientStreamingCall,
    DuplexStreamingCall,
    Other
}
