namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// A <see cref="DelegatingHandler"/> that flushes <see cref="PendingRequestResponseLogs"/>
/// after each HTTP response. Place OUTSIDE <see cref="TestTrackingMessageHandler"/> in the
/// handler chain so it runs after the full request/response cycle.
/// </summary>
public class DeferredLogFlushHandler : DelegatingHandler
{
    private readonly Func<(string Name, string Id)> _testInfoFetcher;

    public DeferredLogFlushHandler(Func<(string Name, string Id)> testInfoFetcher)
    {
        _testInfoFetcher = testInfoFetcher;
    }

    public DeferredLogFlushHandler(TestTrackingMessageHandlerOptions options)
    {
        _testInfoFetcher = options.CurrentTestInfoFetcher
                           ?? throw new ArgumentException(
                               "CurrentTestInfoFetcher must be set on options for DeferredLogFlushHandler.",
                               nameof(options));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (PendingRequestResponseLogs.Count > 0)
        {
            var (testName, testId) = _testInfoFetcher();
            PendingRequestResponseLogs.FlushAll(testName, testId);
        }

        return response;
    }
}
