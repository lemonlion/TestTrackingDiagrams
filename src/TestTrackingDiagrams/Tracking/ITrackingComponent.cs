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
}
