using System.Collections.Concurrent;

namespace TestTrackingDiagrams.xUnit2;

/// <summary>
/// Provides the current test's identity via AsyncLocal for TestTrackingDiagrams.
/// In xUnit v2, there is no <c>TestContext.Current</c>, so this class uses
/// <see cref="AsyncLocal{T}"/> to store the current test's name and ID.
/// The <see cref="TestTrackingAttribute"/> sets and clears this context
/// before and after each test.
/// </summary>
public static class XUnit2TestTrackingContext
{
    private static readonly AsyncLocal<(string Name, string Id)?> CurrentTest = new();

    internal static readonly ConcurrentDictionary<string, ScenarioInfo> CollectedScenarios = new();

    public static (string Name, string Id) GetCurrentTestInfo() =>
        CurrentTest.Value ?? ("Unknown Test", Guid.NewGuid().ToString());

    internal static void SetCurrentTest(string name, string id) =>
        CurrentTest.Value = (name, id);

    internal static void ClearCurrentTest() =>
        CurrentTest.Value = null;

    internal static void UpdateResult(string testId, Reports.ScenarioResult result, string? errorMessage = null, string? errorStackTrace = null)
    {
        if (CollectedScenarios.TryGetValue(testId, out var info))
        {
            info.Result = result;
            info.ErrorMessage = errorMessage;
            info.ErrorStackTrace = errorStackTrace;
        }
    }

    internal static ScenarioInfo[] GetAllScenarios() => CollectedScenarios.Values.ToArray();
}
