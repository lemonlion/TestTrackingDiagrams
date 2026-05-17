[![NuGet](https://img.shields.io/nuget/v/Kronikol.svg)](https://www.nuget.org/packages/Kronikol)
[![CI](https://github.com/lemonlion/Kronikol/actions/workflows/ci.yml/badge.svg)](https://github.com/lemonlion/Kronikol/actions/workflows/ci.yml)


<a name="top"></a>

# <img src="icon.svg" width="32" height="32" alt="Kronikol icon" style="vertical-align: middle;"> Kronikol (Formerly TestTrackingDiagrams)

Automatically generates [rich interactive HTML reports](https://lemonlion.github.io/BreakfastProvider/) with PlantUML sequence, activity and component diagrams, with timeline visualizations from your integration/component test HTTP traffic (real or faked) and telemetry.

Tracks interactions between your test caller, your Service Under Test (SUT), and its dependencies — including HTTP calls, Azure Cosmos DB operations, SQL queries (via EF Core), Redis commands, events/messages, and arbitrary method calls — then converts them into sequence diagrams embedded in searchable HTML reports and YAML specification files.  Method flow within the SUT itself is turned into activity diagrams.  And the combination is turned into flame diagrams.  All the flows combined are turned into a C4 Component Diagram for your service.  A Scenario Timeline diagram allows you to visually compare the execution time of your services.

Input data sets (eg InlineData/MemberData/ClassData for xUnit, and equivalents in NUnit, TUnit & ReqNRoll & LightBDD) are automatically turned into dynamic clickable tables showing you the correct diagrams for each set of inputs.  Data is all collapsible, truncatable and toggleable at view time, allowing you to see from higher level to lower level at the click of a button.  Also contains features for tracking your assertions from FluentAssertions/Awesome assertions and displaying them in the reports in plain english.  

---

## Table of Contents

- [Example Output](#example-output)
- [How It Works](#how-it-works)
- [Use Cases](#use-cases)
- [Deterministic vs AI-Generated Diagrams](#deterministic-vs-ai)
- [Supported Frameworks & NuGet Packages](#supported-frameworks)
- [Documentation](#documentation)

---

## <a name="example-output"></a>Example Output [↑](#top)

**This is just a very simple static example, for a full, rich, interactive example, see the generated [BreakFastProvider diagrams](https://lemonlion.github.io/BreakfastProvider/) in the [BreakFastProvider](https://github.com/lemonlion//BreakfastProvider/) project.** 

[<img width="770" height="1017" alt="image" src="https://github.com/user-attachments/assets/43d48a00-ba37-4951-945c-dd75de64c2bb" />](https://www.plantuml.com/plantuml/uml/j5NDJjj04BvRyZiCBWSGSKa28c2520418PI2258F2A7DxjYnukn6utKYG7so7lf8VONkseO4DnKGgJvvPhwPRtxjtpz_7QMQaSx6YUkiJOX5OmOQrHDeoj1rsgb-JB3ZEl0LfoZrDwKHderedsF6Hn6fJ8eJbIY2Bpn47hPAwvcIkXy_8JGQfUQcW994WaRTA7yOWkqNXdGKomaZDeOPiSdtMEWXxDSDR2tC9DUnah3EBS_6-fGb6MuQ2w7EI8BNpWs1jrMOjZpeUCQCKhpukWxBj9BPU639NSV8N9kSlHEM94WUi1Hu_kewfivOFu9tYccAfE6Qr3GM9KWKoXVT77sYPj17ciOSYsXgLefpJDVs40QaHcMqlAd7kMnpAZ80lrEb2U2yUnl0zZXEHgvJCLhydEqjTAu7VrdOgqlNaNQe54T3xJfbYoDYZvjtfqpZOK_96ZHteCS8clNc7ZJsWjrwK6_0UU_slk9XkP0EBp7LX4dLUajCfY6ItvLSYLX6XtoOoH4A0tITVAqycxONWDTNOtmu8qo73pshSXspBMQWOBDTqWBRWxnxVzVqgS3lZW2ZA5s5t_gzydUjy5dcC54PhKATEyvhpwMFarzVzIqxPoCiWoSOljlMsZ-PQnzhnv8bNxYMmDoQWMuK5sL0MfbDsoppVCYHPamsLBlz-kdgTDxaVimq7ru8cmORx30sE6ZvZHRd_cvJD7qMjfWVYi4-QxA3aDTtoymlP4GeOajWFE-AZzj27RL5JRKZK1a2m7sbxeKYbv_i3QOJ9GMALLPXi5B9vbJ_Pyb7vZN_0_q1003__mC0)

Each test that uses tracked dependencies automatically produces a sequence diagram (with matching PlantUML) showing the full request/response flow between services.

> **Tip:** You can visually separate the setup (arrange) phase from the action phase using the [`SeparateSetup`](https://github.com/lemonlion/Kronikol/wiki/Diagram-Customisation#setup-separation) flag.

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

    See the [Tracking Dependencies](https://github.com/lemonlion/Kronikol/wiki/Tracking-Dependencies) wiki page for setup guides.

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

Enable `WriteCiSummary = true` on your `ReportConfigurationOptions` to surface test results and sequence diagrams directly in your **GitHub Actions job summary** or **Azure DevOps build summary**. The summary includes a pass/fail table, and when tests fail, the failed scenarios are shown with error messages, stack traces, and their sequence diagrams — giving you immediate visual context without downloading artifacts. When all tests pass, diagrams for the first N scenarios are shown as a quick validation. See the [CI Summary Integration](https://github.com/lemonlion/Kronikol/wiki/CI-Summary-Integration) wiki page for full details.

### CI artifact upload

Enable `PublishCiArtifacts = true` to automatically publish generated report files as CI artifacts. On **Azure DevOps**, reports are uploaded directly via `##vso[artifact.upload]` logging commands during test execution — no additional pipeline configuration needed. On **GitHub Actions**, the library writes the reports directory path and retention days to `$GITHUB_OUTPUT` so you can add a single `upload-artifact` step to your workflow. Artifact retention defaults to 1 day (`CiArtifactRetentionDays`). See the [CI Artifact Upload](https://github.com/lemonlion/Kronikol/wiki/CI-Artifact-Upload) wiki page for configuration and workflow examples.

---

## <a name="deterministic-vs-ai"></a>Deterministic vs AI-Generated Diagrams [↑](#top)

A key advantage of these diagrams is that they are **deterministic** — they are derived directly from actual interactions captured during test execution (HTTP traffic, database queries, cache commands, events, method calls), not generated by an AI model. AI-generated diagrams are non-deterministic by nature: they vary between runs, may hallucinate service interactions that don't exist, omit ones that do, or represent payloads inaccurately. The accuracy depends entirely on the model's understanding of your codebase, which is always incomplete.

Because Kronikol captures what actually happened at runtime, the output is a faithful, reproducible record of your system's behaviour. This makes the diagrams and PlantUML source especially valuable as **input to AI tools** — when you give an AI a deterministic, verified diagram as context, it can produce far more accurate outputs for:

- **Debugging** — The AI sees the exact chain of interactions that led to a failure, rather than guessing from code paths
- **Code understanding** — The AI can reason about concrete service interactions instead of inferring them from scattered registrations and handler code
- **Diagram generation** — The AI can aggregate verified low-level sequence diagrams into accurate high-level architecture diagrams, C4 models, or integration maps
- **Documentation** — The AI can write accurate API behaviour descriptions grounded in real data rather than its own interpretation of the source code

In short: use deterministic diagrams as the source of truth, and let AI tools build on top of that truth rather than trying to reconstruct it.

---

## <a name="supported-frameworks"></a>Supported Frameworks & NuGet Packages [↑](#top)

| Framework | Package | Test Runner | NuGet |
|---|---|---|---|
| **Core library** | `Kronikol` | — | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol)](https://www.nuget.org/packages/Kronikol) |
| **xUnit v3** | `Kronikol.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.xUnit3)](https://www.nuget.org/packages/Kronikol.xUnit3) |
| **xUnit v2** | `Kronikol.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.xUnit2)](https://www.nuget.org/packages/Kronikol.xUnit2) |
| **NUnit** | `Kronikol.NUnit4` | NUnit v4 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.NUnit4)](https://www.nuget.org/packages/Kronikol.NUnit4) |
| **MSTest** | `Kronikol.MSTest` | MSTest v3 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.MSTest)](https://www.nuget.org/packages/Kronikol.MSTest) |
| **TUnit** | `Kronikol.TUnit` | TUnit | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.TUnit)](https://www.nuget.org/packages/Kronikol.TUnit) |
| **BDDfy** | `Kronikol.BDDfy.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.BDDfy.xUnit3)](https://www.nuget.org/packages/Kronikol.BDDfy.xUnit3) |
| **LightBDD** | `Kronikol.LightBDD.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.LightBDD.xUnit2)](https://www.nuget.org/packages/Kronikol.LightBDD.xUnit2) |
| **LightBDD** | `Kronikol.LightBDD.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.LightBDD.xUnit3)](https://www.nuget.org/packages/Kronikol.LightBDD.xUnit3) |
| **LightBDD** | `Kronikol.LightBDD.TUnit` | TUnit | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.LightBDD.TUnit)](https://www.nuget.org/packages/Kronikol.LightBDD.TUnit) |
| **ReqNRoll** | `Kronikol.ReqNRoll.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.ReqNRoll.xUnit2)](https://www.nuget.org/packages/Kronikol.ReqNRoll.xUnit2) |
| **ReqNRoll** | `Kronikol.ReqNRoll.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.ReqNRoll.xUnit3)](https://www.nuget.org/packages/Kronikol.ReqNRoll.xUnit3) |
| **ReqNRoll** | `Kronikol.ReqNRoll.TUnit` | TUnit | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.ReqNRoll.TUnit)](https://www.nuget.org/packages/Kronikol.ReqNRoll.TUnit) |

### Extensions

| Extension | Package | Description | NuGet |
|---|---|---|---|
| **CosmosDB** | `Kronikol.Extensions.CosmosDB` | Tracks Azure Cosmos DB SDK operations with classified labels (Create, Read, Query, etc.) and configurable verbosity | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.Extensions.CosmosDB)](https://www.nuget.org/packages/Kronikol.Extensions.CosmosDB) |
| **EF Core Relational** | `Kronikol.Extensions.EfCore.Relational` | Tracks SQL operations from any EF Core relational provider (SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Spanner) with classified labels and configurable verbosity | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.Extensions.EfCore.Relational)](https://www.nuget.org/packages/Kronikol.Extensions.EfCore.Relational) |
| **Redis** | `Kronikol.Extensions.Redis` | Tracks StackExchange.Redis operations with cache hit/miss visualization, classified labels (Get, Set, Delete, Hash, List, Set, etc.) and configurable verbosity | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.Extensions.Redis)](https://www.nuget.org/packages/Kronikol.Extensions.Redis) |
| **Blob Storage** | `Kronikol.Extensions.BlobStorage` | Tracks Azure Blob Storage operations in your tests and converts them into diagrams | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.Extensions.BlobStorage)](https://www.nuget.org/packages/Kronikol.Extensions.BlobStorage) |
| **DispatchProxy** | `Kronikol.Extensions.DispatchProxy` | DI integration helpers for `TrackingProxy<T>` — provides `ReplaceWithTracked<T>()` extension method for `IServiceCollection` to replace service registrations with tracking proxies | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.Extensions.DispatchProxy)](https://www.nuget.org/packages/Kronikol.Extensions.DispatchProxy) |
| **MediatR** | `Kronikol.Extensions.MediatR` | Wraps `IMediator` and `ISender` with `TrackingProxy` to record Send, Publish, and CreateStream calls with command/query type names as diagram labels | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.Extensions.MediatR)](https://www.nuget.org/packages/Kronikol.Extensions.MediatR) |
| **OpenTelemetry** | `Kronikol.Extensions.OpenTelemetry` | Captures internal SUT spans during tests for internal flow visualization in sequence diagram popups | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.Extensions.OpenTelemetry)](https://www.nuget.org/packages/Kronikol.Extensions.OpenTelemetry) |
| **PlantUML IKVM** | `Kronikol.PlantUml.Ikvm` | Local PlantUML rendering via IKVM — no remote server or Java installation required. Supports file-based and inline base64 images | [![NuGet Version](https://img.shields.io/nuget/v/Kronikol.PlantUml.Ikvm)](https://www.nuget.org/packages/Kronikol.PlantUml.Ikvm) |

All packages from 1.23.X onwards target **.NET 8.0**, **.NET 9.0**, and **.NET 10.0** (multi-target).

---

## <a name="documentation"></a>Documentation [↑](#top)

For full documentation including quick start guides, configuration, customisation, and API reference, see the **[Wiki](https://github.com/lemonlion/Kronikol/wiki)**.

Key pages:
- [Quick Start (xUnit)](https://github.com/lemonlion/Kronikol/wiki/Quick-Start-(xUnit))
- [Framework Integration Guides](https://github.com/lemonlion/Kronikol/wiki/Framework-Integration-Guides)
- [CosmosDB Extension](https://github.com/lemonlion/Kronikol/wiki/Integration-CosmosDB-Extension)
- [EF Core Relational Extension](https://github.com/lemonlion/Kronikol/wiki/Integration-EF-Core-Relational-Extension)
- [Redis Extension](https://github.com/lemonlion/Kronikol/wiki/Integration-Redis-Extension)
- [Blob Storage Extension](https://github.com/lemonlion/Kronikol/wiki/Integration-BlobStorage-Extension)
- [DispatchProxy Extension](https://github.com/lemonlion/Kronikol/wiki/Integration-DispatchProxy-Extension)
- [MediatR Extension](https://github.com/lemonlion/Kronikol/wiki/Integration-MediatR-Extension)
- [OpenTelemetry Extension](https://github.com/lemonlion/Kronikol/wiki/Integration-OpenTelemetry-Extension)
- [PlantUML IKVM (Local Rendering)](https://github.com/lemonlion/Kronikol/wiki/Integration-PlantUML-IKVM)
- [HTTP Tracking Setup](https://github.com/lemonlion/Kronikol/wiki/HTTP-Tracking-Setup)
- [Diagram Customisation](https://github.com/lemonlion/Kronikol/wiki/Diagram-Customisation)
- [Report Configuration](https://github.com/lemonlion/Kronikol/wiki/Report-Configuration)
- [API Reference](https://github.com/lemonlion/Kronikol/wiki/API-Reference)
- [Example Project](https://github.com/lemonlion/Kronikol/wiki/Example-Project)
