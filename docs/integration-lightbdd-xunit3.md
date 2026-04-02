# Integration Guide: LightBDD with xUnit 3

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.LightBDD.xUnit3/`](../Example.Api/tests/Example.Api.Tests.Component.LightBDD.xUnit3/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with [LightBDD](https://github.com/LightBDD/LightBDD) using **xUnit v3** as the test runner. After completing this guide, your LightBDD tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams (integrated into LightBDD's report pipeline)
- **YAML specification files**

LightBDD is a BDD framework that lets you write scenarios as C# method calls (`given => ..., when => ..., then => ...`) using its `Runner.RunScenarioAsync` pattern, with support for composite steps, tabular data, and rich reporting.

> **Migrating from LightBDD + xUnit 2?** See [Differences from xUnit 2](#differences-from-xunit-2) at the bottom of this guide.

---

## Prerequisites

- .NET 8.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with LightBDD

---

## Step 1: Create the Test Project

Create a new xUnit v3 test project:

```bash
dotnet new xunit -n MyApi.Tests.Component.LightBDD
```

> **Important:** xUnit v3 test assemblies are standalone executables. Add `<OutputType>Exe</OutputType>` to your `.csproj`:

```xml
<PropertyGroup>
    <OutputType>Exe</OutputType>
</PropertyGroup>
```

---

## Step 2: Install NuGet Packages

```bash
dotnet add package TestTrackingDiagrams.LightBDD.xUnit3
dotnet add package LightBDD.XUnit3
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.v3
dotnet add package xunit.runner.visualstudio
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.LightBDD.xUnit3" Version="1.24.10" />
    <PackageReference Include="LightBDD.XUnit3" Version="3.12.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
</ItemGroup>
```

---

## Step 3: Create the LightBDD Scope Configuration

LightBDD for xUnit v3 uses a `TestPipelineStartup` to configure the test run. Create `Infrastructure/ConfiguredLightBddScope.cs`:

```csharp
using System.Reflection;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using LightBDD.XUnit3;
using TestTrackingDiagrams;
using TestTrackingDiagrams.LightBDD.xUnit3;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(ConfiguredLightBddScope))]

namespace MyApi.Tests.Component.LightBDD.Infrastructure;

public class ConfiguredLightBddScope : LightBddScope
{
    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        var testAssembly = Assembly.GetAssembly(typeof(ConfiguredLightBddScope))!;

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
- `[assembly: TestPipelineStartup(typeof(ConfiguredLightBddScope))]` is required — it tells xUnit v3 to use LightBDD's test pipeline.
- The scope class **extends `LightBddScope`** (a class, not an attribute like xUnit v2's `LightBddScopeAttribute`).
- `CreateStandardReportsWithDiagrams` hooks into LightBDD's native report pipeline, so reports are generated automatically when the test run ends.

---

## Step 4: Create the Base Fixture

Create `Infrastructure/BaseFixture.cs`. All your test classes will inherit from this:

```csharp
using LightBDD.XUnit3;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams.LightBDD.xUnit3;

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
- The test inherits from `FeatureFixture` (LightBDD's base class for xUnit v3).
- `LightBddTestTrackingMessageHandlerOptions` automatically resolves the current test context using LightBDD's `ScenarioExecutionContext`.

---

## Step 5: Write a Scenario Feature Class

LightBDD uses partial classes — one file for the scenario definitions, one for the step implementations.

### `Scenarios/Cake_Feature.cs` (scenario definitions):

```csharp
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

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

Or, since xUnit v3 assemblies are standalone executables, you can run directly:

```bash
bin/Debug/net10.0/MyApi.Tests.Component.LightBDD.exe
```

After the tests complete, check the `bin/Debug/net10.0/Reports/` folder:

| File | Description |
|------|-------------|
| `ComponentSpecificationsWithExamples.html` | HTML specifications with embedded PlantUML sequence diagrams |
| `FeaturesReport.html` | HTML test run report with diagrams and execution summary |
| `ComponentSpecifications.yml` | YAML specifications |

---

## Using PlantUML Overrides

LightBDD's adapter provides a `TrackingDiagramOverride` class for customising diagrams within a test:

```csharp
using TestTrackingDiagrams.LightBDD.xUnit3;

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

## Faking Downstream Dependencies (Correctly)

When your SUT calls downstream HTTP services, those calls must flow through `TestTrackingMessageHandler` to produce proper HTTP-style diagram arrows (with method, status code, headers, body). **Do not** mock service client interfaces and use `MessageTracker` to manually log HTTP interactions — this produces event-style (blue) arrows that are misleading.

Recommended approaches:
- **In-memory fake APIs** — `WebApplicationFactory` instances that serve canned responses (see [Example.Api](../Example.Api/))
- **[JustEat HttpClient Interception](https://github.com/justeattakeaway/httpclient-interception)** — handler-level interception, chain with `TestTrackingMessageHandler`
- **[WireMock.Net](https://github.com/WireMock-Net/WireMock.Net)** — real HTTP server on a random port, map in `PortsToServiceNames`

See the [Tracking Dependencies wiki page](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Tracking-Dependencies#faking-dependencies-getting-proper-http-tracking) for detailed examples of each approach.

---

## Differences from xUnit 2

If you're migrating from the `TestTrackingDiagrams.LightBDD.xUnit2` package, the key changes are:

| Aspect | xUnit 2 | xUnit 3 |
|--------|---------|---------|
| **Package** | `TestTrackingDiagrams.LightBDD.xUnit2` | `TestTrackingDiagrams.LightBDD.xUnit3` |
| **LightBDD package** | `LightBDD.XUnit2` | `LightBDD.XUnit3` |
| **xUnit package** | `xunit` (v2.x) | `xunit.v3` |
| **Scope setup** | `[assembly: ConfiguredLightBddScope]` attribute | `[assembly: TestPipelineStartup(typeof(ConfiguredLightBddScope))]` |
| **Scope base class** | `LightBddScopeAttribute` (attribute) | `LightBddScope` (class) |
| **Parallelisation** | `[assembly: ClassCollectionBehavior(AllowTestParallelization = false)]` | Not needed (xUnit v3 handles this differently) |
| **Output type** | Library (default) | `<OutputType>Exe</OutputType>` required |
| **Namespace** | `TestTrackingDiagrams.LightBDD.xUnit2` | `TestTrackingDiagrams.LightBDD.xUnit3` |

All other concepts (FeatureFixture, Runner.RunScenarioAsync, CompositeStep, TrackingDiagramOverride, etc.) remain the same.

---

## Troubleshooting

### Reports are not generated
- Ensure `[assembly: TestPipelineStartup(typeof(ConfiguredLightBddScope))]` is present at the top of your scope file.
- Ensure `CreateStandardReportsWithDiagrams` is called in `OnConfigure`.
- Check that you have `<OutputType>Exe</OutputType>` in your `.csproj` — xUnit v3 assemblies must be executable.

### Empty specifications HTML / YAML
If any test has failed, the specifications files will be blank by design (they only generate on a fully passing test run). The `FeaturesReport.html` will still be generated.

### ReSharper ignores TestPipelineStartup
Current versions of ReSharper may not recognise `[assembly: TestPipelineStartup]`. Use the Visual Studio Test Explorer or `dotnet test` instead.
