namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Extension methods for sanitising strings for safe inclusion in YAML output.
/// </summary>
public static class YamlExtensions
{
    public static string SanitiseForYml(this string value)
    {
        return value
            .Replace("[", "<")
            .Replace("]", ">")
            .Replace(": ", " = ")
            .Replace("#", "(hash)")
            .Replace("&", "(and)")
            .Replace("*", "(star)")
            .Replace("{", "(")
            .Replace("}", ")")
            .Replace("!", "(bang)")
            .Replace("%", "(pct)")
            .Replace("@", "(at)")
            .Replace("`", "'")
            .Replace("|", "(pipe)");
    }
}