# Integration Guide: BDDfy with xUnit v3

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.BDDfy.xUnit3/`](../Example.Api/tests/Example.Api.Tests.Component.BDDfy.xUnit3/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with [BDDfy](https://github.com/TestStack/TestStack.BDDfy) using **xUnit v3** as the test runner. After completing this guide, your BDDfy tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams and BDD steps (Given/When/Then/And/But)
- **YAML specification files** with step information included
- **Enhanced BDDfy native HTML report** with sequence diagrams injected alongside each scenario

---

## Prerequisites

- .NET 10.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with BDDfy's fluent API or convention-based step patterns

---

## Step 1: Create the Test Project

Create a new xUnit test project:

```bash
dotnet new xunit -n MyApi.Tests.Component.BDDfy
```

**Important:** xUnit v3 runs tests out-of-process, so you must set `<OutputType>Exe</OutputType>` in your `.csproj`:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
</PropertyGroup>
```

---

## Step 2: Install NuGet Packages

Add the following packages to your test project:

```bash
dotnet add package TestTrackingDiagrams.BDDfy.xUnit3
dotnet add package TestStack.BDDfy --version 8.0.1.3
dotnet add package xunit.v3 --version 2.0.2
dotnet add package xunit.runner.visualstudio --version 3.0.2
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.BDDfy.xUnit3" Version="1.23.9" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.12" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="TestStack.BDDfy" Version="8.0.1.3" />
    <PackageReference Include="xunit.v3" Version="2.0.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
</ItemGroup>
```

---

## Step 3: Create the Assembly Fixture

Create an `Infrastructure/BDDfyTestSetup.cs` file. This class uses xUnit v3's `[assembly: AssemblyFixture]` to set up and tear down the test environment. It:

1. Configures BDDfy's diagram-capturing processor
2. Creates a `WebApplicationFactory` for your API with HTTP tracking
3. Generates all reports after tests complete

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams;
using TestTrackingDiagrams.BDDfy.xUnit3;
using Xunit;

[assembly: AssemblyFixture(typeof(MyApi.Tests.Component.BDDfy.Infrastructure.BDDfyTestSetup))]

namespace MyApi.Tests.Component.BDDfy.Infrastructure;

public class BDDfyTestSetup : IAsyncLifetime
{
    private const string ServiceUnderTestName = "My API";

    private static WebApplicationFactory<Program>? _factory;

    public static WebApplicationFactory<Program> Factory => _factory!;

    public ValueTask InitializeAsync()
    {
        // Register BDDfy diagram processors (must be called before any BDDfy tests run)
        BDDfyDiagramsConfigurator.Configure();
        BDDfyScenarioCollector.StartRunTime = DateTime.UtcNow;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.TrackDependenciesForDiagrams(new BDDfyTestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    PortsToServiceNames =
                    {
                        { 80, ServiceUnderTestName },
                        { 5001, "Downstream Service A" },
                        { 5002, "Downstream Service B" }
                    }
                });
            });
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        BDDfyScenarioCollector.EndRunTime = DateTime.UtcNow;

        // Generate HTML and YAML reports with diagrams
        BDDfyReportGenerator.CreateStandardReportsWithDiagrams(
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "My API Specifications"
            });

        _factory?.Dispose();
        return ValueTask.CompletedTask;
    }

    public static HttpClient CreateTrackingClient()
    {
        return _factory!.CreateTestTrackingClient(
            new BDDfyTestTrackingMessageHandlerOptions
            {
                FixedNameForReceivingService = ServiceUnderTestName
            });
    }
}
```

### Key components:

| Component | Purpose |
|-----------|---------|
| `BDDfyDiagramsConfigurator.Configure()` | Registers BDDfy processors that capture scenario info and enhance BDDfy's HTML report |
| `BDDfyScenarioCollector.StartRunTime` | Sets the test run start time for the report header |
| `TrackDependenciesForDiagrams()` | Configures HTTP tracking for outgoing requests from the SUT |
| `CreateTestTrackingClient()` | Creates an `HttpClient` that tracks all requests for diagram generation |
| `BDDfyReportGenerator.CreateStandardReportsWithDiagrams()` | Generates all report files in `bin/Debug/net10.0/Reports/` |

---

## Step 4: Create a Base Test Fixture

Create an `Infrastructure/BaseFixture.cs` that provides each test with a tracking-enabled `HttpClient`:

```csharp
namespace MyApi.Tests.Component.BDDfy.Infrastructure;

public abstract class BaseFixture : IDisposable
{
    protected HttpClient Client { get; }

    protected BaseFixture()
    {
        Client = BDDfyTestSetup.CreateTrackingClient();
    }

