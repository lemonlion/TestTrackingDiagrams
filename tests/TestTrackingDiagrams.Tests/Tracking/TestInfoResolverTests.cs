using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class TestInfoResolverTests
{
    [Fact]
    public void Returns_null_when_both_accessor_and_delegate_are_null()
    {
        var result = TestInfoResolver.Resolve(null, (Func<(string, string)>?)null);

        Assert.Null(result);
    }

    [Fact]
    public void Returns_delegate_result_when_no_http_context()
    {
        var result = TestInfoResolver.Resolve(null, () => ("TestName", "test-id-123"));

        Assert.NotNull(result);
        Assert.Equal("TestName", result.Value.Name);
        Assert.Equal("test-id-123", result.Value.Id);
    }

    [Fact]
    public void Returns_null_when_delegate_throws()
    {
        Func<(string, string)> throwingFetcher = () => throw new InvalidOperationException("No scenario context");

        var result = TestInfoResolver.Resolve(null, throwingFetcher);

        Assert.Null(result);
    }

    [Fact]
    public void Returns_http_headers_when_present()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TestTrackingHttpHeaders.CurrentTestNameHeader] = "HeaderTest";
        httpContext.Request.Headers[TestTrackingHttpHeaders.CurrentTestIdHeader] = "header-id-456";
        var accessor = new FakeHttpContextAccessor(httpContext);

        var result = TestInfoResolver.Resolve(accessor, () => ("DelegateTest", "delegate-id"));

        Assert.NotNull(result);
        Assert.Equal("HeaderTest", result.Value.Name);
        Assert.Equal("header-id-456", result.Value.Id);
    }

    [Fact]
    public void Prefers_http_headers_over_delegate()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TestTrackingHttpHeaders.CurrentTestNameHeader] = "FromHeaders";
        httpContext.Request.Headers[TestTrackingHttpHeaders.CurrentTestIdHeader] = "from-headers";
        var accessor = new FakeHttpContextAccessor(httpContext);

        var result = TestInfoResolver.Resolve(accessor, () => ("FromDelegate", "from-delegate"));

        Assert.NotNull(result);
        Assert.Equal("FromHeaders", result.Value.Name);
        Assert.Equal("from-headers", result.Value.Id);
    }

    [Fact]
    public void Falls_back_to_delegate_when_headers_missing()
    {
        var httpContext = new DefaultHttpContext(); // no test headers set
        var accessor = new FakeHttpContextAccessor(httpContext);

        var result = TestInfoResolver.Resolve(accessor, () => ("FallbackTest", "fallback-id"));

        Assert.NotNull(result);
        Assert.Equal("FallbackTest", result.Value.Name);
        Assert.Equal("fallback-id", result.Value.Id);
    }

    [Fact]
    public void Falls_back_to_delegate_when_only_name_header_present()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TestTrackingHttpHeaders.CurrentTestNameHeader] = "OnlyName";
        var accessor = new FakeHttpContextAccessor(httpContext);

        var result = TestInfoResolver.Resolve(accessor, () => ("Delegate", "delegate-id"));

        Assert.NotNull(result);
        Assert.Equal("Delegate", result.Value.Name);
        Assert.Equal("delegate-id", result.Value.Id);
    }

    [Fact]
    public void Falls_back_to_delegate_when_http_context_is_null()
    {
        var accessor = new FakeHttpContextAccessor(null);

        var result = TestInfoResolver.Resolve(accessor, () => ("DelegateOnly", "delegate-only"));

        Assert.NotNull(result);
        Assert.Equal("DelegateOnly", result.Value.Name);
        Assert.Equal("delegate-only", result.Value.Id);
    }

    [Fact]
    public void Returns_null_when_headers_missing_and_delegate_is_null()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new FakeHttpContextAccessor(httpContext);

        var result = TestInfoResolver.Resolve(accessor, (Func<(string, string)>?)null);

        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_when_headers_missing_and_delegate_throws()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new FakeHttpContextAccessor(httpContext);
        Func<(string, string)> throwingFetcher = () => throw new InvalidOperationException();

        var result = TestInfoResolver.Resolve(accessor, throwingFetcher);

        Assert.Null(result);
    }

    [Fact]
    public void Nullable_overload_returns_delegate_result()
    {
        Func<(string, string)?> fetcher = () => ("NullableTest", "nullable-id");

        var result = TestInfoResolver.Resolve(null, fetcher);

        Assert.NotNull(result);
        Assert.Equal("NullableTest", result.Value.Name);
        Assert.Equal("nullable-id", result.Value.Id);
    }

    [Fact]
    public void Nullable_overload_returns_null_when_delegate_returns_null()
    {
        Func<(string, string)?> fetcher = () => null;

        var result = TestInfoResolver.Resolve(null, fetcher);

        Assert.Null(result);
    }

    [Fact]
    public void Nullable_overload_prefers_http_headers()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TestTrackingHttpHeaders.CurrentTestNameHeader] = "HeaderWins";
        httpContext.Request.Headers[TestTrackingHttpHeaders.CurrentTestIdHeader] = "header-wins";
        var accessor = new FakeHttpContextAccessor(httpContext);
        Func<(string, string)?> fetcher = () => ("DelegateLoses", "delegate-loses");

        var result = TestInfoResolver.Resolve(accessor, fetcher);

        Assert.NotNull(result);
        Assert.Equal("HeaderWins", result.Value.Name);
        Assert.Equal("header-wins", result.Value.Id);
    }

    private class FakeHttpContextAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }
}
