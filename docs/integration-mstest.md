# Integration Guide: MSTest

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with **MSTest**. After completing this guide, your MSTest tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams
- **YAML specification files**

---

## Prerequisites

- .NET 10.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with MSTest

---

## Step 1: Create the Test Project

Create a new MSTest test project:

```bash
dotnet new mstest -n MyApi.Tests.Component
```

---

## Step 2: Install NuGet Packages

```bash
dotnet add package TestTrackingDiagrams.MSTest
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package MSTest
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.MSTest" Version="1.24.9" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="MSTest" Version="3.9.2" />
</ItemGroup>
```

---

## Step 3: Create the Test Run Setup/Teardown

MSTest uses `[AssemblyInitialize]` and `[AssemblyCleanup]` for global setup and teardown.

Create an `Infrastructure/TestRun.cs`:

```csharp
using TestTrackingDiagrams;
using TestTrackingDiagrams.MSTest;

namespace MyApi.Tests.Component.Infrastructure;

[TestClass]
public class TestRun : DiagrammedTestRun
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        Setup();
        // Optional: start any HTTP fakes here
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        EndRunTime = DateTime.UtcNow;

        // Generate reports when the test run ends
        MSTestReportGenerator.CreateStandardReportsWithDiagrams(
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
- `[AssemblyInitialize]` and `[AssemblyCleanup]` must be `static` methods in a class marked with `[TestClass]`.
- Call `Setup()` in `[AssemblyInitialize]` — this records the `StartRunTime`.
- Report generation happens in `[AssemblyCleanup]` after all tests have run.

---

## Step 4: Create the Base Fixture

Create `Infrastructure/BaseFixture.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams.MSTest;

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
                services.TrackDependenciesForDiagrams(new MSTestTestTrackingMessageHandlerOptions
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
            new MSTestTestTrackingMessageHandlerOptions
            {
                FixedNameForReceivingService = ServiceUnderTestName
            });
    }

    public void Dispose() => Client.Dispose();
}
```

**Key points:**
- `DiagrammedComponentTest` provides `[TestInitialize]` (sets the async-local test context) and `[TestCleanup]` (enqueues test metadata for report collection).
- `MSTestTestTrackingMessageHandlerOptions` uses the async-local `TestContext` to resolve the current test's identity.

---

## Step 5: Write Test Scenarios

Tests are written as regular MSTest `[TestMethod]` methods. Use the `[Endpoint]` and `[HappyPath]` attributes to add metadata for the report.

### `Scenarios/Cake_Feature.cs`:

```csharp
using TestTrackingDiagrams.MSTest;

namespace MyApi.Tests.Component.Scenarios;

[Endpoint("/cake")]
public partial class Cake_Feature
{
    [TestMethod]
    [HappyPath]
    public async Task Calling_Create_Cake_Endpoint_Returns_Cake()
    {
        await Given_a_valid_post_request_for_the_Cake_endpoint();
        await When_the_request_is_sent_to_the_cake_post_endpoint();
        await Then_the_response_should_be_successful();
    }

    [TestMethod]
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

[TestClass]
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
- `[TestClass]` — Required on the partial class with steps that inherits from `BaseFixture`.
- Class and method names are converted from underscore-separated to space-separated in reports.

---

## Step 6: Run the Tests

```bash
dotnet test
```

After the tests complete, check the `bin/Debug/net10.0/Reports/` folder:

| File | Description |
|------|-------------|
| `ComponentSpecificationsWithExamples.html` | HTML specifications with embedded PlantUML sequence diagrams |
| `FeaturesReport.html` | HTML test run report with diagrams and execution summary |
| `ComponentSpecifications.yml` | YAML specifications |

---

## Architecture Summary

```
┌─────────────────────────────────┐
│           TestRun               │  ← [TestClass] with [AssemblyInitialize]/[AssemblyCleanup]
│     : DiagrammedTestRun         │     Generates reports in [AssemblyCleanup]
└─────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│          BaseFixture            │  ← Creates tracked HttpClient
│   : DiagrammedComponentTest     │     Sets async-local context on [TestInitialize]
│   IDisposable                   │     Enqueues MSTestScenarioInfo on [TestCleanup]
└─────────────┬───────────────────┘
              │ inherited by
              ▼
┌─────────────────────────────────┐
│  Cake_Feature : BaseFixture     │  ← Your test class with [TestMethod] methods
│  [TestClass]                    │
│  [Endpoint("/cake")]            │
└─────────────────────────────────┘
```

---

## Using PlantUML Overrides

You can customise diagrams within a test using `TrackingDiagramOverride`:

```csharp
using TestTrackingDiagrams.MSTest;

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

## Notes

- MSTest's `TestContext` is not statically accessible like NUnit's `TestContext.CurrentContext` or xUnit v3's `TestContext.Current`. TestTrackingDiagrams.MSTest uses an `AsyncLocal<TestContext>` internally to make the test context available to the HTTP tracking pipeline.
- The `[TestInitialize]` and `[TestCleanup]` methods are provided by `DiagrammedComponentTest`. If your base fixture needs its own initialize/cleanup logic, call the base methods.
- For data-driven tests (`[DataRow]`), each data row is tracked as a separate scenario in the report.
