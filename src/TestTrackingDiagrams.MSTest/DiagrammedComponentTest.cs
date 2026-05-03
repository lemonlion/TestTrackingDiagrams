using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.MSTest;

/// <summary>
/// Abstract base class for MSTest component tests that integrates with the test tracking diagram system to capture test execution context and timing.
/// </summary>
public abstract class DiagrammedComponentTest
{
    private static readonly AsyncLocal<TestContext?> CurrentContext = new();
    private Stopwatch? _stopwatch;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void TestTrackingInitialize()
    {
        // Enable Track.That() assertions to resolve the current test ID.
        Track.TestIdResolver ??= () =>
        {
            var ctx = GetCurrentTestContext();
            return ctx is not null ? $"{ctx.FullyQualifiedTestClassName}.{ctx.TestName}" : null;
        };
        _stopwatch = Stopwatch.StartNew();
        CurrentContext.Value = TestContext;
    }

    [TestCleanup]
    public void TestTrackingCleanup()
    {
        _stopwatch?.Stop();
        var type = GetType();
        var endpoint = type.GetCustomAttribute<EndpointAttribute>()?.Endpoint;
        var methodInfo = type.GetMethod(TestContext.TestName!);
        var isHappyPath = methodInfo?.GetCustomAttribute<HappyPathAttribute>() is not null;
        var parameterNames = methodInfo?.GetParameters().Select(p => p.Name).ToArray();

        DiagrammedTestRun.TestContexts.Enqueue(new MSTestScenarioInfo
        {
            TestClassSimpleName = type.Name,
            TestMethodName = TestContext.TestName!,
            TestDisplayName = TestContext.TestDisplayName,
            TestId = $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}",
            Outcome = TestContext.CurrentTestOutcome,
            ErrorMessage = TestContext.CurrentTestOutcome == UnitTestOutcome.Failed
                ? "Test failed — see ErrorStackTrace for details"
                : null,
            Endpoint = endpoint,
            IsHappyPath = isHappyPath,
            Duration = _stopwatch?.Elapsed,
            ParameterNames = parameterNames
        });
    }

    internal static TestContext? GetCurrentTestContext() => CurrentContext.Value;
}