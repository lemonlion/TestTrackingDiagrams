# Integration Guide: CosmosDB Extension

---

## Overview

The `TestTrackingDiagrams.Extensions.CosmosDB` package adds Cosmos DB operation tracking to your test diagrams. Instead of showing raw HTTP requests like `POST /dbs/abc123/colls/xyz789/docs`, your sequence diagrams show classified operations like `Create Document [orders]` or `Query [orders]: SELECT * FROM c WHERE c.status = 'active'`.

The extension works by inserting a `CosmosTrackingMessageHandler` (a `DelegatingHandler`) into the Cosmos SDK's HTTP pipeline. It classifies each HTTP request into a Cosmos operation (Create, Read, Query, Upsert, etc.) using the HTTP method, URL path, and headers, then logs it to `RequestResponseLogger` with a human-readable label.

---

## Prerequisites

- .NET 10.0 SDK or later
- An existing TestTrackingDiagrams setup (any framework — xUnit, NUnit, BDDfy, LightBDD, ReqNRoll)
- Azure Cosmos DB SDK (`Microsoft.Azure.Cosmos` 3.x)

---

## Install

```bash
dotnet add package TestTrackingDiagrams.Extensions.CosmosDB
```

---

## Verbosity Levels

The extension supports three verbosity levels that control how much detail appears in the diagrams:

| Level | Method shown | URI shown | Headers | Request body | Response body |
|---|---|---|---|---|---|
| **Raw** | HTTP method (`POST`, `GET`, etc.) | Full SDK URI (with `_rid`-encoded paths) | All except excluded set | Full JSON | Full JSON |
| **Detailed** | Classified label (`Create Document [orders]`) | Clean path (`/dbs/mydb/colls/orders`) | Filtered (noisy Cosmos headers excluded) | SQL text for queries, full JSON for others | Full JSON |
| **Summarised** | Classified label (`Create Document [orders]`) | Clean path (`/dbs/mydb/colls/orders`) | None | SQL text for queries only | None |

The default is **Detailed**.

---

## Classified Operations

The classifier recognises these Cosmos operations from the SDK's HTTP traffic:

| Operation | HTTP Pattern |
|---|---|
| Create | `POST /docs` (no upsert or query headers) |
| Read | `GET /docs/{id}` |
| Replace | `PUT /docs/{id}` |
| Patch | `PATCH /docs/{id}` |
| Delete | `DELETE /docs/{id}` |
| Upsert | `POST /docs` with `x-ms-documentdb-is-upsert: true` |
| Query | `POST /docs` with `x-ms-documentdb-isquery: true` |
| List | `GET /docs` (no document ID) |
| ExecStoredProc | `POST /sprocs/{id}` |
| Batch | `POST /colls/{id}` with batch content type |
| Other | SDK metadata requests (partition key ranges, collection reads, etc.) |

In **Summarised** mode, `Other` operations (SDK metadata) are silently skipped — they don't appear in the diagram.

---

## Setup

### Option A: With `CosmosDB.InMemoryEmulator` (DI — recommended)

