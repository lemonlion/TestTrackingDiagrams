using System.Text.RegularExpressions;
using Humanizer;

namespace TestTrackingDiagrams.xUnit2;

public static partial class DisplayNameFormatter
{
    public static string FormatFeatureName(string testClassSimpleName)
    {
        return testClassSimpleName.Humanize(LetterCasing.Title);
    }

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
                parameters = paramContent;
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
        // Insert space between lowercase and uppercase: "givenRequest" → "given Request"
        var result = LowerToUpperRegex().Replace(input, "$1 $2");
        // Insert space between uppercase sequence and uppercase+lowercase: "ABCDef" → "ABC Def"
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
