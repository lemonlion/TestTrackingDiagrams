using Kronikol;

namespace Example.Api.Tests.Component.Shared;

public static class IntegrationTestConfiguration
{
    public static bool IsIntegrationTestMode =>
        string.Equals(
            Environment.GetEnvironmentVariable("KRONIKOL_INTEGRATION_MODE")
            ?? Environment.GetEnvironmentVariable("TTD_INTEGRATION_MODE"),
            "true", StringComparison.OrdinalIgnoreCase);

    public static ReportConfigurationOptions GetReportConfigurationOptions()
    {
        var options = new ReportConfigurationOptions();

        if (TryGetEnv("KRONIKOL_SPECIFICATIONS_TITLE") is { } title)
            options.SpecificationsTitle = title;

        if (TryGetBool("KRONIKOL_SEPARATE_SETUP") is { } separateSetup)
            options.SeparateSetup = separateSetup;

        if (TryGetBool("KRONIKOL_HIGHLIGHT_SETUP") is { } highlightSetup)
            options.HighlightSetup = highlightSetup;

        if (TryGetBool("KRONIKOL_LAZY_LOAD_DIAGRAM_IMAGES") is { } lazyLoad)
            options.LazyLoadDiagramImages = lazyLoad;

        if (TryGetEnum<FocusEmphasis>("KRONIKOL_FOCUS_EMPHASIS") is { } emphasis)
            options.FocusEmphasis = emphasis;

        if (TryGetEnum<FocusDeEmphasis>("KRONIKOL_FOCUS_DE_EMPHASIS") is { } deEmphasis)
            options.FocusDeEmphasis = deEmphasis;

        if (TryGetCsv("KRONIKOL_EXCLUDED_HEADERS") is { } excludedHeaders)
            options.ExcludedHeaders = excludedHeaders;

        if (TryGetEnv("KRONIKOL_PLANTUML_SERVER_BASE_URL") is { } plantUmlServerBaseUrl)
            options.PlantUmlServerBaseUrl = plantUmlServerBaseUrl;

        if (TryGetEnum<PlantUmlRendering>("KRONIKOL_PLANTUML_RENDERING") is { } plantUmlRendering)
            options.PlantUmlRendering = plantUmlRendering;

        if (TryGetBool("KRONIKOL_INTERNAL_FLOW_TRACKING") is { } internalFlowTracking)
            options.InternalFlowTracking = internalFlowTracking;

        return options;
    }

    public static void ApplyDiagramFocus()
    {
        if (TryGetCsv("KRONIKOL_FOCUS_REQUEST_FIELDS") is { } requestFields)
            DiagramFocus.Request(requestFields);

        if (TryGetCsv("KRONIKOL_FOCUS_RESPONSE_FIELDS") is { } responseFields)
            DiagramFocus.Response(responseFields);
    }

    private static string? TryGetEnv(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : name.StartsWith("KRONIKOL_")
                ? Environment.GetEnvironmentVariable("TTD_" + name["KRONIKOL_".Length..]) is { Length: > 0 } fallback ? fallback : null
                : null;

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
