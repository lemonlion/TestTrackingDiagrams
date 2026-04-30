using System.Collections.Concurrent;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Tracks HTTP client names that were provided via <c>clientName</c> parameter
/// but did not match any key in <see cref="TestTrackingMessageHandlerOptions.ClientNamesToServiceNames"/>.
/// The diagnostic report reads this to surface configuration mismatches.
/// </summary>
public static class UnmatchedClientNameRegistry
{
    private static ConcurrentDictionary<string, int> _names = new();

    /// <summary>
    /// Records a client name that failed to match any <c>ClientNamesToServiceNames</c> key.
    /// </summary>
    public static void Record(string clientName)
    {
        _names.AddOrUpdate(clientName, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Returns all recorded unmatched client names with their request counts.
    /// </summary>
    public static IReadOnlyList<(string ClientName, int RequestCount)> GetRecordedNames()
        => _names.Select(kvp => (kvp.Key, kvp.Value)).OrderByDescending(x => x.Value).ToList();

    /// <summary>
    /// Clears all recorded names.
    /// </summary>
    public static void Clear()
    {
        Interlocked.Exchange(ref _names, new());
    }
}
