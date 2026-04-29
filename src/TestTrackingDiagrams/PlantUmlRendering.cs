namespace TestTrackingDiagrams;

/// <summary>
/// Determines how PlantUML source is rendered into diagram images.
/// </summary>
public enum PlantUmlRendering
{
    /// <summary>Renders via HTTP requests to a PlantUML server (e.g. plantuml.com).</summary>
    Server,

    /// <summary>Renders client-side in the browser using a PlantUML JavaScript/WASM encoder. Default.</summary>
    BrowserJs,

    /// <summary>Renders locally at report generation time using a user-supplied delegate.</summary>
    Local,

    /// <summary>Renders locally at report generation time using a Node.js PlantUML CLI.</summary>
    NodeJs
}
