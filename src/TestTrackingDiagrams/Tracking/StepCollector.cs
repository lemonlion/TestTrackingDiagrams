using System.Collections.Concurrent;
using System.Diagnostics;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Collects BDD-style steps (Given/When/Then) per test, keyed by test ID.
/// Supports nested steps (sub-steps), keyword sequencing (Given/And/And),
/// parameter capture, and assertion sub-step integration.
/// Thread-safe via ConcurrentDictionary.
/// </summary>
public static class StepCollector
{
    private static readonly ConcurrentDictionary<string, TestStepState> States = new();

    /// <summary>
    /// Configuration options for step tracking behavior.
    /// Set in test base class constructor or DI initialization.
    /// </summary>
    public static StepTrackingOptions Options { get; set; } = new();

    /// <summary>
    /// Starts a new step, resolving the test ID from the ambient context.
    /// Called by the IL weaver — consumers don't need to supply the test ID.
    /// </summary>
    public static void StartStep(string? keyword, string text, string[]? paramNames, object?[]? paramValues)
    {
        StartStep(ResolveTestId(), keyword, text, paramNames, paramValues);
    }

    /// <summary>
    /// Completes the current active step, resolving the test ID from the ambient context.
    /// Called by the IL weaver.
    /// </summary>
    public static void CompleteStep(bool passed, string? errorMessage = null)
    {
        CompleteStep(ResolveTestId(), passed, errorMessage);
    }

    /// <summary>
    /// Starts a new step for the given test. If there's already an active step,
    /// the new step becomes a sub-step of the current one.
    /// </summary>
    public static void StartStep(string? testId, string? keyword, string text, string[]? paramNames, object?[]? paramValues)
    {
        if (testId is null)
            return;

        var state = States.GetOrAdd(testId, _ => new TestStepState());

        var step = new CollectedStep
        {
            OriginalKeyword = keyword,
            Text = text,
            StartTime = Stopwatch.GetTimestamp(),
            Parameters = BuildParameters(paramNames, paramValues)
        };

        lock (state)
        {
            // Resolve effective keyword (Given/And/And sequencing)
            step.EffectiveKeyword = ResolveKeyword(state, keyword);

            state.StepStack.Push(step);
        }

        // Phase transitions: set ambient test phase based on keyword
        if (Options.WhenTriggersAction && keyword is not null)
        {
            switch (keyword)
            {
                case "Given":
                    TestPhaseContext.Current = TestPhase.Setup;
                    break;
                case "When":
                case "Then":
                    TestPhaseContext.Current = TestPhase.Action;
                    break;
            }
        }
    }

    /// <summary>
    /// Completes the current active step for the given test.
    /// </summary>
    public static void CompleteStep(string? testId, bool passed, string? errorMessage = null)
    {
        if (testId is null)
            return;

        if (!States.TryGetValue(testId, out var state))
            return;

        CollectedStep? completed;
        lock (state)
        {
            if (state.StepStack.Count == 0)
                return;

            completed = state.StepStack.Pop();
            completed.EndTime = Stopwatch.GetTimestamp();
            completed.Passed = passed;
            completed.ErrorMessage = errorMessage;

            if (state.StepStack.Count > 0)
            {
                // Nested: add as sub-step of parent
                state.StepStack.Peek().SubSteps.Add(completed);
            }
            else
            {
                // Top-level step
                state.CompletedSteps.Add(completed);
            }
        }
    }

    /// <summary>
    /// Adds an assertion as a sub-step of the currently active step.
    /// </summary>
    public static void AddAssertionSubStep(string? testId, string expression, bool passed)
    {
        if (testId is null)
            return;

        if (!States.TryGetValue(testId, out var state))
            return;

        lock (state)
        {
            if (state.StepStack.Count == 0)
                return;

            var symbol = passed ? "\u2713" : "\u2717";
            var subStep = new CollectedStep
            {
                OriginalKeyword = null,
                EffectiveKeyword = null,
                Text = $"{symbol} {expression}",
                StartTime = Stopwatch.GetTimestamp(),
                EndTime = Stopwatch.GetTimestamp(),
                Passed = passed
            };

            state.StepStack.Peek().SubSteps.Add(subStep);
        }
    }

    /// <summary>
    /// Returns whether there's an active (in-progress) step for the given test.
    /// </summary>
    public static bool HasActiveStep(string? testId)
    {
        if (testId is null)
            return false;

        if (!States.TryGetValue(testId, out var state))
            return false;

        lock (state)
        {
            return state.StepStack.Count > 0;
        }
    }

    /// <summary>
    /// Returns all collected steps for a test as ScenarioStep array.
    /// </summary>
    public static ScenarioStep[] GetSteps(string? testId)
    {
        if (testId is null)
            return [];

        if (!States.TryGetValue(testId, out var state))
            return [];

        lock (state)
        {
            return state.CompletedSteps.Select(ToScenarioStep).ToArray();
        }
    }

    /// <summary>
    /// Clears all collected steps for a test.
    /// </summary>
    public static void ClearSteps(string? testId)
    {
        if (testId is null)
            return;

        States.TryRemove(testId, out _);
    }

    private static string? ResolveTestId()
    {
        try
        {
            var resolved = Track.TestIdResolver?.Invoke();
            if (resolved is not null)
                return resolved;
        }
        catch
        {
            // Resolver threw — fall through
        }

        return TestIdentityScope.Current?.Id;
    }

    private static string? ResolveKeyword(TestStepState state, string? keyword)
    {
        if (keyword is null)
            return null;

        // Only apply sequencing at the current nesting level (top-level steps)
        if (state.StepStack.Count > 0)
            return keyword; // Sub-steps keep their original keyword

        if (state.LastKeywordCategory == keyword)
        {
            return "And";
        }

        state.LastKeywordCategory = keyword;
        return keyword;
    }

    private static StepParameter[]? BuildParameters(string[]? paramNames, object?[]? paramValues)
    {
        if (paramNames is null || paramValues is null || paramNames.Length == 0)
            return null;

        var count = Math.Min(paramNames.Length, paramValues.Length);
        var result = new StepParameter[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = new StepParameter
            {
                Name = paramNames[i],
                Kind = StepParameterKind.Inline,
                InlineValue = new InlineParameterValue(
                    Value: paramValues[i]?.ToString() ?? "null",
                    Expectation: null,
                    Status: VerificationStatus.NotApplicable)
            };
        }

        return result;
    }

    private static ScenarioStep ToScenarioStep(CollectedStep step)
    {
        var duration = step.EndTime > 0
            ? TimeSpan.FromTicks((long)((step.EndTime - step.StartTime) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)))
            : (TimeSpan?)null;

        return new ScenarioStep
        {
            Keyword = step.EffectiveKeyword,
            Text = step.Text,
            Status = step.Passed ? ExecutionResult.Passed : ExecutionResult.Failed,
            Duration = duration,
            Parameters = step.Parameters,
            SubSteps = step.SubSteps.Count > 0
                ? step.SubSteps.Select(ToScenarioStep).ToArray()
                : null
        };
    }

    private class TestStepState
    {
        public Stack<CollectedStep> StepStack { get; } = new();
        public List<CollectedStep> CompletedSteps { get; } = new();
        public string? LastKeywordCategory { get; set; }
    }

    private class CollectedStep
    {
        public string? OriginalKeyword { get; set; }
        public string? EffectiveKeyword { get; set; }
        public string Text { get; set; } = "";
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public bool Passed { get; set; }
        public string? ErrorMessage { get; set; }
        public StepParameter[]? Parameters { get; set; }
        public List<CollectedStep> SubSteps { get; } = new();
    }
}
