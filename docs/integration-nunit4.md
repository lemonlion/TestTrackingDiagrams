# Integration Guide: NUnit 4

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.NUnit4/`](../Example.Api/tests/Example.Api.Tests.Component.NUnit4/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with **NUnit**. After completing this guide, your NUnit tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams
- **YAML specification files**

---

## Prerequisites

- .NET 8.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with NUnit

---

## Step 1: Create the Test Project

Create a new NUnit test project:

```bash
dotnet new nunit -n MyApi.Tests.Component
```

---

## Step 2: Install NuGet Packages

```bash
dotnet add package TestTrackingDiagrams.NUnit4
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package NUnit
dotnet add package NUnit3TestAdapter
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.NUnit4" Version="1.20.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.12" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="NUnit" Version="4.3.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
    <PackageReference Include="NUnit.Analyzers" Version="4.6.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

---

## Step 3: Add a Global Usings File

Create a `GlobalUsings.cs`:

```csharp
global using NUnit.Framework;
```

---

## Step 4: Create the Test Run Setup/Teardown

NUnit uses a `[SetUpFixture]` class for global setup and teardown. **This must be placed outside of any namespace** so that it applies to the whole assembly.

Create a `Infrastructure/TestRun.cs`:

```csharp
using TestTrackingDiagrams;
using TestTrackingDiagrams.NUnit4;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]

[SetUpFixture]
public class TestRun : DiagrammedTestRun
{
    [OneTimeSetUp]
    public static void GlobalSetup()
    {
        Setup();
        // Optional: start any HTTP fakes here
    }

    [OneTimeTearDown]
    public static void GlobalTeardown()
    {
        EndRunTime = DateTime.UtcNow;

        // Generate reports when the test run ends
        NUnitReportGenerator.CreateStandardReportsWithDiagrams(
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

**Critical points:**
- The class **must not be inside a namespace** — NUnit only runs `[SetUpFixture]` as a global fixture when it has no namespace.
- `[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]` ensures each test gets a fresh fixture instance (important for accurate tracking).
- Call `Setup()` in `[OneTimeSetUp]` — this records the `StartRunTime`.

---

## Step 5: Create the Base Fixture

Create `Infrastructure/BaseFixture.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams.NUnit4;

namespace MyApi.Tests.Component.Infrastructure;

public abstract class BaseFixture : DiagrammedComponentTest, IDisposable
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
                services.TrackDependenciesForDiagrams(new NUnitTestTrackingMessageHandlerOptions
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
            new NUnitTestTrackingMessageHandlerOptions
            {
                FixedNameForReceivingService = ServiceUnderTestName
            });
    }

    public void Dispose() => Client.Dispose();
}
```

**Key points:**
- `DiagrammedComponentTest` provides a `[TearDown]` method that enqueues `TestContext.CurrentContext` for report collection after each test.
- `NUnitTestTrackingMessageHandlerOptions` uses NUnit's `TestContext.CurrentContext` to resolve the current test's identity.

---

## Step 6: Write Test Scenarios

Tests are written as regular NUnit `[Test]` methods. Use the `[Endpoint]` and `[HappyPath]` attributes to add metadata for the report.

### `Scenarios/Cake_Feature.cs`:

```csharp
using TestTrackingDiagrams.NUnit4;

namespace MyApi.Tests.Component.Scenarios;

[Endpoint("/cake")]
public partial class Cake_Feature
{
    [Test]
    [HappyPath]
    public async Task Calling_Create_Cake_Endpoint_Returns_Cake()
    {
        await Given_a_valid_post_request_for_the_Cake_endpoint();
        await When_the_request_is_sent_to_the_cake_post_endpoint();
        await Then_the_response_should_be_successful();
    }

    [Test]
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

[TestFixture]
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
- `[Endpoint("/cake")]` — Sets the endpoint label for this feature group in the report. This attribute maps to NUnit's `PropertyAttribute`.
- `[HappyPath]` — Marks a scenario as a happy path (filterable in the HTML report).
- `[TestFixture]` — Required on the partial class with steps that inherits from `BaseFixture`.
- Class and method names are converted from underscore-separated to space-separated in reports.

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

## Architecture Summary

```
┌─────────────────────────────────┐
│           TestRun               │  ← [SetUpFixture] outside any namespace
│     : DiagrammedTestRun         │     Generates reports in [OneTimeTearDown]
│     [SetUpFixture]              │
└─────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│          BaseFixture            │  ← Creates tracked HttpClient
│   : DiagrammedComponentTest     │     Enqueues TestContext on [TearDown]
│   IDisposable                   │
└─────────────┬───────────────────┘
              │ inherited by
              ▼
┌─────────────────────────────────┐
│  Cake_Feature : BaseFixture     │  ← Your test class with [Test] methods
│  [TestFixture]                  │
└─────────────────────────────────┘
```

---

## Using PlantUML Overrides

You can customise diagrams within a test using `TrackingDiagramOverride`:

```csharp
using TestTrackingDiagrams.NUnit4;

// Insert a delimiter between multiple requests in the diagram
TrackingDiagramOverride.InsertTestDelimiter("Step 1");

// Insert raw PlantUML markup
TrackingDiagramOverride.InsertPlantUml("note over MyApi : Custom note");

// Override the start/end of diagram generation
TrackingDiagramOverride.StartOverride();
TrackingDiagramOverride.EndOverride();
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

### NUnitTestTrackingMessageHandlerOptions

| Property | Description |
|----------|-------------|
| `CallingServiceName` | Display name for the service making outgoing HTTP calls |
| `FixedNameForReceivingService` | Display name for the service receiving requests |
| `PortsToServiceNames` | Dictionary mapping port numbers to friendly service names. Unmapped ports appear as `localhost_80`, `localhost_5001`, etc. |

---

## Troubleshooting

### Reports folder is empty
- Ensure `TestRun.GlobalTeardown()` calls `NUnitReportGenerator.CreateStandardReportsWithDiagrams`.
- Ensure `TestRun` has no namespace (otherwise NUnit won't treat it as a global `[SetUpFixture]`).
- Ensure your test classes inherit from `BaseFixture` (which inherits from `DiagrammedComponentTest`).

### Tests are not showing in the report
- Make sure each test class inherits from `DiagrammedComponentTest` (directly or via `BaseFixture`). The base class's `[TearDown]` enqueues the `TestContext` for collection.

### Empty specifications HTML / YAML
If any test has failed, the specifications files will be blank by design. The `FeaturesReport.html` will still be generated.
