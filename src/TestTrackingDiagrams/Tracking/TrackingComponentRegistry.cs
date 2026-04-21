using System.Collections.Concurrent;
using System.Text;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Central registry for all tracking components. Each handler/interceptor auto-registers
/// itself on construction so that <see cref="ValidateAllComponentsWereInvoked"/> can detect
/// components that were wired up but never actually processed any traffic — a strong
/// indicator of misconfiguration.
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
    /// Throws <see cref="InvalidOperationException"/> if any registered component was never invoked.
    /// Call this in test teardown to catch misconfiguration early.
    /// </summary>
    public static void ValidateAllComponentsWereInvoked()
    {
        var unused = GetUnusedComponents();
        if (unused.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine("The following tracking components were registered but never invoked:");
        sb.AppendLine();
        foreach (var c in unused)
        {
            sb.AppendLine($"  • {c.ComponentName}");
        }
        sb.AppendLine();
        sb.AppendLine("This usually means the component was added to the wrong pipeline, options, or service.");
        sb.AppendLine();
        sb.AppendLine("Common causes:");
        sb.AppendLine("  - EF Core: The interceptor was added to DbContextOptions<TDerived> but the");
        sb.AppendLine("    DbContext constructor accepts DbContextOptions<TBase> (e.g. Duende IdentityServer's");
        sb.AppendLine("    ConfigurationDbContext). Fix: add the interceptor to the base class's options.");
        sb.AppendLine("  - HTTP: The handler was added to an HttpClient that isn't being used by the target service.");
        sb.AppendLine("  - Redis: An untracked IDatabase instance is being used instead of the tracked one.");

        throw new InvalidOperationException(sb.ToString());
    }

    /// <summary>
    /// Removes all registered components. Call in test setup alongside
    /// <see cref="RequestResponseLogger.Clear"/>.
    /// </summary>
    public static void Clear()
    {
        while (Components.TryTake(out _)) { }
    }
}
