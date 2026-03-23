<a name="top"></a>

# Test Tracking Diagrams

Effortlessly autogenerate **PlantUML sequence diagrams** from your component and acceptance tests every time you run them. Tracks the HTTP requests between your test caller, your Service Under Test (SUT), and your SUT dependencies, then converts them into diagrams embedded in searchable HTML reports and YAML specification files.

---

## Table of Contents

- [Example Output](#example-output)
- [Supported Frameworks](#supported-frameworks)
- [NuGet Packages](#nuget-packages)
- [How It Works](#how-it-works)
- [Quick Start (xUnit)](#quick-start)
  - [1. Install Packages](#qs-install)
  - [2. Create the Test Run Fixture](#qs-test-run)
  - [3. Define the Test Collection](#qs-collection)
  - [4. Create a Base Fixture](#qs-base-fixture)
  - [5. Write a Test](#qs-write-test)
  - [6. Run and View Reports](#qs-run)
- [Framework Integration Guides](#integration-guides)
  - [xUnit](#integration-xunit)
  - [NUnit](#integration-nunit)
  - [BDDfy + xUnit v3](#integration-bddfy)
  - [LightBDD + xUnit v2](#integration-lightbdd)
  - [ReqNRoll + xUnit v2](#integration-reqnroll-xunit2)
  - [ReqNRoll + xUnit v3](#integration-reqnroll-xunit3)
- [HTTP Tracking Setup](#http-tracking)
  - [Registering Tracking in DI](#tracking-di)
  - [TestTrackingMessageHandlerOptions](#tracking-options)
  - [Creating a Tracking Client](#tracking-client)
- [Generated Reports](#generated-reports)
  - [ComponentSpecifications.yml](#report-yaml)
  - [ComponentSpecificationsWithExamples.html](#report-html-specs)
  - [FeaturesReport.html](#report-html-run)
  - [Report Features](#report-features)
- [Report Configuration](#report-configuration)
  - [ReportConfigurationOptions](#report-config-options)
- [Diagram Customisation](#diagram-customisation)
  - [TrackingDiagramOverride API](#override-api)
  - [StartOverride / EndOverride](#override-start-end)
  - [InsertPlantUml](#override-insert)
  - [InsertTestDelimiter](#override-delimiter)
  - [StartAction](#override-start-action)
  - [Setup Separation](#setup-separation)
- [Content Formatting](#content-formatting)
  - [DiagramsFetcherOptions](#fetcher-options)
  - [Pre- and Post-Formatting Processors](#formatting-processors)
- [Tags and Attributes](#tags-and-attributes)
  - [Happy Path](#tag-happy-path)
  - [Endpoint](#tag-endpoint)
- [Excluding Requests from Diagrams](#excluding-requests)
- [Excluded Headers](#excluded-headers)
- [Event Annotations](#event-annotations)
- [Large Response and Diagram Handling](#large-responses)
- [PlantUML Server Configuration](#plantuml-server)
- [Example Project](#example-project)
- [API Reference](#api-reference)
  - [Core Library](#api-core)
  - [Integration Libraries](#api-integrations)

---

## <a name="example-output"></a>Example Output [↑](#top)

![d5J1Qjj04Bqlx3zCHGuDYQZakB4ZwX18tD26a80DFOGIrhiZMrZQ3QELJGZziXxwIVs5QYKv8cgAwzoqxysyD_Eqqk-VlyvoAyWMMSePP_aoqG-2H0Ph4W_V4cKNC0m2pjx5XPOsCHhsf4aDlTzRYRJ0_P8Xj0Lfy3vLHEGTrDI11wTUIQDMcrxEH66DBh8P4eJEJqXh2BnpaQP87eWSPF-1](https://github.com/user-attachments/assets/4027c3db-4799-4612-a12a-3de68f4f557c)

Each test that makes HTTP calls through the tracked pipeline automatically produces a sequence diagram showing the full request/response flow between services.

---

## <a name="supported-frameworks"></a>Supported Frameworks [↑](#top)

| Framework | Package | Test Runner |
|---|---|---|
| **xUnit** | `TestTrackingDiagrams.XUnit` | xUnit v3 |
| **NUnit** | `TestTrackingDiagrams.NUnit4` | NUnit v4 |
| **BDDfy** | `TestTrackingDiagrams.BDDfy.xUnit3` | xUnit v3 |
| **LightBDD** | `TestTrackingDiagrams.LightBDD.xUnit2` | xUnit v2 |
| **ReqNRoll** | `TestTrackingDiagrams.ReqNRoll.xUnit2` | xUnit v2 |
| **ReqNRoll** | `TestTrackingDiagrams.ReqNRoll.xUnit3` | xUnit v3 |

All packages target **.NET 8.0**.

---

## <a name="nuget-packages"></a>NuGet Packages [↑](#top)

| Package | NuGet |
|---|---|
| Core library | [TestTrackingDiagrams](https://www.nuget.org/packages/TestTrackingDiagrams) |
| xUnit | [TestTrackingDiagrams.XUnit](https://www.nuget.org/packages/TestTrackingDiagrams.XUnit) |
| NUnit | [TestTrackingDiagrams.NUnit4](https://www.nuget.org/packages/TestTrackingDiagrams.NUnit4) |
| BDDfy + xUnit v3 | [TestTrackingDiagrams.BDDfy.xUnit3](https://www.nuget.org/packages/TestTrackingDiagrams.BDDfy.xUnit3) |
| LightBDD + xUnit v2 | [TestTrackingDiagrams.LightBDD.xUnit2](https://www.nuget.org/packages/TestTrackingDiagrams.LightBDD.xUnit2) |
| ReqNRoll + xUnit v2 | [TestTrackingDiagrams.ReqNRoll.xUnit2](https://www.nuget.org/packages/TestTrackingDiagrams.ReqNRoll.xUnit2) |
| ReqNRoll + xUnit v3 | [TestTrackingDiagrams.ReqNRoll.xUnit3](https://www.nuget.org/packages/TestTrackingDiagrams.ReqNRoll.xUnit3) |

---

## <a name="how-it-works"></a>How It Works [↑](#top)

```
┌─────────────┐     HTTP      ┌─────────────┐     HTTP      ┌─────────────┐
│  Test Code  │ ──────────►   │     SUT     │ ──────────►   │ Dependency  │
│  (Caller)   │ ◄──────────   │  (Your API) │ ◄──────────   │  (Fakes)    │
└─────────────┘               └─────────────┘               └─────────────┘
       │                             │                             │
       └───────────── All HTTP traffic is intercepted ─────────────┘
                                     │
                                     ▼
                          ┌─────────────────────┐
                          │ RequestResponseLogger│
                          │  (in-memory log)     │
                          └──────────┬──────────┘
                                     │
                                     ▼
                          ┌─────────────────────┐
                          │   PlantUmlCreator    │
                          │ (generates PlantUML) │
                          └──────────┬──────────┘
                                     │
                                     ▼
                          ┌─────────────────────┐
                          │   ReportGenerator    │
                          │  (HTML + YAML files) │
                          └─────────────────────┘
```

1. **Intercept** — A `TestTrackingMessageHandler` (a `DelegatingHandler`) is inserted into the HTTP pipeline. It logs every request and response, enriching them with tracking headers (test name, test ID, trace ID, caller name).

2. **Collect** — All logged `RequestResponseLog` entries are held in the static `RequestResponseLogger`. Each entry captures the method, URI, headers, body, status code, service names, and a trace ID to correlate requests across services.

3. **Generate** — At the end of the test run, `PlantUmlCreator` groups logs by test ID and converts them into PlantUML sequence diagram code. The code is encoded and rendered via a PlantUML server.

4. **Report** — `ReportGenerator` combines the diagrams with test metadata (features, scenarios, results, BDD steps) to produce three output files: a YAML specification, an HTML specification with diagrams, and an HTML test run report.

---

## <a name="quick-start"></a>Quick Start (xUnit) [↑](#top)

This is the simplest integration path. For other frameworks see the [Integration Guides](#integration-guides) section.

### <a name="qs-install"></a>1. Install Packages [↑](#top)

```bash
dotnet add package TestTrackingDiagrams.XUnit
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

### <a name="qs-test-run"></a>2. Create the Test Run Fixture [↑](#top)

The test run fixture is a collection fixture that lives for the entire test run. Reports are generated in its `Dispose` method.

```csharp
using TestTrackingDiagrams;
using TestTrackingDiagrams.XUnit;

public class TestRun : DiagrammedTestRun, IDisposable
{
    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;
        XUnitReportGenerator.CreateStandardReportsWithDiagrams(
            TestContexts, StartRunTime, EndRunTime,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "My API Specifications"
            });
    }
}
```

### <a name="qs-collection"></a>3. Define the Test Collection [↑](#top)

```csharp
[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun>;
```

### <a name="qs-base-fixture"></a>4. Create a Base Fixture [↑](#top)

The base fixture creates a `WebApplicationFactory` with HTTP tracking wired in, and provides each test with its own `HttpClient`.

```csharp
using TestTrackingDiagrams.XUnit;

public class BaseFixture : DiagrammedComponentTest
{
    private static readonly WebApplicationFactory<Program> Factory;

    static BaseFixture()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Track HTTP calls FROM the SUT to its dependencies
                    services.TrackDependenciesForDiagrams(
                        new XUnitTestTrackingMessageHandlerOptions
                        {
                            CallingServiceName = "My API",
                            PortsToServiceNames =
                            {
                                { 80, "My API" },
                                { 5001, "Downstream Service" }
                            }
                        });
                });
            });
    }

    // Track HTTP calls TO the SUT from the test
    protected HttpClient Client { get; } = Factory.CreateTestTrackingClient(
        new XUnitTestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "My API"
        });
}
```

### <a name="qs-write-test"></a>5. Write a Test [↑](#top)

```csharp
[Endpoint("/api/cake")]
public class CakeFeature : BaseFixture
{
    [Fact, HappyPath]
    public async Task Creating_a_cake_successfully()
    {
        var response = await Client.PostAsJsonAsync("/api/cake",
            new { Milk = "whole", Eggs = "free-range", Flour = "plain" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### <a name="qs-run"></a>6. Run and View Reports [↑](#top)

```bash
dotnet test
```

After the tests complete, open the three generated files in `bin\Debug\net8.0\Reports\`:

| File | Description |
|---|---|
| `ComponentSpecifications.yml` | YAML specification document |
| `ComponentSpecificationsWithExamples.html` | HTML specification with embedded diagrams |
| `FeaturesReport.html` | HTML test run report with diagrams and execution summary |

---

## <a name="integration-guides"></a>Framework Integration Guides [↑](#top)

Each framework has a complete, step-by-step integration guide. The sections below summarise the key concepts for each; follow the links for the full walkthrough.

### <a name="integration-xunit"></a>xUnit [↑](#top)

**Full guide:** [docs/integration-xunit.md](docs/integration-xunit.md)

The simplest integration path. Uses xUnit v3 collection fixtures to manage the test lifecycle.

**Key components:**
- `DiagrammedTestRun` — Base class for collection fixture; tracks `StartRunTime`/`EndRunTime` and holds a `ConcurrentQueue<ITestContext>`
- `DiagrammedComponentTest` — Base class for test classes; automatically enqueues `TestContext.Current` on dispose
- `XUnitReportGenerator.CreateStandardReportsWithDiagrams()` — Generates all three report files
- `[HappyPath]` and `[Endpoint("/path")]` attributes for test metadata

---

### <a name="integration-nunit"></a>NUnit [↑](#top)

**Full guide:** [docs/integration-nunit4.md](docs/integration-nunit4.md)

Uses NUnit 4's `[SetUpFixture]` for test lifecycle management.

**Key components:**
- `DiagrammedTestRun` — Base class for `[SetUpFixture]` (must be **outside any namespace** for global fixture)
- `DiagrammedComponentTest` — Base class for test fixtures; captures `TestContext.CurrentContext` in `[TearDown]`
- `NUnitReportGenerator.CreateStandardReportsWithDiagrams()` — Generates all three report files
- `[HappyPath]` and `[Endpoint("/path")]` attributes (NUnit `PropertyAttribute` based)

> **Important:** Use `[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]` to ensure each test gets a fresh instance.

---

### <a name="integration-bddfy"></a>BDDfy + xUnit v3 [↑](#top)

**Full guide:** [docs/integration-bddfy-xunit3.md](docs/integration-bddfy-xunit3.md)

BDD with BDDfy's fluent API (`this.Given(...).When(...).Then(...).BDDfy()`). Diagrams are injected into both the BDDfy native HTML report and the TestTrackingDiagrams reports.

**Key components:**
- `BDDfyDiagramsConfigurator.Configure()` — Must be called before tests run; registers BDDfy processors
- `BDDfyScenarioCollector` — Thread-safe collector for scenario metadata
- `BDDfyReportGenerator.CreateStandardReportsWithDiagrams()` — Generates reports with BDD step details
- Tags: `"happy-path"`, `"endpoint:/path"` passed via `.WithTags()`

> **Note:** For xUnit v3, set `<OutputType>Exe</OutputType>` in the test project's csproj.

---

### <a name="integration-lightbdd"></a>LightBDD + xUnit v2 [↑](#top)

**Full guide:** [docs/integration-lightbdd-xunit2.md](docs/integration-lightbdd-xunit2.md)

BDD with LightBDD's C# scenario runner (`Runner.RunScenarioAsync(given => ..., when => ..., then => ...)`). Uses xUnit v2.

**Key components:**
- `ReportWritersConfiguration.CreateStandardReportsWithDiagrams()` — Fluent extension for LightBDD's report writer pipeline
- `LightBddTestTrackingMessageHandlerOptions` — Auto-fetches test info from `ScenarioExecutionContext`
- `LightBddDiagramsFetcher` — Wraps diagrams with LightBDD's `DiagramAsCode` type

> **Note:** Requires `[assembly: ClassCollectionBehavior(AllowTestParallelization = false)]`.

---

### <a name="integration-reqnroll-xunit2"></a>ReqNRoll + xUnit v2 [↑](#top)

**Full guide:** [docs/integration-reqnroll-xunit2.md](docs/integration-reqnroll-xunit2.md)

Gherkin-based BDD with `.feature` files and `[Binding]` step definitions.

**Key components:**
- `ReqNRollTrackingHooks` — Auto-registered via `reqnroll.json` binding assembly; captures scenario IDs, steps, and results
- `ReqNRollReportGenerator.CreateStandardReportsWithDiagrams()` — Generates reports with Gherkin step details
- `ReqNRollReportEnhancer` — Additionally enhances ReqNRoll's native HTML report with sequence diagram attachments (when the `html` formatter is enabled in `reqnroll.json`). The standard custom reports are still generated as before
- Tags: `@happy-path`, `@endpoint:/path` in feature files

> **Critical:** Add to `reqnroll.json`:
> ```json
> {
>   "bindingAssemblies": [
>     { "assembly": "TestTrackingDiagrams.ReqNRoll.xUnit2" }
>   ],
>   "formatters": {
>     "html": { "outputFilePath": "reqnroll_report.html" }
>   }
> }
> ```

---

### <a name="integration-reqnroll-xunit3"></a>ReqNRoll + xUnit v3 [↑](#top)

**Full guide:** [docs/integration-reqnroll-xunit3.md](docs/integration-reqnroll-xunit3.md)

Identical to the xUnit v2 variant, but targets the xUnit v3 out-of-process runner.

**Key differences from xUnit v2:**
- References `TestTrackingDiagrams.ReqNRoll.xUnit3` instead of `xUnit2`
- Requires `<OutputType>Exe</OutputType>` in the test project's csproj
- `reqnroll.json` references `TestTrackingDiagrams.ReqNRoll.xUnit3`

> **ReqNRoll HTML report:** Both ReqNRoll packages additionally enhance ReqNRoll's native HTML report with sequence diagrams when the `html` formatter is enabled in `reqnroll.json`. This is in addition to the standard custom reports that are always generated in the Reports directory. See the [ReqNRoll xUnit2 integration guide](docs/integration-reqnroll-xunit2.md) for details.

---

## <a name="http-tracking"></a>HTTP Tracking Setup [↑](#top)

HTTP tracking is the core mechanism that captures all HTTP traffic for diagram generation. There are two sides to track:

1. **Outgoing calls from the SUT** — Calls your API makes to its downstream dependencies
2. **Incoming calls to the SUT** — Calls your test makes to the API under test

### <a name="tracking-di"></a>Registering Tracking in DI [↑](#top)

Inside your `WebApplicationFactory` configuration, register tracking for **outgoing** calls from the SUT:

```csharp
builder.ConfigureServices(services =>
{
    services.TrackDependenciesForDiagrams(
        new XUnitTestTrackingMessageHandlerOptions
        {
            CallingServiceName = "My API",
            PortsToServiceNames =
            {
                { 80, "My API" },
                { 5001, "Auth Service" },
                { 5002, "Payment Service" }
            }
        });
});
```

This registers:
- `TestTrackingMessageHandlerOptions` as a singleton
- `IHttpContextAccessor` for extracting tracking headers from incoming requests
- `IHttpClientFactory` as `TestTrackingHttpClientFactory` (returns `HttpClient` instances with the tracking `DelegatingHandler`)

### <a name="tracking-options"></a>TestTrackingMessageHandlerOptions [↑](#top)

| Property | Type | Default | Description |
|---|---|---|---|
| `PortsToServiceNames` | `Dictionary<int, string>` | `{}` | Maps port numbers to human-readable service names for the diagram. When the SUT makes an HTTP call to `localhost:5001`, the diagram will show the target as the mapped name. Unmapped ports will appear as `localhost_80`, `localhost_5001`, etc. |
| `FixedNameForReceivingService` | `string?` | `null` | If set, all requests handled by this handler are labelled with this fixed name (useful for the test-to-SUT client). |
| `CallingServiceName` | `string` | `"Caller"` | The name shown in the diagram for the service making the call. |
| `HeadersToForward` | `IEnumerable<string>` | `[]` | Additional HTTP headers to forward from incoming requests to outgoing requests (propagated through the call chain). |
| `CurrentTestInfoFetcher` | `Func<(string Name, string Id)>?` | `null` | Delegate that returns the current test's name and unique ID. Each framework-specific options class sets this automatically. |

> **Framework-specific options classes** (`XUnitTestTrackingMessageHandlerOptions`, `NUnitTestTrackingMessageHandlerOptions`, `BDDfyTestTrackingMessageHandlerOptions`, `LightBddTestTrackingMessageHandlerOptions`, `ReqNRollTestTrackingMessageHandlerOptions`) extend `TestTrackingMessageHandlerOptions` and auto-populate `CurrentTestInfoFetcher` from the framework's test context.

### <a name="tracking-client"></a>Creating a Tracking Client [↑](#top)

To track **incoming** calls from the test to the SUT, create the test's `HttpClient` via the `CreateTestTrackingClient()` extension method:

```csharp
HttpClient client = factory.CreateTestTrackingClient(
    new XUnitTestTrackingMessageHandlerOptions
    {
        FixedNameForReceivingService = "My API"
    });
```

This wraps the `WebApplicationFactory`'s `HttpClient` in a `TestTrackingMessageHandler` so that all calls from the test to the SUT appear in the diagrams.

---

## <a name="generated-reports"></a>Generated Reports [↑](#top)

After tests complete, three files are written to the `Reports` folder inside `bin\Debug\net8.0\` (configurable via `ReportsFolderPath`).

### <a name="report-yaml"></a>ComponentSpecifications.yml [↑](#top)

A YAML specification document listing all features and scenarios. The ReqNRoll and BDDfy adapters additionally include the BDD steps (Given/When/Then/And/But) for each scenario.

### <a name="report-html-specs"></a>ComponentSpecificationsWithExamples.html [↑](#top)

An HTML specification file with embedded sequence diagrams for each test scenario. Features and scenarios are grouped hierarchically.

> **Behaviour:** If any test in the run has failed, this file is written as blank. This prevents publishing stale or misleading specifications when the test suite is not fully passing.

### <a name="report-html-run"></a>FeaturesReport.html [↑](#top)

A full test run report including:
- **Execution summary** — Total pass/fail/skip counts and run duration
- **Sequence diagrams** — Embedded for each scenario
- **BDD steps** — Included when using ReqNRoll or BDDfy
- **Failure details** — Error messages and stack traces for failed tests

### <a name="report-features"></a>Report Features [↑](#top)

Both HTML reports include:

- **Search** — A search bar that filters features and scenarios by keyword. Supports quoted phrases for exact matching.
- **Happy path filter** — A toggle to show only scenarios marked as happy path.
- **Click-to-reveal PlantUML** — Click any diagram image to reveal the raw PlantUML code behind it.
- **Feature grouping** — Tests are grouped by feature (test class / story title / feature file).

---

## <a name="report-configuration"></a>Report Configuration [↑](#top)

### <a name="report-config-options"></a>ReportConfigurationOptions [↑](#top)

Pass a `ReportConfigurationOptions` record when generating reports to customise their output:

```csharp
new ReportConfigurationOptions
{
    SpecificationsTitle = "My API Specifications",
    PlantUmlServerBaseUrl = "https://plantuml.com/plantuml",
    HtmlSpecificationsFileName = "ComponentSpecificationsWithExamples",
    HtmlTestRunReportFileName = "FeaturesReport",
    YamlSpecificationsFileName = "ComponentSpecifications",
    ReportsFolderPath = "Reports",
    ExcludedHeaders = ["Authorization", "X-Api-Key"],
    HtmlSpecificationsCustomStyleSheet = "body { font-family: sans-serif; }",
    RequestResponsePostProcessor = content => content.Replace("secret", "***"),
    SeparateSetup = true,
    HighlightSetup = true
}
```

| Property | Type | Default | Description |
|---|---|---|---|
| `PlantUmlServerBaseUrl` | `string` | `"https://plantuml.com/plantuml"` | Base URL of the PlantUML server used to render diagrams. |
| `SpecificationsTitle` | `string` | `"Service Specifications"` | Title shown at the top of HTML specification reports. |
| `HtmlSpecificationsFileName` | `string` | `"ComponentSpecificationsWithExamples"` | File name (without extension) for the HTML specifications file. |
| `HtmlTestRunReportFileName` | `string` | `"FeaturesReport"` | File name (without extension) for the HTML test run report. |
| `YamlSpecificationsFileName` | `string` | `"ComponentSpecifications"` | File name (without extension) for the YAML specifications file. |
| `HtmlSpecificationsCustomStyleSheet` | `string?` | `null` | Custom CSS injected into the HTML specifications file. When set, replaces the default stylesheet. |
| `ReportsFolderPath` | `string` | `"Reports"` | Folder path (relative to `AppDomain.CurrentDomain.BaseDirectory`) where reports are written. |
| `ExcludedHeaders` | `string[]` | `[]` | HTTP headers to exclude from diagram notes. Combined with `PlantUmlCreator.DefaultExcludedHeaders` (`Cache-Control`, `Pragma`). |
| `RequestResponsePostProcessor` | `Func<string, string>?` | `null` | Post-processing function applied to both request and response content before rendering in diagrams. Useful for redacting sensitive data. |
| `SeparateSetup` | `bool` | `false` | When `true`, HTTP calls made before `StartAction()` (or before the first non-GIVEN BDD step) are wrapped in a visual "Setup" partition in the diagram. See [Setup Separation](#setup-separation). |
| `HighlightSetup` | `bool` | `true` | When `true` (and `SeparateSetup` is enabled), the setup partition is rendered with a background colour. When `false`, the partition has no background colour. Has no effect when `SeparateSetup` is `false`. |

---

## <a name="diagram-customisation"></a>Diagram Customisation [↑](#top)

The `TrackingDiagramOverride` static class (available in every integration package) lets you manually control what appears in the generated diagrams.

### <a name="override-api"></a>TrackingDiagramOverride API [↑](#top)

Each integration package provides a `TrackingDiagramOverride` class that wraps the core `DefaultTrackingDiagramOverride` and automatically resolves the current test ID from the framework's test context. The API is identical across all integrations:

```csharp
TrackingDiagramOverride.StartOverride(plantUml?);
TrackingDiagramOverride.EndOverride(plantUml?);
TrackingDiagramOverride.InsertPlantUml(plantUml);
TrackingDiagramOverride.InsertTestDelimiter(testIdentifier);
TrackingDiagramOverride.StartAction();
```

### <a name="override-start-end"></a>StartOverride / EndOverride [↑](#top)

Suppress automatic diagram generation for a section of your test. Any HTTP calls made between `StartOverride()` and `EndOverride()` will **not** appear in the diagram. Optionally inject custom PlantUML at the start/end boundaries:

```csharp
TrackingDiagramOverride.StartOverride("note across: Starting setup phase");

// These HTTP calls will NOT appear in the diagram
await client.PostAsync("/api/seed-data", content);

TrackingDiagramOverride.EndOverride("note across: Setup complete");
```

### <a name="override-insert"></a>InsertPlantUml [↑](#top)

Insert arbitrary PlantUML code into the diagram at the current position:

```csharp
TrackingDiagramOverride.InsertPlantUml("note across: User is now authenticated");
TrackingDiagramOverride.InsertPlantUml("== Phase 2: Order Processing ==");
```

Any valid PlantUML syntax can be injected — notes, dividers, delays, groups, etc.

### <a name="override-delimiter"></a>InsertTestDelimiter [↑](#top)

Insert a visual test boundary marker in the diagram. Useful when a single test logically contains multiple phases:

```csharp
TrackingDiagramOverride.InsertTestDelimiter("Verify Initial State");
// ... test code ...
TrackingDiagramOverride.InsertTestDelimiter("Verify After Update");
```

This renders as a black horizontal note across all participants with white text showing `Test {identifier}`.

> **LightBDD tip:** This is particularly useful when using LightBDD's [Tabular Parameters](https://github.com/LightBDD/LightBDD/wiki/Advanced-Step-Parameters#tabular-parameters) or [TabularAttributes](https://github.com/lemonlion/LightBdd.TabularAttributes), where a single scenario runs multiple iterations. Insert a delimiter between each iteration to clearly separate them in the diagram.

### <a name="override-start-action"></a>StartAction [↑](#top)

Explicitly mark the boundary between setup and action phases of a test. When `SeparateSetup` is enabled, all HTTP calls before `StartAction()` are rendered inside a "Setup" partition in the diagram:

```csharp
// Setup phase — these calls appear inside the partition
await client.PostAsync("/api/seed-data", content);
await client.PostAsync("/api/configure", settings);

TrackingDiagramOverride.StartAction();

// Action phase — these calls appear after the partition
var response = await client.GetAsync("/api/cake");
```

> **BDD frameworks (BDDfy, LightBDD, ReqNRoll):** `StartAction()` is called automatically when the test transitions from a GIVEN step to a WHEN or THEN step. You only need to call it explicitly if you want to override the automatic detection or are using a non-BDD framework.

### <a name="setup-separation"></a>Setup Separation [↑](#top)

Setup separation visually distinguishes the "arrange" phase of a test from the "act" phase in the generated sequence diagrams by wrapping setup HTTP calls in a PlantUML partition block.

To enable it, set `SeparateSetup = true` on `ReportConfigurationOptions`:

```csharp
new ReportConfigurationOptions
{
    SeparateSetup = true,
    HighlightSetup = true // default — adds a background colour to the partition
}
```

| Property | Effect |
|---|---|
| `SeparateSetup = false` | No partition — all calls rendered sequentially (default) |
| `SeparateSetup = true, HighlightSetup = true` | Setup calls wrapped in a coloured partition |
| `SeparateSetup = true, HighlightSetup = false` | Setup calls wrapped in a plain partition (no background colour) |

The boundary between setup and action is determined by:

1. **Explicit** — Call `TrackingDiagramOverride.StartAction()` in your test code
2. **Implicit (BDD frameworks)** — Automatically detected when the test transitions from a GIVEN step to a WHEN or THEN step (supported in BDDfy, LightBDD, and ReqNRoll)

---

## <a name="content-formatting"></a>Content Formatting [↑](#top)

### <a name="fetcher-options"></a>DiagramsFetcherOptions [↑](#top)

For fine-grained control over how request/response bodies and headers are formatted in diagram notes, use `DiagramsFetcherOptions`:

```csharp
var options = new DiagramsFetcherOptions
{
    PlantUmlServerBaseUrl = "https://plantuml.com/plantuml",
    RequestPreFormattingProcessor = content => content,
    RequestPostFormattingProcessor = content => content,
    ResponsePreFormattingProcessor = content => content,
    ResponsePostFormattingProcessor = content => content,
    ExcludedHeaders = ["Authorization", "X-Api-Key"]
};
```

| Property | Type | Default | Description |
|---|---|---|---|
| `PlantUmlServerBaseUrl` | `string` | `"https://plantuml.com/plantuml"` | Base URL of the PlantUML server. |
| `RequestPreFormattingProcessor` | `Func<string, string>?` | `null` | Transform raw request body **before** the library formats it into the PlantUML note. |
| `RequestPostFormattingProcessor` | `Func<string, string>?` | `null` | Transform the formatted request note **after** the library has formatted it. |
| `ResponsePreFormattingProcessor` | `Func<string, string>?` | `null` | Transform raw response body **before** the library formats it into the PlantUML note. |
| `ResponsePostFormattingProcessor` | `Func<string, string>?` | `null` | Transform the formatted response note **after** the library has formatted it. |
| `ExcludedHeaders` | `IEnumerable<string>` | `[]` | HTTP headers to exclude from diagram notes. |
| `SeparateSetup` | `bool` | `false` | When `true`, HTTP calls made before `StartAction()` are wrapped in a visual "Setup" partition in the diagram. See [Setup Separation](#setup-separation). |
| `HighlightSetup` | `bool` | `true` | When `true` (and `SeparateSetup` is enabled), the setup partition is rendered with a background colour. When `false`, the partition has no background colour. |

### <a name="formatting-processors"></a>Pre- and Post-Formatting Processors [↑](#top)

The formatting pipeline for each request/response is:

```
Raw body → PreFormattingProcessor → Library formatting (JSON pretty-print, header layout) → PostFormattingProcessor → Diagram note
```

**Use cases:**
- **Pre-processor** — Deserialise/transform the body before the library formats it (e.g., decrypt or decompress)
- **Post-processor** — Redact sensitive data, shorten values, or adjust the final note text

```csharp
var options = new DiagramsFetcherOptions
{
    // Remove bearer tokens from the formatted output
    ResponsePostFormattingProcessor = note =>
        Regex.Replace(note, @"Bearer [A-Za-z0-9\-._~+/]+=*", "Bearer ***"),

    // Pretty-print XML before library formatting
    RequestPreFormattingProcessor = body =>
    {
        try { return XDocument.Parse(body).ToString(); }
        catch { return body; }
    }
};
```

---

## <a name="tags-and-attributes"></a>Tags and Attributes [↑](#top)

Tags control how scenarios are categorised and filtered in the generated reports.

### <a name="tag-happy-path"></a>Happy Path [↑](#top)

Mark a scenario as a "happy path" to support filtering in the HTML reports.

| Framework | How to mark |
|---|---|
| **xUnit** | `[HappyPath]` attribute on the test method |
| **NUnit** | `[HappyPath]` attribute on the test method |
| **BDDfy** | `.WithTags("happy-path")` |
| **LightBDD** | `[HappyPath]` attribute on the scenario method (from [LightBDD.Contrib.ReportingEnhancements](https://github.com/AdaskoTheBeAsT/LightBDD.Contrib.ReportingEnhancements)) |
| **ReqNRoll** | `@happy-path` tag on the Gherkin scenario |

In the HTML reports, use the **"Show only happy paths"** toggle to filter the view.

### <a name="tag-endpoint"></a>Endpoint [↑](#top)

Associate a feature/test class with an API endpoint. This appears as metadata in the reports.

| Framework | How to set |
|---|---|
| **xUnit** | `[Endpoint("/api/cake")]` attribute on the test class |
| **NUnit** | `[Endpoint("/api/cake")]` attribute on the test fixture class |
| **BDDfy** | `.WithTags("endpoint:/api/cake")` |
| **ReqNRoll** | `@endpoint:/api/cake` tag on the Gherkin `Feature` |

---

## <a name="excluding-requests"></a>Excluding Requests from Diagrams [↑](#top)

To exclude a specific HTTP request from appearing in the diagram, add the `test-tracking-ignore` header:

```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "/health");
request.Headers.Add("test-tracking-ignore", "true");
await client.SendAsync(request);
```

Any request carrying this header will still be sent normally but will be filtered out of the diagram generation pipeline.

---

## <a name="excluded-headers"></a>Excluded Headers [↑](#top)

HTTP headers can be excluded from appearing in diagram notes. There are two levels:

1. **Default exclusions** — `PlantUmlCreator.DefaultExcludedHeaders` always excludes `Cache-Control` and `Pragma`.
2. **Custom exclusions** — Set `ExcludedHeaders` on `ReportConfigurationOptions` or `DiagramsFetcherOptions` to exclude additional headers:

```csharp
new ReportConfigurationOptions
{
    ExcludedHeaders = ["Authorization", "X-Request-Id", "X-Correlation-Id"]
}
```

Both sets are combined when generating diagrams.

---

## <a name="event-annotations"></a>Event Annotations [↑](#top)

Requests can be marked with `RequestResponseMetaType.Event` to give them special styling in diagrams. Event-annotated notes are rendered with:

- A **light blue background** (`#cfecf7`)
- **Smaller font size** (11px)
- **Rounded corners**

This is useful for distinguishing asynchronous events, webhooks, or message bus interactions from standard HTTP request/response flows.

---

## <a name="large-responses"></a>Large Response and Diagram Handling [↑](#top)

The library automatically handles large content:

- **Response bodies over 15 KB** are split across multiple diagram segments with "Continued From Previous Diagram" / "Continued On Next Diagram" markers.
- **Encoded PlantUML exceeding 2,000 characters** triggers an automatic diagram split. The new diagram continues with the same participants and auto-numbered steps.
- **Long URLs exceeding 100 characters** are wrapped across multiple lines in the diagram.

This ensures diagrams remain renderable by the PlantUML server (which has URL length limits) while preserving all captured information.

---

## <a name="plantuml-server"></a>PlantUML Server Configuration [↑](#top)

By default, diagrams are rendered via the public PlantUML server at `https://plantuml.com/plantuml`. To use a private/self-hosted server, set `PlantUmlServerBaseUrl` in either `ReportConfigurationOptions` or `DiagramsFetcherOptions`:

```csharp
new ReportConfigurationOptions
{
    PlantUmlServerBaseUrl = "https://plantuml.mycompany.com/plantuml"
}
```

The library appends `/png/{encoded}` to this base URL to generate image source URLs.

---

## <a name="example-project"></a>Example Project [↑](#top)

The [`Example.Api`](https://github.com/lemonlion/TestTrackingDiagrams/tree/main/Example.Api) folder contains a complete working example:

- **`src/Example.Api`** — A simple dessert provider API with `CakeController`, `EggsController`, `FlourController`, `MilkController`
- **`fakes/`** — In-memory HTTP fakes for downstream dependencies (Cow Service)
- **`tests/`** — Test projects demonstrating every supported framework:
  - `Example.Api.Tests.Component.XUnit` — Plain xUnit
  - `Example.Api.Tests.Component.BDDfy.xUnit3` — BDDfy + xUnit v3
  - `Example.Api.Tests.Component.ReqNRoll.xUnit2` — ReqNRoll + xUnit v2
  - `Example.Api.Tests.Component.ReqNRoll.xUnit3` — ReqNRoll + xUnit v3
  - `Example.Api.Tests.Component.LightBDD.xUnit2` — LightBDD + xUnit v2
  - `Example.Api.Tests.Component.NUnit4` — NUnit

**To run the examples:**

```bash
cd Example.Api
dotnet test
```

Then browse to `tests\<project>\bin\Debug\net8.0\Reports\` to view the generated reports.

> **Note:** For the BDDfy integration, diagrams are also injected into the standard BDDfy-produced HTML report (`BDDfy.html`), in addition to the TestTrackingDiagrams reports.

---

## <a name="api-reference"></a>API Reference [↑](#top)

### <a name="api-core"></a>Core Library (`TestTrackingDiagrams`) [↑](#top)

| Class / Record | Description |
|---|---|
| `ReportConfigurationOptions` | Configuration for report file names, titles, PlantUML server, excluded headers, setup separation, and custom stylesheets. |
| `DiagramsFetcherOptions` | Configuration for diagram content formatting processors, header exclusions, and setup separation. |
| `DefaultDiagramsFetcher` | Static class; `GetDiagramsFetcher(options?)` returns a `Func<DiagramAsCode[]>` that fetches all generated diagrams. |
| `DefaultDiagramsFetcher.DiagramAsCode` | Record containing `TestRuntimeId`, `ImgSrc` (rendered PNG URL), and `CodeBehind` (raw PlantUML). |
| `DefaultTrackingDiagramOverride` | Static class for manual diagram control: `StartOverride`, `EndOverride`, `InsertPlantUml`, `InsertTestDelimiter`, `StartAction`. |
| `TestTrackingMessageHandlerOptions` | Base configuration for the HTTP tracking handler (service names, port mappings, headers to forward). |
| `TestTrackingMessageHandler` | `DelegatingHandler` that intercepts and logs HTTP requests/responses with tracking headers. |
| `TestTrackingHttpClientFactory` | `IHttpClientFactory` implementation that creates `HttpClient` instances with the tracking handler. |
| `RequestResponseLogger` | Static log collector; `Log(entry)` to add, `RequestAndResponseLogs` to retrieve all entries. |
| `RequestResponseLog` | Record capturing a single HTTP request or response with all metadata. |
| `PlantUmlCreator` | Converts `RequestResponseLog` entries into PlantUML sequence diagram code. |
| `PlantUmlTextEncoder` | Encodes PlantUML text to URL-safe format for server rendering. |
| `ReportGenerator` | Generates HTML and YAML report files from features, scenarios, and diagrams. |
| `Feature` | Record: `DisplayName`, `Endpoint?`, `Scenarios[]`. |
| `Scenario` | Record: `Id`, `DisplayName`, `IsHappyPath`, `Result`, `ErrorMessage?`, `ErrorStackTrace?`. |
| `ScenarioResult` | Enum: `Passed`, `Failed`, `Skipped`. |
| `TestTrackingHttpHeaders` | Constants for tracking HTTP headers (`test-tracking-ignore`, `test-tracking-current-test-name`, etc.). |

### <a name="api-integrations"></a>Integration Libraries [↑](#top)

Each integration package provides the same set of framework-specific types:

| Type | Purpose |
|---|---|
| `[Framework]TestTrackingMessageHandlerOptions` | Extends `TestTrackingMessageHandlerOptions` with auto-populated `CurrentTestInfoFetcher`. |
| `ServiceCollectionExtensions.TrackDependenciesForDiagrams()` | Registers tracking services in DI. |
| `WebApplicationFactoryExtensions.CreateTestTrackingClient()` | Creates an `HttpClient` with the tracking handler attached. |
| `TrackingDiagramOverride` | Framework-specific wrapper that auto-resolves the test ID from the current test context. |
| `[Framework]ReportGenerator` | Generates reports using framework-specific test metadata (xUnit `ITestContext`, NUnit `TestContext`, BDDfy scenarios, ReqNRoll scenarios). |

**BDDfy-specific:**

| Type | Purpose |
|---|---|
| `BDDfyDiagramsConfigurator` | `Configure()` registers BDDfy processors and the step-tracking executor for diagram capture and implicit setup detection. |
| `BDDfyStepTrackingExecutor` | Custom `IStepExecutor` that tracks the current BDD step type (GIVEN/WHEN/THEN) via `AsyncLocal` for implicit setup separation. |
| `BDDfyScenarioCollector` | Thread-safe collector for BDDfy scenario metadata. |
| `BDDfyScenarioInfo` / `BDDfyStepInfo` | Records for BDD scenario and step metadata. |

**ReqNRoll-specific:**

| Type | Purpose |
|---|---|
| `ReqNRollTrackingHooks` | Auto-registered `[Binding]` class with `[BeforeScenario]`/`[AfterStep]`/`[AfterScenario]` hooks. |
| `ReqNRollScenarioCollector` | Thread-safe collector for ReqNRoll scenario metadata. |
| `ReqNRollScenarioInfo` / `ReqNRollStepInfo` | Records for Gherkin scenario and step metadata. |
| `ReqNRollConstants` | Constants for context keys and tag conventions. |
| `ReqNRollReportEnhancer` | Post-processes ReqNRoll's native HTML report to inject sequence diagram attachments linked to each scenario's last step. Registered automatically by `ReqNRollReportGenerator`. |

**LightBDD-specific:**

| Type | Purpose |
|---|---|
| `ReportWritersConfigurationExtensions` | `CreateStandardReportsWithDiagrams()` extension for LightBDD's report writer pipeline. |
| `LightBddDiagramsFetcher` | Wraps diagrams for LightBDD's `DiagramAsCode` type. |
