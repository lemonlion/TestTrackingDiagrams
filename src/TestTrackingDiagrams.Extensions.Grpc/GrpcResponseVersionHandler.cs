namespace TestTrackingDiagrams.Extensions.Grpc;

/// <summary>
/// A delegating handler that copies the HTTP request version onto the response.
/// This is required when testing gRPC services in-process with <c>TestServer</c>
/// because <c>TestServer</c> returns HTTP/1.1 responses, but the gRPC client
/// expects HTTP/2. Without this handler, the gRPC client throws
/// <c>RpcException</c> with status <c>Internal</c>.
/// <para>
/// You do not need to use this handler directly if you use
/// <see cref="GrpcWebApplicationFactoryExtensions.CreateTestTrackingGrpcClient{TEntryPoint,TClient}"/>
/// — it is applied automatically.
/// </para>
/// </summary>
public class GrpcResponseVersionHandler : DelegatingHandler
{
    /// <summary>
    /// Initialises a new <see cref="GrpcResponseVersionHandler"/> that wraps the given inner handler.
    /// </summary>
    /// <param name="innerHandler">The inner handler, typically from <c>TestServer.CreateHandler()</c>.</param>
    public GrpcResponseVersionHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        response.Version = request.Version;
        return response;
    }
}
