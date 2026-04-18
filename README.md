[![NuGet](https://img.shields.io/nuget/v/TestTrackingDiagrams.svg)](https://www.nuget.org/packages/TestTrackingDiagrams)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TestTrackingDiagrams.svg)](https://www.nuget.org/packages/TestTrackingDiagrams)
[![CI](https://github.com/lemonlion/TestTrackingDiagrams/actions/workflows/ci.yml/badge.svg)](https://github.com/lemonlion/TestTrackingDiagrams/actions/workflows/ci.yml)


<a name="top"></a>

# <img src="icon.svg" width="32" height="32" alt="TTD icon" style="vertical-align: middle;"> TestTrackingDiagrams

Effortlessly autogenerate **PlantUML sequence diagrams** from your component and acceptance tests every time you run them. Tracks interactions between your test caller, your Service Under Test (SUT), and its dependencies — including HTTP calls, Azure Cosmos DB operations, SQL queries (via EF Core), Redis commands, events/messages, and arbitrary method calls — then converts them into diagrams embedded in searchable HTML reports and YAML specification files.

---

## Table of Contents

- [Example Output](#example-output)
- [How It Works](#how-it-works)
- [Use Cases](#use-cases)
- [Deterministic vs AI-Generated Diagrams](#deterministic-vs-ai)
- [Supported Frameworks & NuGet Packages](#supported-frameworks)
- [Recommended BDD Framework](#recommended-bdd)
- [Documentation](#documentation)

---

## <a name="example-output"></a>Example Output [↑](#top)

[<img width="770" height="1017" alt="image" src="https://github.com/user-attachments/assets/43d48a00-ba37-4951-945c-dd75de64c2bb" />](https://www.plantuml.com/plantuml/uml/j5NDJjj04BvRyZiCBWSGSKa28c2520418PI2258F2A7DxjYnukn6utKYG7so7lf8VONkseO4DnKGgJvvPhwPRtxjtpz_7QMQaSx6YUkiJOX5OmOQrHDeoj1rsgb-JB3ZEl0LfoZrDwKHderedsF6Hn6fJ8eJbIY2Bpn47hPAwvcIkXy_8JGQfUQcW994WaRTA7yOWkqNXdGKomaZDeOPiSdtMEWXxDSDR2tC9DUnah3EBS_6-fGb6MuQ2w7EI8BNpWs1jrMOjZpeUCQCKhpukWxBj9BPU639NSV8N9kSlHEM94WUi1Hu_kewfivOFu9tYccAfE6Qr3GM9KWKoXVT77sYPj17ciOSYsXgLefpJDVs40QaHcMqlAd7kMnpAZ80lrEb2U2yUnl0zZXEHgvJCLhydEqjTAu7VrdOgqlNaNQe54T3xJfbYoDYZvjtfqpZOK_96ZHteCS8clNc7ZJsWjrwK6_0UU_slk9XkP0EBp7LX4dLUajCfY6ItvLSYLX6XtoOoH4A0tITVAqycxONWDTNOtmu8qo73pshSXspBMQWOBDTqWBRWxnxVzVqgS3lZW2ZA5s5t_gzydUjy5dcC54PhKATEyvhpwMFarzVzIqxPoCiWoSOljlMsZ-PQnzhnv8bNxYMmDoQWMuK5sL0MfbDsoppVCYHPamsLBlz-kdgTDxaVimq7ru8cmORx30sE6ZvZHRd_cvJD7qMjfWVYi4-QxA3aDTtoymlP4GeOajWFE-AZzj27RL5JRKZK1a2m7sbxeKYbv_i3QOJ9GMALLPXi5B9vbJ_Pyb7vZN_0_q1003__mC0)

Each test that uses tracked dependencies automatically produces a sequence diagram (with matching PlantUML) showing the full request/response flow between services.

> **Tip:** You can visually separate the setup (arrange) phase from the action phase using the [`SeparateSetup`](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Diagram-Customisation#setup-separation) flag.

---

## <a name="how-it-works"></a>How It Works [↑](#top)

```
┌─────────────┐     HTTP      ┌─────────────┐     HTTP      ┌─────────────┐
│  Test Code  │ ──────────►   │     SUT     │ ──────────►   │ Dependency  │
│  (Caller)   │ ◄──────────   │  (Your API) │ ◄──────────   │  (Fakes)    │
└─────────────┘               └──────┬──────┘               └─────────────┘
                                     │
                        ┌────────────┼────────────┐
                        │            │            │
                        ▼            ▼            ▼
                  ┌──────────┐ ┌──────────┐ ┌──────────┐
                  │ CosmosDB │ │  SQL DB  │ │  Redis   │
                  │ (Fakes)  │ │ (Fakes)  │ │ (Fakes)  │
                  └──────────┘ └──────────┘ └──────────┘
                        │            │            │
           ┌────────────┼────────────┼────────────┘
           │            │            │
           │   Event / Message       │   Method calls
           │ ──────────────────►     │ ──────────────────►
           │  ┌──────────────┐       │  ┌──────────────┐
           │  │ Event broker │       │  │  Any service │
           │  │   (Fakes)    │       │  │ (via Proxy)  │
           │  └──────────────┘       │  └──────────────┘
           │                         │
           └─── All interactions are intercepted and logged ──┘
                                     │
                                     ▼
                          ┌──────────────────────┐
                          │ RequestResponseLogger│
                          │  (in-memory log)     │
                          └──────────┬───────────┘
                                     │
                                     ▼
                          ┌──────────────────────┐
                          │   PlantUmlCreator    │
                          │ (generates diagrams) │
                          └──────────┬───────────┘
                                     │
                                     ▼
                          ┌──────────────────────┐
                          │   ReportGenerator    │
                          │  (HTML + YAML files) │
                          └──────────────────────┘
```

1. **Intercept** — Each type of dependency has a dedicated tracking mechanism that logs interactions to the same in-memory store:

    | Dependency | Tracking mechanism | How it works |
    |---|---|---|
    | **HTTP** | `TestTrackingMessageHandler` | `DelegatingHandler` in the HTTP pipeline — logs method, URI, headers, body, status code |
    | **Azure Cosmos DB** | `CosmosTrackingMessageHandler` | `DelegatingHandler` injected via `CosmosClientOptions.HttpClientFactory` — classifies operations (Query, Create, Upsert, etc.) with configurable verbosity |
    | **SQL (EF Core)** | `SqlTrackingInterceptor` | `DbCommandInterceptor` — intercepts SQL commands from any relational provider (SQL Server, PostgreSQL, MySQL, SQLite, etc.) |
    | **Redis** | `RedisTrackingDatabase` | `DispatchProxy` wrapping `IDatabase` — tracks GET, SET, HSET, LPUSH, PUBLISH, etc. with cache hit/miss status |
    | **Any interface** | `TrackingProxy<T>` | .NET `DispatchProxy` wrapping any interface — logs method calls, arguments, return values, and exceptions |
    | **Events / messages** | `MessageTracker` | Manual logging — for Kafka, RabbitMQ, EventGrid, or any message bus |

    See the [Tracking Dependencies](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Tracking-Dependencies) wiki page for setup guides.

2. **Collect** — All logged `RequestResponseLog` entries are held in the static `RequestResponseLogger`. Each entry captures the operation details, service names, and a trace ID to correlate requests across services.

3. **Generate** — At the end of the test run, `PlantUmlCreator` groups logs by test ID and converts them into sequence diagram code. PlantUML diagrams are encoded and rendered via a PlantUML server (or locally via IKVM), or rendered client-side in the browser.

4. **Report** — `ReportGenerator` combines the diagrams with test metadata (features, scenarios, results, BDD steps) to produce three output files: a YAML specification, an HTML specification with diagrams, and an HTML test run report.

---

## <a name="use-cases"></a>Use Cases [↑](#top)

### Debugging failed tests locally and in CI/staging

When a test fails, the sequence diagram shows exactly which interaction returned an unexpected result — whether it's an HTTP call, a database query, a Redis command, or a method call. Status codes, headers, payloads, and SQL text are all visible in the diagram notes. This eliminates guesswork when diagnosing failures, whether you're debugging locally or triaging a failed CI pipeline run against a staging environment. Instead of adding logging, re-running, and reading through console output, the diagram gives you the full picture in a single image.

### Living documentation for stakeholders, developers, and AI

The generated HTML reports and YAML specifications serve as an always-up-to-date source of truth for how your API behaves. Because they're produced directly from passing tests, they can never drift out of sync with the actual implementation. Stakeholders can browse the HTML reports to understand feature behaviour without reading code. Developers can use them during onboarding or when working in unfamiliar areas of the codebase. AI assistants can consume the YAML specs or PlantUML source to answer questions about service interactions with high accuracy.

### Feeding AI tools for more accurate analysis

The raw PlantUML code behind each diagram is a compact, structured representation of your service's interactions — HTTP calls, database queries, cache operations, events, and more. You can feed it directly into AI coding assistants, chat interfaces, or documentation generators to give them precise context about how services communicate. This produces significantly better results than asking an AI to infer behaviour from source code alone, because the diagrams capture the actual runtime flow including payloads, status codes, and service names.

### Creating accurate high-level architecture diagrams

The per-test sequence diagrams provide a ground-truth foundation for building higher-level architecture and integration diagrams. Rather than drawing C4 models, system context diagrams, or integration maps from memory (which inevitably drift from reality), you or an AI can derive them from the concrete service interactions captured in the test suite. The PlantUML source is particularly useful here — an AI can aggregate the participants and message flows across multiple test diagrams to produce accurate summary diagrams.

### Reviewing pull requests

When a PR changes service interactions (new downstream calls, modified payloads, changed queries, different cache patterns), the sequence diagrams in the test reports make the impact immediately visible. Reviewers can compare the before and after diagrams to understand exactly what changed, without having to mentally trace through the code.

### Onboarding and knowledge transfer

New team members can browse the HTML reports to quickly understand how the system's services interact, what endpoints and data stores exist, and what the expected request/response shapes look like — all backed by real, passing tests rather than potentially stale wiki pages.

### CI summary integration

Enable `WriteCiSummary = true` on your `ReportConfigurationOptions` to surface test results and sequence diagrams directly in your **GitHub Actions job summary** or **Azure DevOps build summary**. The summary includes a pass/fail table, and when tests fail, the failed scenarios are shown with error messages, stack traces, and their sequence diagrams — giving you immediate visual context without downloading artifacts. When all tests pass, diagrams for the first N scenarios are shown as a quick validation. See the [CI Summary Integration](https://github.com/lemonlion/TestTrackingDiagrams/wiki/CI-Summary-Integration) wiki page for full details.

### CI artifact upload

Enable `PublishCiArtifacts = true` to automatically publish generated report files as CI artifacts. On **Azure DevOps**, reports are uploaded directly via `##vso[artifact.upload]` logging commands during test execution — no additional pipeline configuration needed. On **GitHub Actions**, the library writes the reports directory path and retention days to `$GITHUB_OUTPUT` so you can add a single `upload-artifact` step to your workflow. Artifact retention defaults to 1 day (`CiArtifactRetentionDays`). See the [CI Artifact Upload](https://github.com/lemonlion/TestTrackingDiagrams/wiki/CI-Artifact-Upload) wiki page for configuration and workflow examples.

---

## <a name="deterministic-vs-ai"></a>Deterministic vs AI-Generated Diagrams [↑](#top)

A key advantage of these diagrams is that they are **deterministic** — they are derived directly from actual interactions captured during test execution (HTTP traffic, database queries, cache commands, events, method calls), not generated by an AI model. AI-generated diagrams are non-deterministic by nature: they vary between runs, may hallucinate service interactions that don't exist, omit ones that do, or represent payloads inaccurately. The accuracy depends entirely on the model's understanding of your codebase, which is always incomplete.

Because TestTrackingDiagrams captures what actually happened at runtime, the output is a faithful, reproducible record of your system's behaviour. This makes the diagrams and PlantUML source especially valuable as **input to AI tools** — when you give an AI a deterministic, verified diagram as context, it can produce far more accurate outputs for:

- **Debugging** — The AI sees the exact chain of interactions that led to a failure, rather than guessing from code paths
- **Code understanding** — The AI can reason about concrete service interactions instead of inferring them from scattered registrations and handler code
- **Diagram generation** — The AI can aggregate verified low-level sequence diagrams into accurate high-level architecture diagrams, C4 models, or integration maps
- **Documentation** — The AI can write accurate API behaviour descriptions grounded in real data rather than its own interpretation of the source code

In short: use deterministic diagrams as the source of truth, and let AI tools build on top of that truth rather than trying to reconstruct it.

---

## <a name="supported-frameworks"></a>Supported Frameworks & NuGet Packages [↑](#top)

| Framework | Package | Test Runner | NuGet |
|---|---|---|---|
| **Core library** | `TestTrackingDiagrams` | — | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams)](https://www.nuget.org/packages/TestTrackingDiagrams) |
| **xUnit v3** | `TestTrackingDiagrams.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.xUnit3)](https://www.nuget.org/packages/TestTrackingDiagrams.xUnit3) |
| **xUnit v2** | `TestTrackingDiagrams.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.xUnit2)](https://www.nuget.org/packages/TestTrackingDiagrams.xUnit2) |
| **NUnit** | `TestTrackingDiagrams.NUnit4` | NUnit v4 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.NUnit4)](https://www.nuget.org/packages/TestTrackingDiagrams.NUnit4) |
| **MSTest** | `TestTrackingDiagrams.MSTest` | MSTest v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.MSTest)](https://www.nuget.org/packages/TestTrackingDiagrams.MSTest) |
| **TUnit** | `TestTrackingDiagrams.TUnit` | TUnit | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.TUnit)](https://www.nuget.org/packages/TestTrackingDiagrams.TUnit) |
| **BDDfy** | `TestTrackingDiagrams.BDDfy.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.BDDfy.xUnit3)](https://www.nuget.org/packages/TestTrackingDiagrams.BDDfy.xUnit3) |
| **LightBDD** | `TestTrackingDiagrams.LightBDD.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.LightBDD.xUnit2)](https://www.nuget.org/packages/TestTrackingDiagrams.LightBDD.xUnit2) |
| **LightBDD** | `TestTrackingDiagrams.LightBDD.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.LightBDD.xUnit3)](https://www.nuget.org/packages/TestTrackingDiagrams.LightBDD.xUnit3) |
| **LightBDD** | `TestTrackingDiagrams.LightBDD.TUnit` | TUnit | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.LightBDD.TUnit)](https://www.nuget.org/packages/TestTrackingDiagrams.LightBDD.TUnit) |
| **ReqNRoll** | `TestTrackingDiagrams.ReqNRoll.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.ReqNRoll.xUnit2)](https://www.nuget.org/packages/TestTrackingDiagrams.ReqNRoll.xUnit2) |
| **ReqNRoll** | `TestTrackingDiagrams.ReqNRoll.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.ReqNRoll.xUnit3)](https://www.nuget.org/packages/TestTrackingDiagrams.ReqNRoll.xUnit3) |
| **ReqNRoll** | `TestTrackingDiagrams.ReqNRoll.TUnit` | TUnit | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.ReqNRoll.TUnit)](https://www.nuget.org/packages/TestTrackingDiagrams.ReqNRoll.TUnit) |

### Extensions

| Extension | Package | Description | NuGet |
|---|---|---|---|
| **CosmosDB** | `TestTrackingDiagrams.Extensions.CosmosDB` | Tracks Azure Cosmos DB SDK operations with classified labels (Create, Read, Query, etc.) and configurable verbosity | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.Extensions.CosmosDB)](https://www.nuget.org/packages/TestTrackingDiagrams.Extensions.CosmosDB) |
| **EF Core Relational** | `TestTrackingDiagrams.Extensions.EfCore.Relational` | Tracks SQL operations from any EF Core relational provider (SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Spanner) with classified labels and configurable verbosity | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.Extensions.EfCore.Relational)](https://www.nuget.org/packages/TestTrackingDiagrams.Extensions.EfCore.Relational) |
| **Redis** | `TestTrackingDiagrams.Extensions.Redis` | Tracks StackExchange.Redis operations with cache hit/miss visualization, classified labels (Get, Set, Delete, Hash, List, Set, etc.) and configurable verbosity | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.Extensions.Redis)](https://www.nuget.org/packages/TestTrackingDiagrams.Extensions.Redis) |
| **PlantUML IKVM** | `TestTrackingDiagrams.PlantUml.Ikvm` | Local PlantUML rendering via IKVM — no remote server or Java installation required. Supports file-based and inline base64 images | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.PlantUml.Ikvm)](https://www.nuget.org/packages/TestTrackingDiagrams.PlantUml.Ikvm) |

All packages from 1.23.X onwards target **.NET 10.0** .

---

## <a name="documentation"></a>Documentation [↑](#top)

For full documentation including quick start guides, configuration, customisation, and API reference, see the **[Wiki](https://github.com/lemonlion/TestTrackingDiagrams/wiki)**.

Key pages:
- [Quick Start (xUnit)](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Quick-Start-(xUnit))
- [Framework Integration Guides](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Framework-Integration-Guides)
- [CosmosDB Extension](docs/integration-cosmosdb.md)
- [EF Core Relational Extension](docs/integration-efcore-relational.md)
- [Redis Extension](docs/integration-redis.md)
- [PlantUML IKVM (Local Rendering)](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Integration-PlantUML-IKVM)
- [HTTP Tracking Setup](https://github.com/lemonlion/TestTrackingDiagrams/wiki/HTTP-Tracking-Setup)
- [Diagram Customisation](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Diagram-Customisation)
- [Report Configuration](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Report-Configuration)
- [API Reference](https://github.com/lemonlion/TestTrackingDiagrams/wiki/API-Reference)
- [Example Project](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Example-Project)
