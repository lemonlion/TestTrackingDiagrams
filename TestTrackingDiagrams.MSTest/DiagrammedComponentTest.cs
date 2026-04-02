using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestTrackingDiagrams.MSTest;

public abstract class DiagrammedComponentTest
{
    private static readonly AsyncLocal<TestContext?> CurrentContext = new();

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void TestTrackingInitialize()
    {
        CurrentContext.Value = TestContext;
    }

    [TestCleanup]
    public void TestTrackingCleanup()
    {
        var type = GetType();
        var endpoint = type.GetCustomAttribute<EndpointAttribute>()?.Endpoint;
        var methodInfo = type.GetMethod(TestContext.TestName!);
        var isHappyPath = methodInfo?.GetCustomAttribute<HappyPathAttribute>() is not null;

        DiagrammedTestRun.TestContexts.Enqueue(new MSTestScenarioInfo
        {
            TestClassSimpleName = type.Name,
            TestMethodName = TestContext.TestName!,
            TestId = $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}",
            Outcome = TestContext.CurrentTestOutcome,
            ErrorMessage = TestContext.CurrentTestOutcome == UnitTestOutcome.Failed
                ? "Test failed — see ErrorStackTrace for details"
                : null,
            Endpoint = endpoint,
            IsHappyPath = isHappyPath
        });
    }

    internal static TestContext? GetCurrentTestContext() => CurrentContext.Value;
}
