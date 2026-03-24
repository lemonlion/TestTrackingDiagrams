# Integration Guide: xUnit v2 (without BDD framework)

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.xUnit2/`](../Example.Api/tests/Example.Api.Tests.Component.xUnit2/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with **xUnit v2** (no BDD framework). After completing this guide, your xUnit v2 tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams
- **YAML specification files**

xUnit v2 does not have `TestContext.Current` (introduced in xUnit v3), so this integration uses `AsyncLocal<T>` and a `BeforeAfterTestAttribute` to track the current test identity. A custom `XunitTestFramework` is used to capture test results and generate reports after all tests complete.

---

## Prerequisites

- .NET 8.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with xUnit v2

---

## Step 1: Create the Test Project

Create a new xUnit v2 test project:

```bash
dotnet new xunit -n MyApi.Tests.Component
```

---

## Step 2: Install NuGet Packages

```bash
dotnet add package TestTrackingDiagrams.xUnit2
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.xUnit2" Version="1.22.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.12" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
</ItemGroup>
```

---

## Step 3: Register the Custom Test Framework

xUnit v2's testhost calls `Environment.Exit` when tests finish, giving `ProcessExit` only ~2 seconds — not enough time for report generation. The library provides a custom `XunitTestFramework` that generates reports **before** the testhost exits.

### `GlobalUsings.cs`:

```csharp
global using Xunit;

[assembly: TestFramework(
    "TestTrackingDiagrams.xUnit2.ReportingTestFramework",
    "TestTrackingDiagrams.xUnit2")]
```

This also captures test results (pass/fail/skip) automatically via a message sink wrapper.

---

## Step 4: Create the Test Run Collection Fixture

Even though the custom framework handles report generation, a collection fixture is still useful for:
- Starting/stopping HTTP fakes
- Configuring `ReportConfigurationOptions`

### `Infrastructure/TestRun.cs`:

```csharp
using TestTrackingDiagrams;
using TestTrackingDiagrams.xUnit2;

namespace MyApi.Tests.Component.Infrastructure;

public class TestRun : DiagrammedTestRun, IDisposable
{
    public TestRun()
    {
        // Configure report options for the ReportingTestFramework
        ReportLifecycle.Options = new ReportConfigurationOptions
        {
            SpecificationsTitle = "My API Specifications",
            SeparateSetup = true,
        };

        // Optional: start any HTTP fakes here
    }

    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;
        // Optional: dispose HTTP fakes here
    }
}
```

> **Note:** Reports are generated automatically by `ReportingTestFramework` after all tests complete — you do **not** need to call `XUnit2ReportGenerator.CreateStandardReportsWithDiagrams` in `Dispose()`. If you prefer manual control, see the [Alternative: Collection Fixture Approach](#alternative-collection-fixture-approach) section below.

---

## Step 5: Create the Test Collection Definition

Create a collection definition that ties all your diagrammed tests to the `TestRun` fixture:

### `DiagrammedTestCollection.cs`:

```csharp
using TestTrackingDiagrams.xUnit2;

namespace MyApi.Tests.Component;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<Infrastructure.TestRun> { }
```

---

## Step 6: Create the Base Fixture

Create `Infrastructure/BaseFixture.cs`. All your test classes will inherit from this:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams.xUnit2;

namespace MyApi.Tests.Component.Infrastructure;

public abstract class BaseFixture : DiagrammedComponentTest
{
    private static readonly WebApplicationFactory<Program>? SFactory;
    protected HttpClient Client { get; }

    private const string ServiceUnderTestName = "My API";

    static BaseFixture()
    {
        SFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.TrackDependenciesForDiagrams(new XUnit2TestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    PortsToServiceNames =
                    {
                        { 80, ServiceUnderTestName },
                        { 5001, "Downstream Service A" }
                    }
                });
            });
        });
    }

    protected BaseFixture()
    {
        Client = SFactory!.CreateTestTrackingClient(
            new XUnit2TestTrackingMessageHandlerOptions
            {
                FixedNameForReceivingService = ServiceUnderTestName
            });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Client?.Dispose();
        base.Dispose(disposing);
    }
}
```

**Key points:**
- `DiagrammedComponentTest` applies `[Collection("Diagrammed Test Collection")]` and `[TestTracking]` automatically.
- `[TestTracking]` is a `BeforeAfterTestAttribute` that sets the current test identity in `AsyncLocal` before each test, and collects scenario metadata.
- `XUnit2TestTrackingMessageHandlerOptions` reads the current test identity from the `AsyncLocal` context.

