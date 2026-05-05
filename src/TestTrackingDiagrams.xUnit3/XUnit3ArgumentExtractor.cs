using System.Reflection;
using Xunit.v3;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// Shared helper for extracting raw test method arguments from xUnit v3's test context types.
/// Used by TestTrackingDiagrams.xUnit3, TestTrackingDiagrams.BDDfy.xUnit3,
/// and TestTrackingDiagrams.LightBDD.xUnit3 to avoid duplicating the extraction pattern.
/// </summary>
public static class XUnit3ArgumentExtractor
{
    /// <summary>
    /// Extracts raw test method arguments and their parameter names from an xUnit v3 test.
    /// Handles XunitTest.TestMethodArguments with XunitTestCase fallback.
    /// </summary>
    public static (object?[]? Args, string[]? ParamNames) Extract(IXunitTest? test)
    {
        try
        {
            object[]? args = null;

            if (test is XunitTest xunitTest)
                args = xunitTest.TestMethodArguments;

            if (args is null or { Length: 0 } && test?.TestCase is XunitTestCase testCase)
                args = testCase.TestMethodArguments;

            if (args is not { Length: > 0 })
                return (null, null);

            // Get parameter names from the test method
            MethodInfo? method = null;
            if (test is XunitTest xt)
                method = xt.TestMethod?.Method as MethodInfo;
            if (method is null && test?.TestCase is XunitTestCase tc)
                method = tc.TestMethod?.Method as MethodInfo;

            if (method is null)
                return (args.ToArray(), null);

            var parameters = method.GetParameters();
            if (parameters.Length == 0 || parameters.Length != args.Length)
                return (args.ToArray(), null);

            var paramNames = new string[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                paramNames[i] = parameters[i].Name ?? $"arg{i}";

            return (args.ToArray(), paramNames);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Extracts raw test method arguments from an xUnit v3 test context (ITestContext).
    /// Convenience overload that accesses the Test property.
    /// </summary>
    public static (object?[]? Args, string[]? ParamNames) Extract(Xunit.ITestContext? testContext)
    {
        return Extract(testContext?.Test as IXunitTest);
    }
}
