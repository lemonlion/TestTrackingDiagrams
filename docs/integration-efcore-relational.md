# Integration Guide: EF Core Relational Extension

---

## Overview

The `TestTrackingDiagrams.Extensions.EfCore.Relational` package adds SQL operation tracking to your test diagrams for any EF Core relational database provider. Instead of invisible database interactions, your sequence diagrams show classified operations like `Select: sql://server/db/Users` or `Upsert: sql://db/Orders`.

The extension works by inserting a `SqlTrackingInterceptor` (a `DbCommandInterceptor`) into EF Core's command pipeline. It classifies each SQL command into an operation (Select, Insert, Update, Delete, Upsert, Merge, StoredProc) by parsing the command text and `CommandType`, then logs it to `RequestResponseLogger` with a human-readable label.

### Supported Providers

The classifier handles SQL dialects from all major EF Core relational providers:

| Provider | Package | Quoting | Upsert Syntax | Stored Proc Syntax |
|---|---|---|---|---|
| **SQL Server** | `Microsoft.EntityFrameworkCore.SqlServer` | `[brackets]` | `MERGE` | `EXEC`/`EXECUTE` or `CommandType.StoredProcedure` |
| **PostgreSQL** | `Npgsql.EntityFrameworkCore.PostgreSQL` | `"double quotes"` | `INSERT...ON CONFLICT DO UPDATE` | `CALL` or `CommandType.StoredProcedure` |
| **MySQL** | `Pomelo.EntityFrameworkCore.MySql` | `` `backticks` `` | `INSERT...ON DUPLICATE KEY UPDATE` | `CALL` or `CommandType.StoredProcedure` |
| **SQLite** | `Microsoft.EntityFrameworkCore.Sqlite` | `"double quotes"` | `INSERT OR REPLACE` / `INSERT...ON CONFLICT DO UPDATE` | *(not supported)* |
| **Oracle** | `Oracle.EntityFrameworkCore` | `"DOUBLE QUOTES"` | `MERGE` | `CALL` or `CommandType.StoredProcedure` |
| **Spanner (GoogleSQL)** | `Google.Cloud.EntityFrameworkCore.Spanner` | `` `backticks` `` | `MERGE` / `INSERT OR UPDATE` / `INSERT...ON CONFLICT DO UPDATE` | `CALL` (built-in only) |
| **Spanner (PostgreSQL)** | `Google.Cloud.EntityFrameworkCore.Spanner` | `"double quotes"` | `INSERT...ON CONFLICT DO UPDATE` | `CALL` |

---

## Prerequisites

- .NET 10.0 SDK or later
- An existing TestTrackingDiagrams setup (any framework — xUnit, NUnit, BDDfy, LightBDD, ReqNRoll)
- Any EF Core relational provider (`Microsoft.EntityFrameworkCore.Relational` 9.x)

---

## Install

```bash
dotnet add package TestTrackingDiagrams.Extensions.EfCore.Relational
```

---

## Verbosity Levels

The extension supports three verbosity levels that control how much detail appears in the diagrams:

| Level | Method shown | URI shown | Command text | Parameters |
|---|---|---|---|---|
| **Raw** | SQL keyword (`SELECT`) | `sql://server/database` | Full SQL | All |
| **Detailed** | Classified operation (`Select`) | `sql://server/database/TableName` | Full SQL | Named params |
| **Summarised** | Classified operation (`Select`) | `sql://database/TableName` | None | None |

The default is **Detailed**.

---

## Classified Operations

The classifier recognises these SQL operations:

| Operation | Detection |
|---|---|
| **Select** | `SELECT` keyword |
| **Insert** | `INSERT INTO` (without upsert modifiers) |
| **Update** | `UPDATE` keyword |
| **Delete** | `DELETE` keyword |
| **Merge** | `MERGE` keyword (SQL Server, Oracle, Spanner) |
| **Upsert** | `INSERT OR UPDATE` (Spanner), `INSERT OR REPLACE` (SQLite), `INSERT...ON CONFLICT DO UPDATE` (PostgreSQL, SQLite, Spanner), `INSERT...ON DUPLICATE KEY UPDATE` (MySQL) |
| **StoredProc** | `CommandType.StoredProcedure`, or `EXEC`/`EXECUTE` (SQL Server), or `CALL` (PostgreSQL, MySQL, Oracle, Spanner) |
| **Other** | DDL, `SET`, `BEGIN`, `TRUNCATE`, `COPY`, `BULK INSERT`, etc. |

In **Summarised** mode, `Other` operations are silently skipped — they don't appear in the diagram.

The classifier also handles:
- **CTEs** — `WITH cte AS (...) SELECT/INSERT/UPDATE/DELETE/MERGE` — the real operation after the CTE is classified
- **SET prefixes** — `SET NOCOUNT ON; SELECT ...` — the SET is skipped
- **Spanner statement hints** — `@{PDML_MAX_PARALLELISM=10} DELETE ...` — the hint is skipped
- **All quoting styles** — `[brackets]`, `"double quotes"`, `` `backticks` `` — stripped for table name extraction
- **Schema-prefixed identifiers** — `[dbo].[Users]`, `"public"."Orders"`, `` `mydb`.`Users` `` — last part extracted

