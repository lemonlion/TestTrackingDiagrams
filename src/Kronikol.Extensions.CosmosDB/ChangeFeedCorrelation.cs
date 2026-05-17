using System.Text.Json;
using Kronikol.Tracking;

namespace Kronikol.Extensions.CosmosDB;

/// <summary>
/// Wraps a Change Feed Processor delegate to establish <see cref="TestIdentityScope"/>
/// for each change item by resolving its document ID from <see cref="TestCorrelationStore"/>.
/// <para>
/// Register in test DI only — no production code changes required.
/// The CosmosDB tracking handler auto-populates the correlation store on writes
/// when <see cref="CosmosTrackingMessageHandlerOptions.AutoCorrelateWrites"/> is <c>true</c>.
/// </para>
/// </summary>
public static class ChangeFeedCorrelation
{
    /// <summary>
    /// Wraps a Change Feed delegate so each item processed is attributed to the correct test.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="handler">The original Change Feed handler delegate.</param>
    /// <param name="serviceName">The service name matching <see cref="CosmosTrackingMessageHandlerOptions.ServiceName"/>.</param>
    /// <param name="idSelector">
    /// Extracts the document ID from a change item. If <c>null</c>, reads the "id" property via reflection or JsonElement.
    /// </param>
    /// <returns>A wrapped delegate that establishes test identity scope before calling the original handler.</returns>
    public static Func<IReadOnlyCollection<T>, CancellationToken, Task> Wrap<T>(
        Func<IReadOnlyCollection<T>, CancellationToken, Task> handler,
        string serviceName,
        Func<T, string>? idSelector = null)
    {
        return async (changes, cancellationToken) =>
        {
            var scopes = new List<IDisposable>();
            try
            {
                foreach (var item in changes)
                {
                    var docId = idSelector is not null
                        ? idSelector(item)
                        : ExtractId(item);

                    if (docId is not null)
                    {
                        var key = CorrelationKeys.Cosmos(serviceName, docId);
                        var scope = CorrelatedProcessingScope.Begin(key);
                        if (scope is not null)
                        {
                            scopes.Add(scope);
                            break; // Use first match (all items in batch are typically from same test)
                        }
                    }
                }

                await handler(changes, cancellationToken);
            }
            finally
            {
                foreach (var scope in scopes)
                    scope.Dispose();
            }
        };
    }

    /// <summary>
    /// Wraps a Change Feed delegate that receives <see cref="JsonElement"/> items.
    /// </summary>
    public static Func<IReadOnlyCollection<JsonElement>, CancellationToken, Task> WrapJson(
        Func<IReadOnlyCollection<JsonElement>, CancellationToken, Task> handler,
        string serviceName,
        Func<JsonElement, string>? idSelector = null)
    {
        return Wrap(handler, serviceName, idSelector ?? (item =>
            item.TryGetProperty("id", out var idProp) ? idProp.GetString()! : null!));
    }

    private static string? ExtractId<T>(T item)
    {
        if (item is JsonElement json)
        {
            return json.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        }

        // Try reflection for POCO types
        var idPropInfo = typeof(T).GetProperty("Id")
            ?? typeof(T).GetProperty("id");
        return idPropInfo?.GetValue(item)?.ToString();
    }
}
