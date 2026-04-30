namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Implemented by tracking handlers, interceptors, and proxies to enable
/// automated detection of misconfigured components that were registered but
/// never invoked during a test run.
/// </summary>
public interface ITrackingComponent
{
    /// <summary>
    /// Human-readable name shown in diagnostic messages (e.g. "SqlTrackingInterceptor", "CosmosDB handler").
    /// </summary>
    string ComponentName { get; }

    /// <summary>
    /// True once the component has processed at least one request/command.
    /// </summary>
    bool WasInvoked { get; }

    /// <summary>
    /// Number of requests or commands processed so far.
    /// </summary>
    int InvocationCount { get; }

    /// <summary>
    /// Indicates whether this component has an <c>IHttpContextAccessor</c> configured.
    /// Components without one cannot resolve test identity from HTTP request headers.
    /// Returns <c>null</c> when the concept is not applicable (e.g. SQL interceptors).
    /// </summary>
    bool HasHttpContextAccessor => false;
}
