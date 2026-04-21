using System.Collections.Concurrent;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Central registry for all tracking components. Each handler/interceptor auto-registers
/// itself on construction so that the diagnostic report can detect components that were
/// wired up but never actually processed any traffic — a strong indicator of misconfiguration.
/// </summary>
public static class TrackingComponentRegistry
{
    private static readonly ConcurrentBag<ITrackingComponent> Components = [];

    /// <summary>
    /// Called by tracking component constructors to register themselves.
    /// </summary>
    public static void Register(ITrackingComponent component) => Components.Add(component);

    /// <summary>
    /// Returns all components that were registered but have not processed any traffic.
    /// </summary>
    public static IReadOnlyList<ITrackingComponent> GetUnusedComponents()
        => Components.Where(c => !c.WasInvoked).ToList();

    /// <summary>
    /// Returns all registered tracking components.
    /// </summary>
    public static IReadOnlyList<ITrackingComponent> GetRegisteredComponents()
        => [.. Components];

    /// <summary>
    /// Removes all registered components. Call in test setup alongside
    /// <see cref="RequestResponseLogger.Clear"/>.
    /// </summary>
    public static void Clear()
    {
        while (Components.TryTake(out _)) { }
    }
}
