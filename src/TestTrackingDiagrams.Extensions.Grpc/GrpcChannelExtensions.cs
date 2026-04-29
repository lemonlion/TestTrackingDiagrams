using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace TestTrackingDiagrams.Extensions.Grpc;

/// <summary>
/// Provides extension methods for configuring gRPC client options to enable test tracking.
/// </summary>
public static class GrpcChannelExtensions
{
    public static CallInvoker WithTestTracking(
        this GrpcChannel channel,
        GrpcTrackingOptions options)
    {
        return channel.Intercept(new GrpcTrackingInterceptor(options, options.HttpContextAccessor));
    }
}