    public void Dispose() => Client.Dispose();
}
```

---

## Step 5: Write BDDfy Test Scenarios

Create a test class that uses BDDfy's fluent API. Each test method calls `this.BDDfy()` which:
1. Executes your Given/When/Then steps
2. Captures the scenario info (title, steps, result) for report generation
3. Captures the scenario ID for BDDfy HTML report enhancement

```csharp
using System.Net;
using System.Net.Http.Json;
using MyApi.Tests.Component.BDDfy.Infrastructure;
using TestStack.BDDfy;
using Xunit;

[Story(
    AsA = "dessert provider",
    IWant = "to create cakes from ingredients",
    SoThat = "customers can enjoy delicious cakes")]
public class CakeFeature : BaseFixture
{
    private HttpResponseMessage? _response;

    [Fact]
    public void Calling_Create_Cake_Endpoint_Returns_Cake()
    {
        this.Given(x => x.GivenAValidPostRequest())
            .When(x => x.WhenTheRequestIsSent())
            .Then(x => x.ThenTheResponseShouldBeSuccessful())
            .WithTags("happy-path", "endpoint:/cake")
            .BDDfy();
    }

    [Fact]
    public void Calling_Create_Cake_Endpoint_Without_Eggs_Returns_Bad_Request()
    {
        this.Given(x => x.GivenAValidPostRequest())
            .And(x => x.AndTheRequestBodyIsMissingEggs())
            .When(x => x.WhenTheRequestIsSent())
            .Then(x => x.ThenTheResponseShouldBeBadRequest())
            .WithTags("endpoint:/cake")
            .BDDfy();
    }

    // Step methods
    private async Task GivenAValidPostRequest() { /* ... */ }
    private void AndTheRequestBodyIsMissingEggs() { /* ... */ }
    private async Task WhenTheRequestIsSent() { /* ... */ }
    private async Task ThenTheResponseShouldBeSuccessful() { /* ... */ }
    private void ThenTheResponseShouldBeBadRequest() { /* ... */ }
}
```

### Tags that TestTrackingDiagrams uses:

| Tag | Purpose |
|-----|---------|
| `endpoint:/cake` | Sets the endpoint label on the feature in the report |
| `happy-path` | Marks the scenario as a "happy path" (highlighted in reports, filterable) |

### BDDfy Step Patterns

BDDfy captures step names from your method names. The first word is used as the keyword (Given/When/Then/And/But):

| Method Name | Rendered Step |
|-------------|--------------|
| `GivenAValidPostRequest` | **Given** a valid post request |
| `WhenTheRequestIsSent` | **When** the request is sent |
| `ThenTheResponseShouldBeSuccessful` | **Then** the response should be successful |
| `AndTheRequestBodyIsMissingEggs` | **And** the request body is missing eggs |

---

## Step 6: Story Metadata

BDDfy's `[Story]` attribute provides feature-level metadata that appears in the reports:

```csharp
[Story(
    AsA = "dessert provider",
    IWant = "to create cakes from ingredients",
    SoThat = "customers can enjoy delicious cakes")]
