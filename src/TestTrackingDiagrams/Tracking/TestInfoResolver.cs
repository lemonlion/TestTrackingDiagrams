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