---

## Step 7: Write Test Scenarios

Tests are written as regular xUnit `[Fact]` or `[Theory]` methods. Use the `[Endpoint]` and `[HappyPath]` attributes to add metadata for the report.

### `Scenarios/Cake_Feature.cs`:

```csharp
using TestTrackingDiagrams.xUnit2;

namespace MyApi.Tests.Component.Scenarios;

[Endpoint("/cake")]
public partial class Cake_Feature
{
    [Fact]
    [HappyPath]
    public async Task Calling_Create_Cake_Endpoint_Returns_Cake()
    {
        await Given_a_valid_post_request_for_the_Cake_endpoint();
        await When_the_request_is_sent_to_the_cake_post_endpoint();
        await Then_the_response_should_be_successful();
    }

    [Fact]
    public async Task Calling_Create_Cake_Endpoint_Without_Eggs_Returns_Bad_Request()
    {
        await Given_a_valid_post_request_for_the_Cake_endpoint();
        await But_the_request_body_is_missing_eggs();
        await When_the_request_is_sent_to_the_cake_post_endpoint();
        await Then_the_response_http_status_should_be_bad_request();
    }
}
```

### `Scenarios/Cake_Feature.steps.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using MyApi.Tests.Component.Infrastructure;

namespace MyApi.Tests.Component.Scenarios;

public partial class Cake_Feature : BaseFixture
{
    private HttpResponseMessage? _response;

    private async Task Given_a_valid_post_request_for_the_Cake_endpoint()
    {
        // Build your request using Client
    }

    private async Task But_the_request_body_is_missing_eggs()
    {
        // Modify request
    }

    private async Task When_the_request_is_sent_to_the_cake_post_endpoint()
    {
        _response = await Client.PostAsJsonAsync("cake", /* request */);
    }

