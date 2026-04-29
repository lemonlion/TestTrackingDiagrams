namespace TestTrackingDiagrams;

/// <summary>
/// The image format used for rendered PlantUML diagrams.
/// </summary>
public enum PlantUmlImageFormat
{
    /// <summary>PNG image, referenced via URL or file path.</summary>
    Png,

    /// <summary>SVG image, referenced via URL or file path.</summary>
    Svg,

    /// <summary>PNG image, embedded inline as a Base64 data URI.</summary>
    Base64Png,

    /// <summary>SVG image, embedded inline as a Base64 data URI.</summary>
    Base64Svg
}
