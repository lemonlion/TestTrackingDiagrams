# Integration Guide: LightBDD with xUnit 2

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.LightBDD.xUnit2/`](../Example.Api/tests/Example.Api.Tests.Component.LightBDD.xUnit2/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with [LightBDD](https://github.com/LightBDD/LightBDD) using xUnit as the test runner. After completing this guide, your LightBDD tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams (integrated into LightBDD's report pipeline)
- **YAML specification files**

LightBDD is a BDD framework that lets you write scenarios as C# method calls (`given => ..., when => ..., then => ...`) using its `Runner.RunScenarioAsync` pattern, with support for composite steps, tabular data, and rich reporting.

---

## Prerequisites

- .NET 8.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with LightBDD

---

## Step 1: Create the Test Project

Create a new xUnit test project:

```bash
dotnet new xunit -n MyApi.Tests.Component.LightBDD
```

---

## Step 2: Install NuGet Packages

```bash
dotnet add package TestTrackingDiagrams.LightBDD.xUnit2
dotnet add package LightBDD.XUnit2
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.LightBDD.xUnit2" Version="1.20.0" />
    <PackageReference Include="LightBDD.XUnit2" Version="3.10.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.12" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
</ItemGroup>
```

---

## Step 3: Create the LightBDD Scope Configuration

LightBDD requires a "scope attribute" to configure the test run. Create `Infrastructure/ConfiguredLightBddScopeAttribute.cs`:

```csharp
using System.Reflection;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using LightBDD.XUnit2;
using TestTrackingDiagrams;
using TestTrackingDiagrams.LightBDD.xUnit2;

[assembly: ConfiguredLightBddScope]
[assembly: ClassCollectionBehavior(AllowTestParallelization = false)]

namespace MyApi.Tests.Component.LightBDD.Infrastructure;

internal class ConfiguredLightBddScopeAttribute : LightBddScopeAttribute
{
    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        var testAssembly = Assembly.GetAssembly(typeof(ConfiguredLightBddScopeAttribute))!;

        // Wire up TestTrackingDiagrams report generation into LightBDD's report pipeline
        configuration.ReportWritersConfiguration().CreateStandardReportsWithDiagrams(
            testAssembly,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "My API Specifications"
            });

        // Optional: Register global setup/teardown for HTTP fakes
        configuration.ExecutionExtensionsConfiguration()
            .RegisterGlobalTearDown("dispose factory", BaseFixture.DisposeFactory)
            .RegisterGlobalSetUp("http fakes", StartHttpFakes, DisposeHttpFakes);
    }

    private void StartHttpFakes() { /* start your HTTP fakes here */ }
    private void DisposeHttpFakes() { /* dispose your HTTP fakes here */ }
}
```

**Key points:**
- `[assembly: ConfiguredLightBddScope]` at the top is required — it tells LightBDD to use this configuration.
- `[assembly: ClassCollectionBehavior(AllowTestParallelization = false)]` ensures tests run sequentially (required for accurate diagram tracking).
- `CreateStandardReportsWithDiagrams` hooks into LightBDD's native report pipeline, so reports are generated automatically when the test run ends.

---

## Step 4: Create the Base Fixture

Create `Infrastructure/BaseFixture.cs`. All your test classes will inherit from this:

```csharp
using LightBDD.XUnit2;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams.LightBDD.xUnit2;

namespace MyApi.Tests.Component.LightBDD.Infrastructure;

public abstract class BaseFixture : FeatureFixture, IDisposable
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
                services.TrackDependenciesForDiagrams(new LightBddTestTrackingMessageHandlerOptions
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
            new LightBddTestTrackingMessageHandlerOptions
            {
                FixedNameForReceivingService = ServiceUnderTestName
            });
    }

    public void Dispose() => Client.Dispose();
    public static void DisposeFactory() => SFactory?.Dispose();
}
```

**Key points:**
- The static constructor creates the `WebApplicationFactory` once for all tests.
- Each test instance gets its own `HttpClient` via the instance constructor.
- The test inherits from `FeatureFixture` (LightBDD's base class for xUnit).
- `LightBddTestTrackingMessageHandlerOptions` automatically resolves the current test context using LightBDD's `ScenarioExecutionContext`.

---

## Step 5: Write a Scenario Feature Class

LightBDD uses partial classes — one file for the scenario definitions, one for the step implementations.

### `Scenarios/Cake_Feature.cs` (scenario definitions):

```csharp
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace MyApi.Tests.Component.LightBDD.Scenarios;

