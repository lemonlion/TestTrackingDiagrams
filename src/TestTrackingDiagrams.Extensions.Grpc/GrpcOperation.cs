namespace TestTrackingDiagrams.Extensions.Grpc;

public enum GrpcOperation
{
    UnaryCall,
    ServerStreamingCall,
    ClientStreamingCall,
    DuplexStreamingCall,
    Other
}
