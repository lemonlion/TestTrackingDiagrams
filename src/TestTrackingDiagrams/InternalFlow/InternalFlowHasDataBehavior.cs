namespace TestTrackingDiagrams;

/// <summary>
/// Controls how the internal-flow visualization link is displayed for scenarios that have span data.
/// </summary>
public enum InternalFlowHasDataBehavior
{
    /// <summary>Shows the link only when the user hovers over the scenario.</summary>
    ShowLinkOnHover,

    /// <summary>Shows the link permanently alongside the scenario.</summary>
    ShowLink
}
