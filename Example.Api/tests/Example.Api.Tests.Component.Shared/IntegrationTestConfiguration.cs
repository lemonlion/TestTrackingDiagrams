using TestTrackingDiagrams;

namespace Example.Api.Tests.Component.Shared;

public static class IntegrationTestConfiguration
{
    public static bool IsIntegrationTestMode =>
        string.Equals(Environment.GetEnvironmentVariable("TTD_INTEGRATION_MODE"), "true", StringComparison.OrdinalIgnoreCase);

    public static ReportConfigurationOptions GetReportConfigurationOptions()
    {
        var options = new ReportConfigurationOptions();

        if (TryGetEnv("TTD_SPECIFICATIONS_TITLE") is { } title)
            options.SpecificationsTitle = title;

        if (TryGetBool("TTD_SEPARATE_SETUP") is { } separateSetup)
            options.SeparateSetup = separateSetup;

        if (TryGetBool("TTD_HIGHLIGHT_SETUP") is { } highlightSetup)
            options.HighlightSetup = highlightSetup;

        if (TryGetBool("TTD_LAZY_LOAD_DIAGRAM_IMAGES") is { } lazyLoad)
            options.LazyLoadDiagramImages = lazyLoad;

        if (TryGetEnum<FocusEmphasis>("TTD_FOCUS_EMPHASIS") is { } emphasis)
            options.FocusEmphasis = emphasis;

        if (TryGetEnum<FocusDeEmphasis>("TTD_FOCUS_DE_EMPHASIS") is { } deEmphasis)
            options.FocusDeEmphasis = deEmphasis;

        if (TryGetCsv("TTD_EXCLUDED_HEADERS") is { } excludedHeaders)
            options.ExcludedHeaders = excludedHeaders;

        return options;
    }

    public static void ApplyDiagramFocus()
    {
        if (TryGetCsv("TTD_FOCUS_REQUEST_FIELDS") is { } requestFields)
            DiagramFocus.Request(requestFields);

        if (TryGetCsv("TTD_FOCUS_RESPONSE_FIELDS") is { } responseFields)
            DiagramFocus.Response(responseFields);
    }

    private static string? TryGetEnv(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    private static bool? TryGetBool(string name) =>
        TryGetEnv(name) is { } value && bool.TryParse(value, out var result) ? result : null;

    private static string[]? TryGetCsv(string name) =>
        TryGetEnv(name)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static TEnum? TryGetEnum<TEnum>(string name) where TEnum : struct, Enum
    {
        var csv = TryGetCsv(name);
        if (csv is null) return null;

        TEnum result = default;
        foreach (var item in csv)
        {
            if (Enum.TryParse<TEnum>(item, ignoreCase: true, out var parsed))
                result = (TEnum)(object)((int)(object)result | (int)(object)parsed);
        }

        return result;
    }
}
