namespace TestTrackingDiagrams;

/// <summary>
/// Controls which <see cref="System.Diagnostics.Activity"/> spans are included in internal-flow diagrams.
/// </summary>
public enum InternalFlowSpanGranularity
{
    /// <summary>Include only spans from well-known auto-instrumentation sources (ASP.NET Core, EF Core, etc.).</summary>
    AutoInstrumentation,

    /// <summary>Include only spans from manually created <see cref="System.Diagnostics.ActivitySource"/>s.</summary>
    Manual,

    /// <summary>Include all captured spans regardless of source.</summary>
    Full
}
