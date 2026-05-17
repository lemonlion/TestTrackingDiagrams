using Kronikol.Tracking;

namespace Kronikol.Extensions.MongoDB;

/// <summary>
/// Wraps a MongoDB Change Stream processing delegate to establish <see cref="TestIdentityScope"/>
/// for each change document by resolving its ID from <see cref="TestCorrelationStore"/>.
/// <para>
/// Register in test DI only — no production code changes required.
/// The MongoDB tracking subscriber auto-populates the correlation store on writes
/// when <see cref="MongoDbTrackingOptions.AutoCorrelateWrites"/> is <c>true</c>.
/// </para>
/// </summary>
public static class ChangeStreamCorrelation
{
    /// <summary>
    /// Wraps a Change Stream processing delegate so each document is attributed to the correct test.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="handler">The original processing delegate.</param>
    /// <param name="serviceName">The service name matching <see cref="MongoDbTrackingOptions.ServiceName"/>.</param>
    /// <param name="idSelector">Extracts the document ID from a change item.</param>
    /// <returns>A wrapped delegate that establishes test identity scope before calling the original handler.</returns>
    public static Func<T, CancellationToken, Task> Wrap<T>(
        Func<T, CancellationToken, Task> handler,
        string serviceName,
        Func<T, string> idSelector)
    {
        return async (item, cancellationToken) =>
        {
            var docId = idSelector(item);
            var key = CorrelationKeys.Mongo(serviceName, docId);
            using var scope = CorrelatedProcessingScope.Begin(key);
            await handler(item, cancellationToken);
        };
    }

    /// <summary>
    /// Wraps a batch Change Stream processing delegate.
    /// </summary>
    public static Func<IReadOnlyCollection<T>, CancellationToken, Task> WrapBatch<T>(
        Func<IReadOnlyCollection<T>, CancellationToken, Task> handler,
        string serviceName,
        Func<T, string> idSelector)
    {
        return async (items, cancellationToken) =>
        {
            IDisposable? scope = null;
            try
            {
                foreach (var item in items)
                {
                    var docId = idSelector(item);
                    var key = CorrelationKeys.Mongo(serviceName, docId);
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
