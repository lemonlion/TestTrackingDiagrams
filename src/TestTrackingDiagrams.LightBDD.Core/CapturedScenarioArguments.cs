using System.Collections.Concurrent;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Static store for raw test method arguments captured during LightBDD scenario execution.
/// Uses multiple key strategies to enable reliable lookup during report generation
/// when only formatted string representations are available.
/// </summary>
public static class CapturedScenarioArguments
{
    private static readonly ConcurrentDictionary<string, (string[] ParamNames, object?[] RawValues)> Store = new();

    /// <summary>
    /// Stores raw argument values with multiple key strategies for reliable lookup.
    /// Called from framework-specific capture attributes (e.g. BeforeAfterTestAttribute in xUnit3).
    /// </summary>
    public static void Capture(string[] paramNames, string[] formattedValues, object?[] rawValues)
    {
        // Store under both a full key (names+values) and a values-only key
        var fullKey = BuildKey(paramNames, formattedValues);
        var valuesKey = BuildValuesOnlyKey(formattedValues);
        var entry = (paramNames, rawValues);
        Store[fullKey] = entry;
        Store[valuesKey] = entry;
    }

    /// <summary>
    /// Attempts to retrieve captured raw arguments for a given key.
    /// Does NOT remove the entry (since we store under multiple keys).
    /// </summary>
    public static (string[] ParamNames, object?[] RawValues)? TryGet(string key)
    {
        return Store.TryGetValue(key, out var result) ? result : null;
    }

    /// <summary>
    /// Generates a lookup key from parameter names and their formatted (ToString) values.
    /// </summary>
    public static string BuildKey(string[] paramNames, string[] formattedValues)
    {
        var parts = new string[paramNames.Length + formattedValues.Length];
        for (var i = 0; i < paramNames.Length; i++)
        {
            parts[i * 2] = paramNames[i];
            parts[i * 2 + 1] = formattedValues[i];
        }
        return "full:" + string.Join("\0", parts);
    }

    /// <summary>
    /// Generates a values-only key from formatted values, avoiding parameter name mismatches.
    /// </summary>
    public static string BuildValuesOnlyKey(string[] formattedValues)
    {
        return "vals:" + string.Join("\0", formattedValues);
    }

    /// <summary>
    /// Clears all captured arguments (for test cleanup).
    /// </summary>
    public static void Clear() => Store.Clear();
}