---

## Setup

### Option A: EF Core DI (recommended)

In your test `WebApplicationFactory` or DI configuration:

```csharp
builder.ConfigureTestServices(services =>
{
    services.AddDbContext<MyDbContext>(options =>
    {
        options.UseSqlServer(connectionString); // or UseNpgsql, UseMySql, etc.
        options.WithSqlTestTracking(new SqlTrackingInterceptorOptions
        {
            ServiceName = "SQL Server",
            CallingServiceName = "My API",
            Verbosity = SqlTrackingVerbosity.Detailed,
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

### Option B: Manual DbContext configuration

```csharp
var trackingOptions = new SqlTrackingInterceptorOptions
{
    ServiceName = "PostgreSQL",
    CallingServiceName = "My API",
    Verbosity = SqlTrackingVerbosity.Detailed,
    CurrentTestInfoFetcher = () =>
    {
        var test = TestContext.Current.Test;
        return test is not null
            ? (test.TestDisplayName, test.UniqueID)
            : ("Unknown", "unknown");
    }
};

var contextOptions = new DbContextOptionsBuilder<MyDbContext>()
    .UseNpgsql(connectionString)
    .WithSqlTestTracking(trackingOptions)
    .Options;

using var context = new MyDbContext(contextOptions);
```

### Option C: Raw interceptor (without extension method)

```csharp
var interceptor = new SqlTrackingInterceptor(new SqlTrackingInterceptorOptions
{
    ServiceName = "MySQL",
    CallingServiceName = "My API",
    Verbosity = SqlTrackingVerbosity.Summarised,
    CurrentTestInfoFetcher = () =>
    {
        var test = TestContext.Current.Test;
        return test is not null
            ? (test.TestDisplayName, test.UniqueID)
            : ("Unknown", "unknown");
    }
});

var contextOptions = new DbContextOptionsBuilder<MyDbContext>()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
    .AddInterceptors(interceptor)
    .Options;
```

---

## Configuration

### `SqlTrackingInterceptorOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `ServiceName` | `string` | `"Database"` | The participant name shown in the diagram for the database service |
| `CallingServiceName` | `string` | `"Caller"` | The participant name shown for the service making database calls |
| `Verbosity` | `SqlTrackingVerbosity` | `Detailed` | Controls how much detail appears in the diagram (Raw, Detailed, Summarised) |
| `CurrentTestInfoFetcher` | `Func<(string Name, string Id)>?` | `null` | Returns the current test's name and ID. Required — if null, commands are executed but not logged |
| `CurrentStepTypeFetcher` | `Func<string?>?` | `null` | Optional — returns the current BDD step type (Given/When/Then) |

### `CurrentTestInfoFetcher` by Framework

**xUnit v3:**
```csharp
CurrentTestInfoFetcher = () =>
{
    var test = TestContext.Current.Test;
    return test is not null
        ? (test.TestDisplayName, test.UniqueID)
        : ("Unknown", "unknown");
}
```

**NUnit:**
```csharp
CurrentTestInfoFetcher = () =>
{
    var test = NUnit.Framework.TestContext.CurrentContext.Test;
    return (test.FullName, test.ID);
}
```

---

## Diagram Labels by Verbosity

Examples of how the same operation appears at each verbosity level:

| Operation | Raw | Detailed | Summarised |
|---|---|---|---|
| Read rows | `SELECT` | `Select` | `Select` |
| Insert a row | `INSERT` | `Insert` | `Insert` |
| Upsert (PostgreSQL) | `INSERT` | `Upsert` | `Upsert` |
| Stored procedure | `EXEC` | `StoredProc` | `StoredProc` |
| DDL/metadata | `CREATE` | `Other` | *(skipped — not shown)* |

---

## Architecture

```
┌────────────────┐              ┌───────────────────────────┐              ┌──────────────┐
│  EF Core App   │ ── SQL ──►   │  SqlTrackingInterceptor   │ ── SQL ──►   │   Database   │
│  (DbContext)   │ ◄── SQL ──   │  • Classifies operation   │ ◄── SQL ──   │  (any RDBMS) │
│                │              │  • Logs to RequestResponse │              │              │
└────────────────┘              └───────────────────────────┘              └──────────────┘
                                         │
                                         ▼
                               ┌──────────────────────┐
                               │ RequestResponseLogger │
                               │  (shared with other   │
                               │   tracking handlers)  │
                               └──────────────────────┘
```

The `SqlTrackingInterceptor` logs to the same `RequestResponseLogger` as the standard `TestTrackingMessageHandler` and `CosmosTrackingMessageHandler`. This means SQL operations appear alongside your HTTP API calls and Cosmos operations in the same sequence diagram — showing the complete flow from test → API → Database.
