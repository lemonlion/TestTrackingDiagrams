namespace TestTrackingDiagrams.Constants;

/// <summary>
/// Default values shared across all tracking options classes.
/// </summary>
public static class TrackingDefaults
{
    /// <summary>
    /// Default display name for the calling service in diagrams when no explicit name is configured.
    /// Used as the default value of <c>CallerName</c> on all tracking options classes.
    /// </summary>
    public const string CallerName = "Caller";

    /// <summary>
    /// CDN base URL for the PlantUML JavaScript renderer used in HTML reports.
    /// </summary>
    public const string PlantUmlJsCdnBase = "https://cdn.jsdelivr.net/gh/lemonlion/plantuml-js-plantuml_limit_size_98304@v1.2026.3beta6-patched";
}
