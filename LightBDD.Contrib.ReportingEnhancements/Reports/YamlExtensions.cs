namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public static class YamlExtensions
{
    public static string SanitiseForYml(this string value)
    {
        return value.Replace("[", "<").Replace("]", ">").Replace(": ", " = ");
    }
}