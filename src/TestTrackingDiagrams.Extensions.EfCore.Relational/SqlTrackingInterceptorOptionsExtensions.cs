using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public static class SqlTrackingInterceptorOptionsExtensions
{
    /// <summary>
    /// Copies <see cref="TestTrackingMessageHandlerOptions.CurrentTestInfoFetcher"/>,
    /// <see cref="TestTrackingMessageHandlerOptions.CurrentStepTypeFetcher"/>, and
    /// <see cref="TestTrackingMessageHandlerOptions.CallerName"/> from an existing
    /// HTTP tracking options instance so that SQL and HTTP tracking share the same test context.
    /// </summary>
    public static SqlTrackingInterceptorOptions WithTestInfoFrom(
        this SqlTrackingInterceptorOptions sqlOptions,
        TestTrackingMessageHandlerOptions httpOptions)
    {
        sqlOptions.CurrentTestInfoFetcher = httpOptions.CurrentTestInfoFetcher;
        sqlOptions.CurrentStepTypeFetcher = httpOptions.CurrentStepTypeFetcher;
        sqlOptions.CallerName = httpOptions.CallerName;
        return sqlOptions;
    }
}
