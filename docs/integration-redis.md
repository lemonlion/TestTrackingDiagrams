The `TestTrackingDiagrams.Extensions.Redis` package adds Redis cache operation tracking to your test diagrams with **cache hit/miss visualization**. Instead of invisible Redis operations, your sequence diagrams show classified operations like `Get (Hit): redis://db0/user:123` or `Set: redis://db0/session:abc`.

---

## How It Works

`RedisTrackingDatabase` is a `DispatchProxy`-based decorator around `IDatabase` that intercepts Redis operations, classifies each one (Get, Set, Delete, HashGet, ListPush, etc.), determines **cache hit/miss** for read operations, then logs it to `RequestResponseLogger` with a human-readable label.

Because it logs to the same `RequestResponseLogger` as the standard `TestTrackingMessageHandler`, Redis operations appear alongside your HTTP API calls, Cosmos DB operations, and SQL queries in the same sequence diagram — showing the complete flow from test → API → Redis.

### Cache Hit/Miss Detection

The key differentiator of this extension is automatic **cache hit/miss visualization**:

- **Hit**: A `StringGet`, `HashGet`, or similar read operation returned a non-null value
- **Miss**: A read operation returned `RedisValue.Null` (key doesn't exist or field doesn't exist)
- **None**: Write operations (Set, Delete, Expire, etc.) — hit/miss doesn't apply

This information is encoded directly in the diagram labels: `Get (Hit)`, `Get (Miss)`, `HashGet (Hit)`, `HashGet (Miss)`, etc.

---

## Install

```bash
dotnet add package TestTrackingDiagrams.Extensions.Redis
```

---

## Verbosity Levels

The extension supports three verbosity levels that control how much detail appears in the diagrams:

| Level | Method shown | URI shown | Request content | Response content |
|---|---|---|---|---|
| **Raw** | Redis command name (`GET`, `SET`, etc.) | `redis://host:port/db/key` | Full value | Full value |
| **Detailed** | Classified label (`Get (Hit)`, `Set`) | `redis://db0/mykey` | Full value | Full value |
| **Summarised** | Classified label (`Get (Hit)`, `Set`) | `redis://db0/` | None | None |

The default is **Detailed**.

### Diagram Label Examples

| Operation | Result | Raw | Detailed | Summarised |
|---|---|---|---|---|
| `StringGet("user:123")` → `"John"` | Hit | `GET: redis://localhost/0/user:123` | `Get (Hit): redis://db0/user:123` | `Get (Hit): redis://db0/` |
| `StringGet("user:999")` → `null` | Miss | `GET: redis://localhost/0/user:999` | `Get (Miss): redis://db0/user:999` | `Get (Miss): redis://db0/` |
| `StringSet("user:123", "John")` | — | `SET: redis://localhost/0/user:123` | `Set: redis://db0/user:123` | `Set: redis://db0/` |
| `KeyDelete("user:123")` | — | `DEL: redis://localhost/0/user:123` | `Delete: redis://db0/user:123` | `Delete: redis://db0/` |
| `HashGet("user:123", "name")` → `"John"` | Hit | `HGET: redis://localhost/0/user:123` | `HashGet (Hit): redis://db0/user:123` | `HashGet (Hit): redis://db0/` |
| `HashGet("user:123", "missing")` → `null` | Miss | `HGET: redis://localhost/0/user:123` | `HashGet (Miss): redis://db0/user:123` | `HashGet (Miss): redis://db0/` |
| `HashSet("user:123", "name", "John")` | — | `HSET: redis://localhost/0/user:123` | `HashSet: redis://db0/user:123` | `HashSet: redis://db0/` |
| `ListLeftPush("queue", "msg")` | — | `LPUSH: redis://localhost/0/queue` | `ListPush: redis://db0/queue` | `ListPush: redis://db0/` |
| `SetAdd("tags", "redis")` | — | `SADD: redis://localhost/0/tags` | `SetAdd: redis://db0/tags` | `SetAdd: redis://db0/` |
| `StringIncrement("counter")` → `42` | — | `INCR: redis://localhost/0/counter` | `Increment: redis://db0/counter` | `Increment: redis://db0/` |
| `KeyExpire("key", 5min)` | — | `EXPIRE: redis://localhost/0/key` | `Expire: redis://db0/key` | `Expire: redis://db0/` |

---

## Classified Operations

The classifier recognises these Redis operations:

| Operation | Redis Commands | Cache Hit/Miss |
|---|---|---|
| `Get` | `GET`, `GETDEL`, `GETSET`, `GETEX`, `MGET` | ✅ Yes |
| `Set` | `SET`, `SETEX`, `SETNX`, `PSETEX`, `MSET`, `APPEND` | ❌ None |
| `Delete` | `DEL`, `UNLINK` | ❌ None |
| `KeyExists` | `EXISTS` | ❌ None |
| `Expire` | `EXPIRE`, `PEXPIRE`, `EXPIREAT`, `PERSIST` | ❌ None |
| `Increment` | `INCR`, `INCRBY`, `INCRBYFLOAT` | ❌ None |
| `Decrement` | `DECR`, `DECRBY` | ❌ None |
| `HashGet` | `HGET`, `HMGET` | ✅ Yes |
| `HashGetAll` | `HGETALL` | ❌ None |
| `HashSet` | `HSET`, `HMSET`, `HSETNX` | ❌ None |
| `HashDelete` | `HDEL` | ❌ None |
| `ListPush` | `LPUSH`, `RPUSH` | ❌ None |
| `ListRange` | `LRANGE` | ❌ None |
| `SetAdd` | `SADD` | ❌ None |
| `SetMembers` | `SMEMBERS` | ❌ None |
| `Publish` | `PUBLISH` | ❌ None |
| `Other` | Any unrecognised command | ❌ None |

