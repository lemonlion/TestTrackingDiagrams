namespace TestTrackingDiagrams.Tracking;

public class TestTrackingMessageHandler : DelegatingHandler
{
    private readonly string _serviceName;
    private readonly Func<string> _currentTestInfoFetcher;

    public TestTrackingMessageHandler(string serviceName, Func<string> currentTestInfoFetcher)
    {
        _serviceName = serviceName;
        _currentTestInfoFetcher = currentTestInfoFetcher;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestContentString = request.Content is null
            ? null : await request.Content!.ReadAsStringAsync(cancellationToken);
        var requestHeaders = request.Headers.Select(x => (x.Key, x.Value.First())).ToArray();

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContentString = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseHeaders = response.Headers.Select(x => (x.Key, x.Value.First())).ToArray();

        RequestResponseLogger.Log(new RequestResponseLog(
            _currentTestInfoFetcher(),
            new RequestLog(request.Method, requestContentString, request.RequestUri!, requestHeaders, _serviceName),
            new ResponseLog(response.StatusCode, responseContentString, responseHeaders)));

        return response;
    }
}