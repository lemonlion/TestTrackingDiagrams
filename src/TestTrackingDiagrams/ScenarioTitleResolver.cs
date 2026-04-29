using System.Text.RegularExpressions;

namespace TestTrackingDiagrams;

/// <summary>
/// Resolves human-readable scenario and feature titles from test method names and display names.
/// </summary>
public static partial class ScenarioTitleResolver
{
    /// <summary>
    /// Detects when a BDDfy scenario title has been set to the class name (e.g. via .BDDfy(nameof(ClassName)))
    /// and replaces it with a humanized version of the test method name.
    /// </summary>
    public static string ResolveScenarioTitle(string scenarioTitle, string? testClassSimpleName, string? testMethodName)
    {
        if (testClassSimpleName is null || testMethodName is null)
            return scenarioTitle;

        if (scenarioTitle != testClassSimpleName)
            return scenarioTitle;

        var humanized = SplitPascalCase(testMethodName);
        humanized = humanized.Replace("_", " ");
        humanized = MultipleSpacesRegex().Replace(humanized, " ").Trim();
        return char.ToUpper(humanized[0]) + humanized[1..].ToLowerInvariant();
    }

    /// <summary>
    /// Extracts the parameter portion from a test display name (e.g. xUnit Theory's
    /// <c>Ns.Class.Method(param1: "v1", param2: "v2")</c>) and appends it as <c>[param1: "v1", param2: "v2"]</c>.
    /// Returns the original title unchanged when there are no parameters.
    /// </summary>
    private const int MaxParameterLength = 200;

    public static string AppendTestParameters(string resolvedTitle, string? testDisplayName)
    {
        if (testDisplayName is null)
            return resolvedTitle;

        var parenIndex = testDisplayName.IndexOf('(');
        if (parenIndex < 0)
            return resolvedTitle;

        var paramContent = testDisplayName[(parenIndex + 1)..].TrimEnd(')');
        if (paramContent.Length == 0)
            return resolvedTitle;

        if (paramContent.Length > MaxParameterLength)
            paramContent = paramContent[..MaxParameterLength] + "...";

        return $"{resolvedTitle} [{paramContent}]";
    }

    /// <summary>
    /// Humanizes a test class simple name (e.g. PascalCase) into a Title Case feature name.
    /// </summary>
    public static string FormatFeatureName(string testClassSimpleName) => testClassSimpleName.Titleize();

    /// <summary>
    /// Parses a test display name (optionally fully-qualified), humanizes the method name,
    /// and appends any parameter values in brackets.
    /// <c>Ns.Class.MyTestMethod(p: "v")</c> → <c>My test method [p: "v"]</c>.
    /// </summary>
    public static string FormatScenarioDisplayName(string testDisplayName)
    {
        string methodPath;
        string? parameters = null;

        var parenIndex = testDisplayName.IndexOf('(');
        if (parenIndex >= 0)
        {
            methodPath = testDisplayName[..parenIndex];
            var paramContent = testDisplayName[(parenIndex + 1)..].TrimEnd(')');
            if (paramContent.Length > 0)
            {
                parameters = paramContent.Length > MaxParameterLength
                    ? paramContent[..MaxParameterLength] + "..."
                    : paramContent;
            }
        }
        else
        {
            methodPath = testDisplayName;
        }

        var lastDotIndex = methodPath.LastIndexOf('.');
        var methodName = lastDotIndex >= 0 ? methodPath[(lastDotIndex + 1)..] : methodPath;

        var humanized = SplitPascalCase(methodName);
        humanized = humanized.Replace("_", " ");
        humanized = MultipleSpacesRegex().Replace(humanized, " ").Trim();
        humanized = char.ToUpper(humanized[0]) + humanized[1..].ToLowerInvariant();

        return parameters is not null ? $"{humanized} [{parameters}]" : humanized;
    }

    private static string SplitPascalCase(string input)
    {
        var result = LowerToUpperRegex().Replace(input, "$1 $2");
        result = UpperSequenceRegex().Replace(result, "$1 $2");
        return result;
    }

    [GeneratedRegex(@"(\p{Ll})(\p{Lu})")]
    private static partial Regex LowerToUpperRegex();

    [GeneratedRegex(@"(\p{Lu}+)(\p{Lu}\p{Ll})")]
    private static partial Regex UpperSequenceRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();
}
