using System.Text.RegularExpressions;

namespace TestTrackingDiagrams;

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
