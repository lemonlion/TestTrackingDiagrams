using Grpc.Core;

namespace TestTrackingDiagrams.Extensions.Grpc;

public static class GrpcOperationClassifier
{
    public static GrpcOperationInfo Classify(
        MethodType methodType, string? fullName, string? serviceName, string? methodName)
    {
        var operation = methodType switch
        {
            MethodType.Unary => GrpcOperation.UnaryCall,
            MethodType.ServerStreaming => GrpcOperation.ServerStreamingCall,
            MethodType.ClientStreaming => GrpcOperation.ClientStreamingCall,
            MethodType.DuplexStreaming => GrpcOperation.DuplexStreamingCall,
            _ => GrpcOperation.Other
        };

        return new GrpcOperationInfo(operation, serviceName, methodName, fullName);
    }

    public static string GetDiagramLabel(GrpcOperationInfo op, GrpcTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            GrpcTrackingVerbosity.Raw =>
                $"{op.FullMethodName} [{op.Operation}]",

            GrpcTrackingVerbosity.Detailed =>
                op.Operation switch
                {
                    GrpcOperation.UnaryCall => op.MethodName ?? "Call",
                    GrpcOperation.ServerStreamingCall => $"{op.MethodName} (server-stream)",
                    GrpcOperation.ClientStreamingCall => $"{op.MethodName} (client-stream)",
                    GrpcOperation.DuplexStreamingCall => $"{op.MethodName} (duplex-stream)",
                    _ => op.MethodName ?? "Call"
                },

            GrpcTrackingVerbosity.Summarised =>
                op.MethodName ?? "Call",

            _ => op.MethodName ?? "Call"
        };
    }
}