    private async Task Then_the_response_should_be_successful()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task Then_the_response_http_status_should_be_bad_request()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

---

## Step 8: Run the Tests

```bash
dotnet test
```

After the tests complete, check the `bin/Debug/net8.0/Reports/` folder:

| File | Description |
|------|-------------|
| `ComponentSpecificationsWithExamples.html` | HTML specifications with embedded PlantUML sequence diagrams |
| `FeaturesReport.html` | HTML test run report with diagrams and execution summary |
| `ComponentSpecifications.yml` | YAML specifications |

---

## Using PlantUML Overrides

You can customise diagrams within a test using `TrackingDiagramOverride`:

```csharp
using TestTrackingDiagrams.xUnit2;

// Insert a delimiter between multiple requests in the diagram
TrackingDiagramOverride.InsertTestDelimiter("Step 1");

// Insert raw PlantUML markup
TrackingDiagramOverride.InsertPlantUml("note over MyApi : Custom note");

// Override the start/end of diagram generation
TrackingDiagramOverride.StartOverride();
TrackingDiagramOverride.EndOverride();

// Explicitly mark the boundary between setup and action phases
TrackingDiagramOverride.StartAction();
```

> **Setup separation:** When `SeparateSetup = true` is set on `ReportConfigurationOptions`, HTTP calls made before `StartAction()` are wrapped in a visual "Setup" partition in the diagram.

---

## Architecture Summary

```
┌─────────────────────────────────┐
│     DiagrammedTestCollection    │  ← Collection definition (one per assembly)
│   ICollectionFixture<TestRun>   │
└─────────────┬───────────────────┘
              │ creates once
              ▼
┌─────────────────────────────────┐
│           TestRun               │  ← Sets ReportLifecycle.Options, starts fakes
│     : DiagrammedTestRun         │
└─────────────────────────────────┘
              │ shared across
              ▼
┌─────────────────────────────────┐
│          BaseFixture            │  ← Creates tracked HttpClient
│   : DiagrammedComponentTest     │     [TestTracking] sets AsyncLocal identity
└─────────────┬───────────────────┘
              │ inherited by
              ▼
┌─────────────────────────────────┐
│  Cake_Feature : BaseFixture     │  ← Your test class with [Fact] methods
└─────────────────────────────────┘
              │ after all tests
              ▼
┌─────────────────────────────────┐
│    ReportingTestFramework       │  ← Custom xUnit framework (via assembly attr)
│  → ReportingTestFrameworkExecutor│     Captures results via message sink
│  → ReportLifecycle.GenerateReports│    Generates HTML/YAML/PlantUML reports
└─────────────────────────────────┘
```

---

## How xUnit v2 Integration Differs from xUnit v3

| Aspect | xUnit v3 | xUnit v2 |
|--------|----------|----------|
| Test identity | `TestContext.Current` (built-in) | `AsyncLocal<T>` via `[TestTracking]` attribute |
| Report generation trigger | `DiagrammedTestRun.Dispose()` | `ReportingTestFramework` (custom `XunitTestFramework`) |
| Result capture | `TestContext.Current.TestState.Result` | `TestResultCapturingSink` (wraps `IMessageSink`) |
| Trait attributes | `ITraitAttribute.GetTraits()` | `ITraitAttribute` + `ITraitDiscoverer` |
| TestContext collection | `ConcurrentQueue<ITestContext>` in `DiagrammedComponentTest.Dispose()` | `ConcurrentDictionary<string, ScenarioInfo>` in `[TestTracking].Before()` |
| Package reference | `xunit.v3` | `xunit` (v2) |

---

## Alternative: Collection Fixture Approach

If you prefer not to use the custom test framework, you can generate reports in `TestRun.Dispose()` instead. Note that with this approach:
- Test results will not be captured automatically (all will show as "Passed")
- Report generation may be truncated if the testhost exits too quickly

```csharp
public class TestRun : DiagrammedTestRun, IDisposable
{
    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;

        XUnit2ReportGenerator.CreateStandardReportsWithDiagrams(
            StartRunTime,
            EndRunTime,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "My API Specifications"
            });
    }
}
```

For this approach, do **not** add the `[assembly: TestFramework(...)]` attribute.

---

## Customisation Options

### ReportConfigurationOptions

| Property | Default | Description |
|----------|---------|-------------|
| `SpecificationsTitle` | `"Specifications"` | Title shown at the top of reports |
| `PlantUmlServerBaseUrl` | `"https://www.plantuml.com/plantuml"` | PlantUML server URL |
| `HtmlSpecificationsFileName` | `"ComponentSpecificationsWithExamples"` | Output filename for specs HTML |
| `HtmlTestRunReportFileName` | `"FeaturesReport"` | Output filename for test run HTML |
| `YamlSpecificationsFileName` | `"ComponentSpecifications"` | Output filename for YAML specs |
| `HtmlSpecificationsCustomStyleSheet` | `null` | Custom CSS appended to specs HTML |
| `ExcludedHeaders` | `[]` | HTTP headers to exclude from diagrams |
| `SeparateSetup` | `false` | When `true`, HTTP calls made before `StartAction()` are wrapped in a visual "Setup" partition in the diagram |
| `HighlightSetup` | `true` | When `true` (and `SeparateSetup` is enabled), the setup partition is rendered with a background colour |

### XUnit2TestTrackingMessageHandlerOptions

| Property | Description |
|----------|-------------|
| `CallingServiceName` | Display name for the service making outgoing HTTP calls |
| `FixedNameForReceivingService` | Display name for the service receiving requests |
| `PortsToServiceNames` | Dictionary mapping port numbers to friendly service names. Unmapped ports appear as `localhost_80`, `localhost_5001`, etc. |

---

## Troubleshooting

### Reports folder is empty
- Ensure you have `[assembly: TestFramework("TestTrackingDiagrams.xUnit2.ReportingTestFramework", "TestTrackingDiagrams.xUnit2")]` in your project.
- Ensure `ReportLifecycle.Options` is set (e.g. in `TestRun` constructor).
- Alternatively, if using the collection fixture approach, ensure `TestRun.Dispose()` calls `XUnit2ReportGenerator.CreateStandardReportsWithDiagrams`.

### Tests are not showing in the report
- Make sure each test class inherits from `DiagrammedComponentTest` (which applies `[TestTracking]`), or manually apply `[TestTracking]` to your test classes or assembly.
- The `[TestTracking]` attribute collects scenario metadata in `Before()`. Without it, no scenarios are tracked.

### AsyncLocal not flowing correctly
- `AsyncLocal` flows through `await` calls and through `WebApplicationFactory`'s `TestServer`. If you spawn new threads manually, the value may not propagate — use `async`/`await` instead.

### All test results show as "Passed"
- This happens when not using the `ReportingTestFramework`. The custom framework wraps the execution message sink to capture `ITestFailed` and `ITestSkipped` messages. Without it, results default to "Passed".

### Empty specifications HTML / YAML
If any test has failed, the specifications files will be blank by design. The `FeaturesReport.html` will still be generated.