[FeatureDescription("/cake")]
public partial class Cake_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Calling_Create_Cake_Endpoint_Successfully()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_post_request_for_the_Cake_endpoint(),
            when => The_request_is_sent_to_the_cake_post_endpoint(),
            then => The_response_should_be_successful());
    }

    [Scenario]
    public async Task Calling_Create_Cake_Endpoint_Without_Eggs()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_post_request_for_the_Cake_endpoint(),
            but => The_request_body_is_missing_eggs(),
            when => The_request_is_sent_to_the_cake_post_endpoint(),
            then => The_response_http_status_should_be_bad_request());
    }
}
```

### `Scenarios/Cake_Feature.steps.cs` (step implementations):

```csharp
using System.Net;
using System.Net.Http.Json;
using MyApi.Tests.Component.LightBDD.Infrastructure;

namespace MyApi.Tests.Component.LightBDD.Scenarios;

public partial class Cake_Feature : BaseFixture
{
    private HttpResponseMessage? _response;

    private async Task A_valid_post_request_for_the_Cake_endpoint()
    {
        // Set up your request data using Client
    }

    private async Task The_request_body_is_missing_eggs()
    {
        // Modify request
    }

    private async Task The_request_is_sent_to_the_cake_post_endpoint()
    {
        _response = await Client.PostAsJsonAsync("cake", /* your request */);
    }

    private async Task The_response_should_be_successful()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task The_response_http_status_should_be_bad_request()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

**Key points:**
- `[FeatureDescription("/cake")]` sets the endpoint label in the report (equivalent to `@endpoint:/cake` in Gherkin).
- `[HappyPath]` marks a scenario as a happy path (from `LightBDD.Contrib.ReportingEnhancements`).
- Steps are regular `async Task` methods — method names are converted to readable text by LightBDD (underscores become spaces).

---

## Step 6: Run the Tests

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

LightBDD's adapter provides a `TrackingDiagramOverride` class for customising diagrams within a test:

```csharp
using TestTrackingDiagrams.LightBDD.xUnit2;

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

> **Setup separation:** When `SeparateSetup = true` is set on `ReportConfigurationOptions`, LightBDD automatically detects the boundary between GIVEN steps and WHEN/THEN steps. HTTP calls made during GIVEN steps are wrapped in a visual "Setup" partition in the diagram — no manual `StartAction()` call is needed.

> **Tip:** `InsertTestDelimiter` is particularly useful when using LightBDD's [Tabular Parameters](https://github.com/LightBDD/LightBDD/wiki/Advanced-Step-Parameters#tabular-parameters) or [TabularAttributes](https://github.com/lemonlion/LightBdd.TabularAttributes), where a single scenario runs multiple iterations. Insert a delimiter between each iteration to clearly separate them in the diagram.

---

## Customisation Options

### ReportConfigurationOptions

Passed to `CreateStandardReportsWithDiagrams`:

| Property | Default | Description |
|----------|---------|-------------|
| `SpecificationsTitle` | `"Specifications"` | Title shown at the top of reports |
| `PlantUmlServerBaseUrl` | `"https://www.plantuml.com/plantuml"` | PlantUML server URL |
| `HtmlSpecificationsFileName` | `"ComponentSpecificationsWithExamples"` | Output filename for specs HTML |
| `HtmlTestRunReportFileName` | `"FeaturesReport"` | Output filename for test run HTML |
| `YamlSpecificationsFileName` | `"ComponentSpecifications"` | Output filename for YAML specs |
| `HtmlSpecificationsCustomStyleSheet` | `null` | Custom CSS appended to specs HTML |
| `ExcludedHeaders` | `[]` | HTTP headers to exclude from diagrams |
| `SeparateSetup` | `false` | When `true`, HTTP calls made during GIVEN steps are wrapped in a visual "Setup" partition in the diagram |
| `HighlightSetup` | `true` | When `true` (and `SeparateSetup` is enabled), the setup partition is rendered with a background colour |

### LightBddTestTrackingMessageHandlerOptions

| Property | Description |
|----------|-------------|
| `CallingServiceName` | Display name for the service making outgoing HTTP calls |
| `FixedNameForReceivingService` | Display name for the service receiving requests (your SUT) |
| `PortsToServiceNames` | Dictionary mapping port numbers to friendly service names. Unmapped ports appear as `localhost_80`, `localhost_5001`, etc. |

---

## Troubleshooting

### Reports are not generated
- Ensure `[assembly: ConfiguredLightBddScope]` is present at the top of your scope attribute file.
- Ensure `CreateStandardReportsWithDiagrams` is called in `OnConfigure`.
- Check that `AllowTestParallelization = false` is set — parallel tests can cause diagram tracking issues.

### Empty specifications HTML / YAML
If any test has failed, the specifications files will be blank by design (they only generate on a fully passing test run). The `FeaturesReport.html` will still be generated.