If you use [`CosmosDB.InMemoryEmulator`](https://github.com/lemonlion/CosmosDB.InMemoryEmulator) with the `UseInMemoryCosmosDB()` DI extension, use `WithHttpMessageHandlerWrapper` to insert the tracking handler:

```csharp
builder.ConfigureTestServices(services =>
{
    services.UseInMemoryCosmosDB(options => options
        .AddContainer("orders", "/customerId")
        .AddContainer("customers", "/id")
        .WithHttpMessageHandlerWrapper(fakeHandler =>
            new CosmosTrackingMessageHandler(
                new CosmosTrackingMessageHandlerOptions
                {
                    ServiceName = "CosmosDB",
                    CallingServiceName = "My API",
                    Verbosity = CosmosTrackingVerbosity.Detailed,
                    CurrentTestInfoFetcher = () => /* your test info fetcher */
                },
                fakeHandler)));
});
```

The pipeline becomes:

```
CosmosClient → CosmosTrackingMessageHandler → FakeCosmosHandler → InMemoryContainer
```

The tracking handler intercepts and logs every operation, then forwards the request to `FakeCosmosHandler` which serves in-memory responses.

### Option B: With `CosmosDB.InMemoryEmulator` (manual wiring)

If you use `FakeCosmosHandler` directly (Pattern 6 / manual wiring):

```csharp
var inMemoryContainer = new InMemoryContainer("orders", "/customerId");
var fakeHandler = new FakeCosmosHandler(inMemoryContainer);

var trackingOptions = new CosmosTrackingMessageHandlerOptions
{
    ServiceName = "CosmosDB",
    CallingServiceName = "My API",
    Verbosity = CosmosTrackingVerbosity.Detailed,
    CurrentTestInfoFetcher = () => /* your test info fetcher */
};

var client = new CosmosClient(
    "AccountEndpoint=https://localhost:9999/;AccountKey=dGVzdGtleQ==;",
    new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        HttpClientFactory = () => new HttpClient(
            new CosmosTrackingMessageHandler(trackingOptions, fakeHandler))
    });
```

### Option C: With `CosmosClientOptions` extension (real Cosmos or emulator)

For use against a real Cosmos DB instance or the Microsoft Cosmos DB Emulator:

```csharp
var trackingOptions = new CosmosTrackingMessageHandlerOptions
{
    ServiceName = "CosmosDB",
    CallingServiceName = "My API",
    Verbosity = CosmosTrackingVerbosity.Detailed,
    CurrentTestInfoFetcher = () => /* your test info fetcher */
};

var clientOptions = new CosmosClientOptions();
clientOptions.WithTestTracking(trackingOptions);

// Or, for the emulator with self-signed certs:
clientOptions.WithTestTrackingAndCustomSslValidation(trackingOptions);

var client = new CosmosClient(connectionString, clientOptions);
```

> **Note:** `WithTestTracking` forces `ConnectionMode.Gateway` because Cosmos DB's Direct mode uses a custom TCP protocol (RNTBD) that bypasses `HttpMessageHandler` entirely.

---

## Configuration

### `CosmosTrackingMessageHandlerOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `ServiceName` | `string` | `"CosmosDB"` | The participant name shown in the diagram for the Cosmos DB service |
| `CallingServiceName` | `string` | `"Caller"` | The participant name shown for the service making Cosmos calls |
| `Verbosity` | `CosmosTrackingVerbosity` | `Detailed` | Controls how much detail appears in the diagram (Raw, Detailed, Summarised) |
| `CurrentTestInfoFetcher` | `Func<(string Name, string Id)>?` | `null` | Returns the current test's name and ID. Required — if null, requests are forwarded but not logged |
| `CurrentStepTypeFetcher` | `Func<string?>?` | `null` | Optional — returns the current BDD step type (Given/When/Then) |
| `ExcludedHeaders` | `HashSet<string>` | See below | Headers to exclude from diagrams in Raw/Detailed mode |

**Default excluded headers:**
- `Authorization`
- `x-ms-date`
- `x-ms-version`
- `x-ms-session-token`
- `User-Agent`
- `Cache-Control`
- `x-ms-cosmos-sdk-supportedcapabilities`
- `x-ms-cosmos-internal-operation-type`

### `CurrentTestInfoFetcher` by Framework

The `CurrentTestInfoFetcher` delegate must return the current test's name and unique ID. How you obtain this depends on your test framework:

**xUnit v3:**
```csharp
CurrentTestInfoFetcher = () =>
{
    var ctx = TestContext.Current;
    return (ctx.Test.TestDisplayName, ctx.Test.UniqueID);
}
```

**xUnit v2 (with TestTrackingDiagrams.xUnit2):**
```csharp
CurrentTestInfoFetcher = () =>
{
    var (name, id) = XUnit2TestAccessor.GetCurrentTest();
    return (name, id);
}
```

**NUnit:**
```csharp
CurrentTestInfoFetcher = () =>
{
    var test = TestContext.CurrentContext.Test;
    return (test.FullName, test.ID);
}
```

---

## Diagram Labels by Verbosity

Examples of how the same operation appears at each verbosity level:

| Operation | Raw | Detailed | Summarised |
|---|---|---|---|
| Create a document | `POST` | `Create Document [orders]` | `Create Document [orders]` |
| Read a document | `GET` | `Read Document [orders]` | `Read Document [orders]` |
| Query documents | `POST` | `Query [orders]: SELECT * FROM c WHERE c.status = 'active'` | `Query [orders]: SELECT * FROM c WHERE c.status = 'active'` |
| SDK metadata | `GET` | `GET` (shown as raw) | *(skipped — not shown)* |

---

## Architecture

```
┌──────────────┐                ┌───────────────────────────────┐              ┌──────────────────────┐
│  Cosmos SDK  │ ── HTTP ──►    │ CosmosTrackingMessageHandler  │ ── HTTP ──►  │  Inner Handler       │
│  (real)      │ ◄── HTTP ──    │  • Classifies operation       │ ◄── HTTP ──  │  (FakeCosmosHandler  │
│              │                │  • Logs to RequestResponseLog │              │   or HttpClientHandler│
└──────────────┘                └───────────────────────────────┘              └──────────────────────┘
                                         │
                                         ▼
                               ┌──────────────────────┐
                               │ RequestResponseLogger │
                               │  (shared with other   │
                               │   tracking handlers)  │
                               └──────────────────────┘
```

The `CosmosTrackingMessageHandler` logs to the same `RequestResponseLogger` as the standard `TestTrackingMessageHandler`. This means Cosmos operations appear alongside your HTTP API calls in the same sequence diagram — showing the complete flow from test → API → Cosmos DB.