In **Summarised** mode, `Other` operations are silently skipped.

---

## Setup

### Option A: IDatabase Decorator (recommended)

The simplest approach — wrap your `IDatabase` with the tracking decorator:

```csharp
var options = new RedisTrackingDatabaseOptions
{
    ServiceName = "Redis",
    CallingServiceName = "My API",
    Verbosity = RedisTrackingVerbosity.Detailed,
    CurrentTestInfoFetcher = () =>
    {
        var test = TestContext.Current.Test;
        return test is not null
            ? (test.TestDisplayName, test.UniqueID)
            : ("Unknown", "unknown");
    }
};

// Wrap any IDatabase instance
IDatabase db = multiplexer.GetDatabase();
IDatabase trackedDb = db.WithRedisTestTracking(options);

// Use trackedDb everywhere — all operations are tracked
var value = trackedDb.StringGet("user:123"); // logs Get (Hit) or Get (Miss)
trackedDb.StringSet("user:123", "John");     // logs Set
```

### Option B: Extension Method on IConnectionMultiplexer

Shorthand to get a tracked database directly:

```csharp
IDatabase db = multiplexer.GetTrackedDatabase(options);
```

### Option C: DI Registration in Test Setup

For ASP.NET Core integration tests using `WebApplicationFactory`:

```csharp
builder.ConfigureTestServices(services =>
{
    // Replace the IDatabase registration with a tracked version
    services.AddSingleton<IDatabase>(sp =>
    {
        var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
        return multiplexer.GetTrackedDatabase(new RedisTrackingDatabaseOptions
        {
            ServiceName = "Redis",
            CallingServiceName = "My API",
            Verbosity = RedisTrackingVerbosity.Detailed,
            CurrentTestInfoFetcher = () =>
            {
                var test = TestContext.Current.Test;
                return test is not null
                    ? (test.TestDisplayName, test.UniqueID)
                    : ("Unknown", "unknown");
            }
        });
    });
});
```

### Option D: With a Fake/In-Memory Redis

If you use a fake Redis implementation (e.g. for unit tests), wrap the fake's `IDatabase`:

```csharp
var fakeDb = GetFakeRedisDatabase(); // your in-memory fake
var trackedDb = fakeDb.WithRedisTestTracking(options);
```

The tracking decorator simply wraps any `IDatabase` — it doesn't care whether the underlying implementation is a real Redis connection or a fake.

---

## CurrentTestInfoFetcher by Framework

The `CurrentTestInfoFetcher` delegate tells the tracker which test is currently running. Set it based on your test framework:

| Framework | CurrentTestInfoFetcher |
|---|---|
| **xUnit v3** | `() => { var t = TestContext.Current.Test; return t is not null ? (t.TestDisplayName, t.UniqueID) : ("Unknown", "unknown"); }` |
| **xUnit v2** | `() => (TestContext.Current.TestName, TestContext.Current.TestId)` (via TestTrackingDiagrams.xUnit2) |
| **NUnit** | `() => (NUnit.Framework.TestContext.CurrentContext.Test.FullName, NUnit.Framework.TestContext.CurrentContext.Test.ID)` |
| **MSTest** | `() => (TestContext.TestName, TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName)` |

---

## Architecture

The tracking pipeline for Redis operations:

```
Test Code
    │
    ▼
IDatabase (RedisTrackingDatabase proxy)
    │
    ├── Classify operation (GET → Get, SET → Set, etc.)
    ├── Log request to RequestResponseLogger
    │
    ▼
IDatabase (real/fake inner)
    │
    ├── Execute Redis command
    │
    ▼
RedisTrackingDatabase
    │
    ├── Detect hit/miss from result
    ├── Log response to RequestResponseLogger
    │
    ▼
RequestResponseLogger → PlantUmlCreator → ReportGenerator
```

The `DispatchProxy` approach means **every `IDatabase` method** is automatically forwarded to the inner implementation. Only the methods explicitly mapped in the command table produce tracking logs — all others pass through transparently with zero overhead.

### Tracked vs Untracked Methods

The following method families are tracked (produce diagram entries):

- **String**: `StringGet`, `StringSet`, `StringIncrement`, `StringDecrement`, `StringGetDelete`, `StringGetSet`, `StringAppend`
- **Key**: `KeyDelete`, `KeyExists`, `KeyExpire`, `KeyPersist`
- **Hash**: `HashGet`, `HashGetAll`, `HashSet`, `HashDelete`, `HashIncrement`, `HashDecrement`, `HashExists`
- **List**: `ListLeftPush`, `ListRightPush`, `ListLeftPop`, `ListRightPop`, `ListRange`, `ListMove`
- **Set**: `SetAdd`, `SetMembers`, `SetRemove`, `SetPop`, `SetContains`
- **Pub/Sub**: `Publish`

All other `IDatabase` methods (Geo, HyperLogLog, Sorted Set, Stream, Script, Lock, Vector, metadata queries like `KeyTimeToLive`, `StringLength`, etc.) pass through without tracking. This keeps diagrams focused on the cache interactions that matter for understanding application behaviour.
