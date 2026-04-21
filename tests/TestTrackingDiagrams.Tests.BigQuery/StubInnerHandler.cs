namespace TestTrackingDiagrams.Tests.BigQuery;

internal class StubInnerHandler : HttpMessageHandler
{
    public HttpRequestMessage? CapturedRequest { get; private set; }

    private readonly HttpResponseMessage _response;

    public StubInnerHandler(HttpResponseMessage? response = null)
    {
        _response = response ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("""{"kind":"bigquery#queryResponse","rows":[]}""")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        return Task.FromResult(_response);
    }
}
