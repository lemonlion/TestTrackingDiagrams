namespace Kronikol.Tracking;

/// <summary>
/// Generic processing correlation wrapper that establishes <see cref="TestIdentityScope"/>
/// before invoking a processing delegate, by resolving the work-item key from
/// <see cref="TestCorrelationStore"/>.
/// <para>
/// Use this for custom background processors not covered by built-in extension wrappers
/// (e.g. Hangfire jobs, hosted service loops, custom channel readers).
/// </para>
/// </summary>
public static class ProcessingCorrelation
{
    /// <summary>
    /// Wraps an async processing delegate so each item is attributed to the correct test.
    /// </summary>
    /// <typeparam name="T">The work-item type.</typeparam>
    /// <param name="handler">The original processing delegate.</param>
    /// <param name="keySelector">Extracts the correlation key from a work item.</param>
    /// <returns>A wrapped delegate that establishes test identity scope before calling the original handler.</returns>
    public static Func<T, CancellationToken, Task> Wrap<T>(
        Func<T, CancellationToken, Task> handler,
        Func<T, string> keySelector)
    {
        return async (item, cancellationToken) =>
        {
            var key = keySelector(item);
            using var scope = CorrelatedProcessingScope.Begin(key);
            await handler(item, cancellationToken);
        };
    }

    /// <summary>
    /// Wraps a synchronous processing action so each item is attributed to the correct test.
    /// </summary>
    /// <typeparam name="T">The work-item type.</typeparam>
    /// <param name="handler">The original processing action.</param>
    /// <param name="keySelector">Extracts the correlation key from a work item.</param>
    /// <returns>A wrapped action that establishes test identity scope before calling the original handler.</returns>
    public static Action<T> WrapSync<T>(
        Action<T> handler,
        Func<T, string> keySelector)
    {
        return item =>
        {
            var key = keySelector(item);
            using var scope = CorrelatedProcessingScope.Begin(key);
            handler(item);
        };
    }

    /// <summary>
    /// Wraps a batch processing delegate so items are attributed to the correct test.
    /// Uses the first correlatable item in the batch to establish scope.
    /// </summary>
    /// <typeparam name="T">The work-item type.</typeparam>
    /// <param name="handler">The original batch processing delegate.</param>
    /// <param name="keySelector">Extracts the correlation key from a work item.</param>
    /// <returns>A wrapped delegate that establishes test identity scope before calling the original handler.</returns>
    public static Func<IReadOnlyCollection<T>, CancellationToken, Task> WrapBatch<T>(
        Func<IReadOnlyCollection<T>, CancellationToken, Task> handler,
        Func<T, string> keySelector)
    {
        return async (items, cancellationToken) =>
        {
            IDisposable? scope = null;
            try
            {
                foreach (var item in items)
                {
                    var key = keySelector(item);
                    scope = CorrelatedProcessingScope.Begin(key);
                    if (scope is not null) break;
                }

                await handler(items, cancellationToken);
            }
            finally
            {
                scope?.Dispose();
            }
        };
    }
}