public class CakeFeature : BaseFixture
```

The `AsA`/`IWant`/`SoThat` narrative appears as a feature description in the generated reports. The class name (converted from `CamelCase` to `Camel case`) becomes the feature display name.

---

## Step 7: Run the Tests

```bash
dotnet test
```

After a successful run, check the output directory (`bin/Debug/net10.0/`) for generated reports:

### Reports generated

| File | Location | Description |
|------|----------|-------------|
| `ComponentSpecifications.yml` | `Reports/` | YAML specifications with BDDfy steps |
| `ComponentSpecificationsWithExamples.html` | `Reports/` | HTML specifications with embedded diagrams |
| `FeaturesReport.html` | `Reports/` | Test run report with diagrams and execution summary |
| `BDDfy.html` | Root output dir | BDDfy's native HTML report, enhanced with sequence diagrams |

### BDDfy Report Enhancement

The library automatically enhances BDDfy's native HTML report by injecting sequence diagram images after each scenario's steps. This happens via a custom `IBatchProcessor` that runs after BDDfy generates its report. No additional configuration is needed — it works automatically when you call `BDDfyDiagramsConfigurator.Configure()`.

---

## How It Works

The integration uses three coordinated components:

1. **`BDDfyTestTrackingMessageHandlerOptions`** — Provides the test tracking ID from xUnit v3's `TestContext.Current.Test.UniqueID` so that HTTP requests are tagged with the correct test ID during execution.

2. **`DiagramCapturingProcessor`** — A BDDfy `IProcessor` that runs during each `this.BDDfy()` call. It captures the scenario title, steps (Given/When/Then), tags, result, and maps them to the xUnit test ID.

3. **`DiagramEnhancingBatchProcessor`** — A BDDfy `IBatchProcessor` that runs after all tests complete. It reads BDDfy's native HTML report and injects sequence diagram images next to each scenario.

4. **`BDDfyReportGenerator`** — Called in the assembly fixture's `DisposeAsync`, generates the three standard TestTrackingDiagrams reports with BDDfy step data and diagrams.

---

## Faking Downstream Dependencies (Correctly)

When your SUT calls downstream HTTP services, those calls must flow through `TestTrackingMessageHandler` to produce proper HTTP-style diagram arrows (with method, status code, headers, body). **Do not** mock service client interfaces and use `MessageTracker` to manually log HTTP interactions — this produces event-style (blue) arrows that are misleading.

Recommended approaches:
- **In-memory fake APIs** — `WebApplicationFactory` instances that serve canned responses (see [Example.Api](../Example.Api/))
- **[JustEat HttpClient Interception](https://github.com/justeattakeaway/httpclient-interception)** — handler-level interception, chain with `TestTrackingMessageHandler`
- **[WireMock.Net](https://github.com/WireMock-Net/WireMock.Net)** — real HTTP server on a random port, map in `PortsToServiceNames`

See the [Tracking Dependencies wiki page](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Tracking-Dependencies#faking-dependencies-getting-proper-http-tracking) for detailed examples of each approach.

---

## Common Gotchas

### 1. `BDDfyDiagramsConfigurator.Configure()` must be called before any tests run
If you forget to call `Configure()`, the `DiagramCapturingProcessor` won't be registered and no scenario data will be captured. Use xUnit v3's `[assembly: AssemblyFixture]` to ensure it runs first.

### 2. xUnit v3 requires `<OutputType>Exe</OutputType>`
Without this, xUnit v3 tests won't execute because the out-of-process runner requires an executable entry point.

### 3. Async steps in BDDfy
BDDfy supports async step methods that return `Task`. The fluent API accepts `Func<TScenario, Task>` lambdas. Your HTTP call steps can be async.

### 4. Feature names derived from class names
BDDfy converts your class name to a readable title. `CakeFeature` becomes "Cake feature". Use the `[Story(Title = "...")]` attribute to customize.

### 5. BDDfy report not enhanced
The BDDfy HTML report enhancement requires `BDDfyDiagramsConfigurator.Configure()` to register the batch processor. The enhancement only works when BDDfy generates its HTML report (the default behaviour).

---

## Customization

### Report Options

Customize report generation in the `BDDfyReportGenerator.CreateStandardReportsWithDiagrams()` call:

```csharp
BDDfyReportGenerator.CreateStandardReportsWithDiagrams(
    new ReportConfigurationOptions
    {
        SpecificationsTitle = "My Custom Title",
        HtmlSpecificationsFileName = "CustomSpecs",
        HtmlTestRunReportFileName = "CustomReport",
        YamlSpecificationsFileName = "CustomYaml",
        PlantUmlServerBaseUrl = "https://my-plantuml-server.com/plantuml",
        ExcludedHeaders = new[] { "Authorization", "X-Api-Key" },
        SeparateSetup = true,
        HighlightSetup = true
    });
```

### Diagram Overrides

Insert custom PlantUML content into diagrams:

```csharp
using TestTrackingDiagrams.BDDfy.xUnit3;

TrackingDiagramOverride.InsertPlantUml("note right: Custom annotation");
TrackingDiagramOverride.InsertTestDelimiter("Phase 1");

// Override the start/end of diagram generation
TrackingDiagramOverride.StartOverride();
TrackingDiagramOverride.EndOverride();

// Explicitly mark the boundary between setup and action phases
TrackingDiagramOverride.StartAction();
```

### Setup Separation

BDDfy supports **automatic setup separation**. When `SeparateSetup = true` is set on `ReportConfigurationOptions`, HTTP calls made during GIVEN steps are automatically wrapped in a visual "Setup" partition in the diagram. The boundary is detected implicitly when the test transitions from a GIVEN step to a WHEN or THEN step — no manual `StartAction()` call is needed.

This works via a custom `IStepExecutor` that tracks the current BDD step type during execution. It is registered automatically when you call `BDDfyDiagramsConfigurator.Configure()`.

---

## What's New in v2.0

### BDD Steps in the Unified Report

BDDfy steps (Given/When/Then/And/But) are now rendered directly in the main HTML report alongside sequence diagrams. Steps are automatically extracted from BDDfy's step model and displayed with keyword highlighting.

### Tags → Labels

BDDfy tags (other than the built-in `happy-path` and `endpoint:` tags) are now displayed as **labels** on scenario summaries in the report. For example:

```csharp
this.Given(...).When(...).Then(...)
    .WithTags("smoke", "regression", "happy-path")
    .BDDfy();
```

The tags `smoke` and `regression` appear as badges; `happy-path` continues to control the happy-path filter.

### Story Description → Feature Description

The BDDfy Story description (from `[Story]` attribute or `StoryMetadata`) is now displayed as a feature description in the report.

### Feature Summary Table

A sortable summary table at the top of the report shows per-feature scenario counts by status (Passed/Failed/Skipped) and step counts when available.

### Category Filter

When scenarios have categories, a category filter toolbar appears in the report allowing you to filter scenarios by category.

### YAML Steps

The YAML specification file now includes steps, labels, and categories for each scenario.
