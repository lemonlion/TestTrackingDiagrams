using System.Net;
using TestTrackingDiagrams.Extensions.AtlasDataApi;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.AtlasDataApi;

public class AtlasDataApiTrackingMessageHandlerTests
{
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsForTest()
        => RequestResponseLogger.RequestAndResponseLogs.Where(l => l.TestId == _testId).ToArray();

    // ── Basic tracking ──

    [Fact]
    public async Task SendAsync_TracksRequest_AndResponse()
    {
        var options = CreateOptions();
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"documents":[]}""")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var body = """{"dataSource":"Cluster0","database":"myDb","collection":"users","filter":{}}""";
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/find");
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var logs = GetLogsForTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public async Task SendAsync_SetsAtlasDataApiDependencyCategory()
    {
        var options = CreateOptions();
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/findOne");
        request.Content = new StringContent("""{"dataSource":"C","database":"db","collection":"col"}""",
            System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var logs = GetLogsForTest();
        Assert.All(logs, l => Assert.Equal("AtlasDataApi", l.DependencyCategory));
    }

    [Fact]
    public async Task SendAsync_BuildsCleanUri_WithDatabaseAndCollection()
    {
        var options = CreateOptions();
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/insertOne");
        request.Content = new StringContent("""{"dataSource":"C","database":"orders_db","collection":"orders"}""",
            System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var log = GetLogsForTest().First();
        Assert.Equal(new Uri("atlas:///orders_db/orders"), log.Uri);
    }

    // ── Verbosity ──

    [Fact]
    public async Task SendAsync_Summarised_NoContent()
    {
        var options = CreateOptions();
        options.Verbosity = AtlasDataApiTrackingVerbosity.Summarised;
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"document":{"_id":"1"}}""")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/findOne");
        request.Content = new StringContent("""{"dataSource":"C","database":"db","collection":"col"}""",
            System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var logs = GetLogsForTest();
        Assert.All(logs, l => Assert.Null(l.Content));
    }

    [Fact]
    public async Task SendAsync_Summarised_SkipsOtherOperation()
    {
        var options = CreateOptions();
        options.Verbosity = AtlasDataApiTrackingVerbosity.Summarised;
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/unknownOp");
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Empty(GetLogsForTest());
    }

    [Fact]
    public async Task SendAsync_Raw_UsesHttpMethod()
    {
        var options = CreateOptions();
        options.Verbosity = AtlasDataApiTrackingVerbosity.Raw;
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/find");
        request.Content = new StringContent("""{"dataSource":"C","database":"db","collection":"col"}""",
            System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var log = GetLogsForTest().First();
        Assert.IsType<HttpMethod>(log.Method.Value); // HttpMethod
    }

    // ── ExcludedOperations ──

    [Fact]
    public async Task SendAsync_ExcludedOperation_NotTracked()
    {
        var options = CreateOptions();
        options.ExcludedOperations = [AtlasDataApiOperation.Find];
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/find");
        request.Content = new StringContent("""{"dataSource":"C","database":"db","collection":"col"}""",
            System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Empty(GetLogsForTest());
    }

    // ── Excluded headers ──

    [Fact]
    public async Task SendAsync_FiltersExcludedHeaders()
    {
        var options = CreateOptions();
        options.Verbosity = AtlasDataApiTrackingVerbosity.Detailed;
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/find");
        request.Headers.Add("api-key", "secret-key-123");
        request.Headers.Add("X-Custom", "keep-me");
        request.Content = new StringContent("""{"dataSource":"C","database":"db","collection":"col"}""",
            System.Text.Encoding.UTF8, "application/json");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var log = GetLogsForTest().First();
        Assert.DoesNotContain(log.Headers, h => h.Key == "api-key");
        Assert.Contains(log.Headers, h => h.Key == "X-Custom");
    }

    // ── ITrackingComponent ──

    [Fact]
    public async Task InvocationCount_TracksCallCount()
    {
        var options = CreateOptions();
        var inner = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        using var handler = new AtlasDataApiTrackingMessageHandler(options, inner);
        using var client = new HttpClient(handler);

        Assert.False(handler.WasInvoked);
        Assert.Equal(0, handler.InvocationCount);

        var request1 = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/find");
        request1.Content = new StringContent("""{"dataSource":"C","database":"db","collection":"col"}""",
            System.Text.Encoding.UTF8, "application/json");
        await client.SendAsync(request1, TestContext.Current.CancellationToken);

        Assert.True(handler.WasInvoked);
        Assert.Equal(1, handler.InvocationCount);
    }

    [Fact]
    public void ComponentName_IncludesServiceName()
    {
        var options = new AtlasDataApiTrackingMessageHandlerOptions { ServiceName = "MyAtlas" };
        using var handler = new AtlasDataApiTrackingMessageHandler(options);

        Assert.Contains("MyAtlas", handler.ComponentName);
    }

    // ── BuildCleanUri ──

    [Fact]
    public void BuildCleanUri_DatabaseAndCollection()
    {
        var op = new AtlasDataApiOperationInfo(AtlasDataApiOperation.Find, "C", "myDb", "users");
        var uri = AtlasDataApiTrackingMessageHandler.BuildCleanUri(op);
        Assert.Equal(new Uri("atlas:///myDb/users"), uri);
    }

    [Fact]
    public void BuildCleanUri_DatabaseOnly()
    {
        var op = new AtlasDataApiOperationInfo(AtlasDataApiOperation.Find, "C", "myDb", null);
        var uri = AtlasDataApiTrackingMessageHandler.BuildCleanUri(op);
        Assert.Equal(new Uri("atlas:///myDb"), uri);
    }

    [Fact]
    public void BuildCleanUri_NoDatabaseOrCollection()
    {
        var op = new AtlasDataApiOperationInfo(AtlasDataApiOperation.Other, null, null, null);
        var uri = AtlasDataApiTrackingMessageHandler.BuildCleanUri(op);
        Assert.Equal(new Uri("atlas:///"), uri);
    }

    // ── Helpers ──

    private AtlasDataApiTrackingMessageHandlerOptions CreateOptions() =>
        new()
        {
            CurrentTestInfoFetcher = () => ("TestName", _testId)
        };

    private class FakeInnerHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeInnerHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }
}
