using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Constants;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Resolves test identity using a dual-resolution strategy: first tries HTTP context
/// request headers (for code running inside the SUT's request pipeline), then falls
/// back to a delegate (for code running on the test thread).
/// </summary>
public static class TestInfoResolver
{
    /// <summary>
    /// Attempts to resolve the current test name and ID.
    /// First checks HTTP request headers propagated by <see cref="TestTrackingMessageHandler"/>,
    /// then falls back to the delegate (e.g. from a test framework's execution context).
    /// </summary>
    public static (string Name, string Id)? Resolve(
        IHttpContextAccessor? httpContextAccessor,
        Func<(string Name, string Id)>? currentTestInfoFetcher)
    {
        if (TryResolveFromHttpContext(httpContextAccessor, out var result))
            return result;

        try
        {
            return currentTestInfoFetcher?.Invoke();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Overload for delegates that return a nullable tuple (e.g. Dapper extension).
    /// </summary>
    public static (string Name, string Id)? Resolve(
        IHttpContextAccessor? httpContextAccessor,
        Func<(string Name, string Id)?>? currentTestInfoFetcher)
    {
        if (TryResolveFromHttpContext(httpContextAccessor, out var result))
            return result;

        try
        {
            return currentTestInfoFetcher?.Invoke();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a <c>Func&lt;(string Name, string Id)&gt;</c> that tries to resolve test identity
    /// from HTTP request headers first, falling back to the provided delegate.
    /// <para>
    /// Use this to eliminate the repetitive httpContext+fallback boilerplate when setting
    /// <c>CurrentTestInfoFetcher</c> on tracking options classes.
    /// </para>
    /// </summary>
    /// <param name="httpContextAccessor">
    /// Optional accessor for the current HTTP context. When available, the returned delegate
    /// reads <see cref="TestTrackingHttpHeaders.CurrentTestNameHeader"/> and
    /// <see cref="TestTrackingHttpHeaders.CurrentTestIdHeader"/> from request headers.
    /// </param>
    /// <param name="fallback">
    /// Delegate invoked when the HTTP context is unavailable or headers are missing
    /// (e.g. test framework context like <c>TestContext.Current</c>).
    /// </param>
    public static Func<(string Name, string Id)> CreateHttpFallbackFetcher(
        IHttpContextAccessor? httpContextAccessor,
        Func<(string Name, string Id)> fallback)
    {
        return () =>
        {
            if (TryResolveFromHttpContext(httpContextAccessor, out var result))
                return result;

            return fallback();
        };
    }

    private static bool TryResolveFromHttpContext(
        IHttpContextAccessor? httpContextAccessor,
        out (string Name, string Id) result)
    {
        result = default;

        try
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext is not null &&
                httpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out var testName) &&
                httpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out var testId) &&
                testName.Count > 0 && testId.Count > 0)
            {
                result = (testName[0]!, testId[0]!);
                return true;
            }
        }
        catch
        {
            // HttpContext access can fail in edge cases — fall through to delegate
        }

        return false;
    }
}
