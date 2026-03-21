# Integration Guide: xUnit (without BDD framework)

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.XUnit/`](../Example.Api/tests/Example.Api.Tests.Component.XUnit/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with plain **xUnit** (no BDD framework). After completing this guide, your xUnit tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams
- **YAML specification files**

This is the simplest integration path if you are already writing xUnit tests and just want to add automatic diagram generation.

---

## Prerequisites

- .NET 8.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with xUnit

---

## Step 1: Create the Test Project

Create a new xUnit test project:

```bash
dotnet new xunit -n MyApi.Tests.Component
```

---

## Step 2: Install NuGet Packages

```bash
dotnet add package TestTrackingDiagrams.XUnit
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.v3
dotnet add package xunit.runner.visualstudio
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.XUnit" Version="1.20.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.12" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit.v3" Version="1.0.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
</ItemGroup>
```

---

## Step 3: Create the Test Run Collection Fixture

xUnit uses "collection fixtures" to share state across tests. TestTrackingDiagrams provides `DiagrammedTestRun` as a base class for your collection fixture. This is where reports are generated.

### `Infrastructure/TestRun.cs`:

```csharp
using TestTrackingDiagrams;
using TestTrackingDiagrams.XUnit;

namespace MyApi.Tests.Component.Infrastructure;

public class TestRun : DiagrammedTestRun, IDisposable
{
    public TestRun()
    {
        // Optional: start any HTTP fakes here
    }

    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;

        // Generate reports when the test run ends
        XUnitReportGenerator.CreateStandardReportsWithDiagrams(
            TestContexts,
            StartRunTime,
            EndRunTime,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "My API Specifications"
            });

        // Optional: dispose HTTP fakes here
    }
}
```

---

## Step 4: Create the Test Collection Definition

Create a collection definition that ties all your diagrammed tests to the `TestRun` fixture:

### `DiagrammedTestCollection.cs`:

```csharp
using TestTrackingDiagrams.XUnit;

namespace MyApi.Tests.Component;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<Infrastructure.TestRun> { }
```

This class is never instantiated directly — it just tells xUnit to create a single `TestRun` instance shared across all tests in the collection.

---

## Step 5: Create the Base Fixture

Create `Infrastructure/BaseFixture.cs`. All your test classes will inherit from this:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams.XUnit;

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
                services.TrackDependenciesForDiagrams(new XUnitTestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    PortsToServiceNames =
                    {
                        { 5001, "Downstream Service A" }
                    }
                });
            });
        });
    }

    protected BaseFixture()
    {
        Client = SFactory!.CreateTestTrackingClient(
            new XUnitTestTrackingMessageHandlerOptions
            {
                FixedNameForReceivingService = ServiceUnderTestName
            });
    }

    public void Dispose(bool disposing) => Client?.Dispose();
}
```

**Key points:**
- `DiagrammedComponentTest` is the library's base class. It applies `[Collection("Diagrammed Test Collection")]` automatically and enqueues the `TestContext` on `Dispose()` for report collection.
- `XUnitTestTrackingMessageHandlerOptions` uses xUnit's built-in `TestContext.Current` to resolve the current test's identity.

---

## Step 6: Write Test Scenarios

Tests are written as regular xUnit `[Fact]` or `[Theory]` methods. Use the `[Endpoint]` and `[HappyPath]` attributes to add metadata for the report.

### `Scenarios/Cake_Feature.cs`:

```csharp
using TestTrackingDiagrams.XUnit;

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

**Key points:**
- `[Endpoint("/cake")]` — Sets the endpoint label for this feature group in the report.
- `[HappyPath]` — Marks a scenario as a happy path (filterable in the HTML report).
- Class names are converted to feature names: underscores become spaces (e.g. `Cake_Feature` → "Cake Feature").
- Method names are converted to scenario names in the same way.

---

## Step 7: Run the Tests

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
using TestTrackingDiagrams.XUnit;

// Insert a delimiter between multiple requests in the diagram
TrackingDiagramOverride.InsertTestDelimiter("Step 1");

// Insert raw PlantUML markup
TrackingDiagramOverride.InsertPlantUml("note over MyApi : Custom note");

// Override the start/end of diagram generation
TrackingDiagramOverride.StartOverride();
TrackingDiagramOverride.EndOverride();
```

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
│           TestRun               │  ← Generates reports in Dispose()
│     : DiagrammedTestRun         │
└─────────────────────────────────┘
              │ shared across
              ▼
┌─────────────────────────────────┐
│          BaseFixture            │  ← Creates tracked HttpClient
│   : DiagrammedComponentTest     │     Enqueues TestContext on Dispose
└─────────────┬───────────────────┘
              │ inherited by
              ▼
┌─────────────────────────────────┐
│       Cake_Feature : BaseFixture│  ← Your test class with [Fact] methods
└─────────────────────────────────┘
```

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

### XUnitTestTrackingMessageHandlerOptions

| Property | Description |
|----------|-------------|
| `CallingServiceName` | Display name for the service making outgoing HTTP calls |
| `FixedNameForReceivingService` | Display name for the service receiving requests |
| `PortsToServiceNames` | Dictionary mapping port numbers to friendly service names |

---

## Troubleshooting

### Reports folder is empty
- Ensure `TestRun.Dispose()` calls `XUnitReportGenerator.CreateStandardReportsWithDiagrams`.
- Ensure your test classes inherit from `BaseFixture` (which inherits from `DiagrammedComponentTest`).
- Ensure you have the `DiagrammedTestCollection` collection definition class.

### Tests are not showing in the report
- Make sure each test class inherits from `DiagrammedComponentTest` (directly or via `BaseFixture`). The base class calls `DiagrammedTestRun.TestContexts.Enqueue(TestContext.Current)` on `Dispose()`.

### Empty specifications HTML / YAML
If any test has failed, the specifications files will be blank by design. The `FeaturesReport.html` will still be generated.
