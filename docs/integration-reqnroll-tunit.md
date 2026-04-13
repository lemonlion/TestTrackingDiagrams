# Integration Guide: ReqNRoll with TUnit

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.ReqNRoll.TUnit/`](../Example.Api/tests/Example.Api.Tests.Component.ReqNRoll.TUnit/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with [ReqNRoll](https://reqnroll.net/) (the successor to SpecFlow) using **[TUnit](https://tunit.dev/)** as the test runner. After completing this guide, your ReqNRoll BDD tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams and Gherkin steps (Given/When/Then/And/But)
- **YAML specification files** with Gherkin steps included

---

## Prerequisites

- .NET 8.0 SDK or later (recommended: .NET 10.0)
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with ReqNRoll / SpecFlow and Gherkin syntax
- Basic familiarity with [TUnit](https://tunit.dev/)

---

## Step 1: Create the Test Project

Create a new TUnit test project:

```bash
dotnet new tunit -n MyApi.Tests.Component.ReqNRollTUnit
```

**Important:** TUnit uses the Microsoft Testing Platform and requires `<OutputType>Exe</OutputType>`. For .NET 10+, also set `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>MyApi.Tests.Component.ReqNRollTUnit</RootNamespace>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

> **Namespace collision warning:** If your project name ends in `.TUnit`, you **must** set `<RootNamespace>` to a value that does not end in `.TUnit` (e.g. `MyApi.Tests.Component.ReqNRollTUnit` instead of `MyApi.Tests.Component.ReqNRoll.TUnit`). This avoids a namespace collision where Reqnroll's auto-generated code references `TUnit.Core` without the `global::` prefix, causing the compiler to resolve it against your project namespace instead of the TUnit framework.

---

## Step 2: Install NuGet Packages

Add the following packages to your test project:

```bash
dotnet add package TestTrackingDiagrams.ReqNRoll.TUnit
dotnet add package Reqnroll --version 3.3.4
dotnet add package Reqnroll.TUnit --version 3.3.4
dotnet add package TUnit --version 1.33.0
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.ReqNRoll.TUnit" Version="2.0.78-beta" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.5" />
    <PackageReference Include="Reqnroll" Version="3.3.4" />
    <PackageReference Include="Reqnroll.TUnit" Version="3.3.4" />
    <PackageReference Include="TUnit" Version="1.33.0" />
</ItemGroup>
```

---

## Step 3: Add the reqnroll.json Configuration File (Critical)

Create a `reqnroll.json` file in your test project root. **This step is mandatory** — without it, the library's hooks will not be discovered and all tests will fail with `InvalidOperationException: No ReqNRoll scenario is currently executing`.

```json
{
  "$schema": "https://schemas.reqnroll.net/reqnroll-config-latest.json",
  "bindingAssemblies": [
    {
      "assembly": "TestTrackingDiagrams.ReqNRoll.TUnit"
    }
  ],
  "formatters": {
    "html": { "outputFilePath": "reqnroll_report.html" }
  }
}
```

**Why is this needed?** ReqNRoll only auto-discovers `[Binding]` classes in the test project's own assembly. The library's hooks (`ReqNRollTrackingHooks`, `ReqNRollTestRunHooks`) live in the `TestTrackingDiagrams.ReqNRoll.TUnit` assembly, so you must explicitly register it.

The `formatters.html` section enables ReqNRoll's native HTML report. When enabled, the library additionally enhances the report with sequence diagram images attached to each scenario's last step — on top of the standard custom reports that are always generated in the Reports directory. See the [ReqNRoll xUnit2 integration guide](integration-reqnroll-xunit2.md#reqnroll-report-enhancement) for full details on how the enhancement works.

---

## Step 4: Create the Test Setup Hooks

Create a `Hooks/TestSetupHooks.cs` file. This class:

1. Creates a `WebApplicationFactory` for your API and registers diagram tracking
2. Provides each scenario with a tracking-enabled `HttpClient`
3. Generates the reports after all tests complete

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Reqnroll;
using Reqnroll.BoDi;
using TestTrackingDiagrams;
using TestTrackingDiagrams.ReqNRoll.TUnit;

namespace MyApi.Tests.Component.ReqNRollTUnit.Hooks;

[Binding]
public class TestSetupHooks
{
    private const string ServiceUnderTestName = "My API";

    private static WebApplicationFactory<Program>? _factory;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Register diagram tracking for all outgoing HTTP calls
                services.TrackDependenciesForDiagrams(new ReqNRollTestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    // Map ports to friendly service names for diagram labels
                    PortsToServiceNames =
                    {
                        { 80, ServiceUnderTestName },
                        { 5001, "Downstream Service A" },
                        { 5002, "Downstream Service B" }
                    }
                });
            });
        });
    }

    [BeforeScenario]
    public void BeforeScenario(IObjectContainer objectContainer)
    {
        // Create a tracking-enabled HttpClient and register it for injection
        var client = _factory!.CreateTestTrackingClient(
            new ReqNRollTestTrackingMessageHandlerOptions
            {
                FixedNameForReceivingService = ServiceUnderTestName
            });
        objectContainer.RegisterInstanceAs(client);
    }

    [AfterScenario]
    public void AfterScenario(IObjectContainer objectContainer)
    {
        var client = objectContainer.Resolve<HttpClient>();
        client.Dispose();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        // Generate the HTML and YAML reports with diagrams
        ReqNRollReportGenerator.CreateStandardReportsWithDiagrams(
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "My API Specifications"
            });

        _factory?.Dispose();
    }
}
```

### What each hook does:

| Hook | Purpose |
|------|---------|
| `[BeforeTestRun]` | Creates the `WebApplicationFactory` and registers HTTP tracking |
| `[BeforeScenario]` | Creates a fresh tracking `HttpClient` for each scenario and injects it via `IObjectContainer` |
| `[AfterScenario]` | Disposes the `HttpClient` |
| `[AfterTestRun]` | Generates all reports (HTML + YAML) with embedded sequence diagrams |

---

## Step 5: Write a Gherkin Feature File

Create a `.feature` file under a `Features/` directory, e.g. `Features/Cake.feature`:

```gherkin
@endpoint:/cake
Feature: Cake
    As a dessert provider
    I want to create cakes from ingredients
    So that customers can enjoy delicious cakes

@happy-path
Scenario: Calling Create Cake Endpoint Successfully
    Given a valid post request for the Cake endpoint
    When the request is sent to the cake post endpoint
    Then the response should be successful

Scenario: Calling Create Cake Endpoint Without Eggs
    Given a valid post request for the Cake endpoint
    But the request body is missing eggs
    When the request is sent to the cake post endpoint
    Then the response http status should be bad request
```

### Tags that TestTrackingDiagrams uses:

| Tag | Purpose |
|-----|---------|
| `@endpoint:/cake` | Sets the endpoint label on the feature in the report |
| `@happy-path` | Marks the scenario as a "happy path" (highlighted in reports, filterable) |

---

## Step 6: Write Step Definitions

Create a `StepDefinitions/CakeStepDefinitions.cs`. Note that the `HttpClient` is constructor-injected by ReqNRoll's DI container (registered by `TestSetupHooks.BeforeScenario`):

```csharp
using System.Net;
using System.Net.Http.Json;
using Reqnroll;

namespace MyApi.Tests.Component.ReqNRollTUnit.StepDefinitions;

[Binding]
public class CakeStepDefinitions
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;

    public CakeStepDefinitions(HttpClient client)
    {
        _client = client;
    }

    [Given("a valid post request for the Cake endpoint")]
    public async Task GivenAValidPostRequestForTheCakeEndpoint()
    {
        // Set up your request data
    }

    [Given("the request body is missing eggs")]
    public void GivenTheRequestBodyIsMissingEggs()
    {
        // Modify request to remove eggs
    }

    [When("the request is sent to the cake post endpoint")]
    public async Task WhenTheRequestIsSentToTheCakePostEndpoint()
    {
        _response = await _client.PostAsJsonAsync("cake", /* your request */);
    }

    [Then("the response should be successful")]
    public async Task ThenTheResponseShouldBeSuccessful()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then("the response http status should be bad request")]
    public void ThenTheResponseHttpStatusShouldBeBadRequest()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

---

## Step 7: Run the Tests

TUnit uses the Microsoft Testing Platform, so tests can be run with:

```bash
dotnet run --project MyApi.Tests.Component.ReqNRollTUnit
```

Or using `dotnet test` (with `TestingPlatformDotnetTestSupport` enabled):

```bash
dotnet test
```

After the tests complete, check the `bin/Debug/net10.0/Reports/` folder. You should find three files:

| File | Description |
|------|-------------|
| `ComponentSpecificationsWithExamples.html` | HTML specifications with embedded PlantUML sequence diagrams |
| `FeaturesReport.html` | HTML test run report with diagrams and execution summary |
| `ComponentSpecifications.yml` | YAML specifications with Gherkin steps |

---

## Report Features (ReqNRoll-specific)

The ReqNRoll adapter produces richer reports than the XUnit/NUnit adapters because it has access to Gherkin metadata:

- **Gherkin steps** — Each scenario in the HTML report shows its Given/When/Then/And/But steps with keyword highlighting
- **Feature descriptions** — The feature description from the `.feature` file is shown under the feature heading
- **YAML with steps** — The YAML spec includes a `Steps:` array per scenario with the full Gherkin steps

---

## Customisation Options

### ReportConfigurationOptions

Pass these when calling `ReqNRollReportGenerator.CreateStandardReportsWithDiagrams`:

| Property | Default | Description |
|----------|---------|-------------|
| `SpecificationsTitle` | `"Specifications"` | Title shown at the top of reports |
| `PlantUmlServerBaseUrl` | `"https://www.plantuml.com/plantuml"` | PlantUML server URL for diagram rendering |
| `HtmlSpecificationsFileName` | `"ComponentSpecificationsWithExamples"` | Output filename for the specifications HTML |
| `HtmlTestRunReportFileName` | `"FeaturesReport"` | Output filename for the test run HTML |
| `YamlSpecificationsFileName` | `"ComponentSpecifications"` | Output filename for the YAML specs |
| `HtmlSpecificationsCustomStyleSheet` | `null` | Custom CSS to append to the specifications HTML |
| `ExcludedHeaders` | `[]` | HTTP headers to exclude from diagrams |
| `RequestResponsePostProcessor` | `null` | Post-processing function for request/response content in diagrams |
| `SeparateSetup` | `false` | When `true`, HTTP calls made during GIVEN steps are wrapped in a visual "Setup" partition in the diagram |
| `HighlightSetup` | `true` | When `true` (and `SeparateSetup` is enabled), the setup partition is rendered with a background colour |

### ReqNRollTestTrackingMessageHandlerOptions

Pass these when calling `TrackDependenciesForDiagrams` and `CreateTestTrackingClient`:

| Property | Description |
|----------|-------------|
| `CallingServiceName` | Display name for the service making outgoing HTTP calls |
| `FixedNameForReceivingService` | Display name for the service receiving requests (your SUT) |
| `PortsToServiceNames` | Dictionary mapping port numbers to friendly service names. Unmapped ports appear as `localhost_80`, `localhost_5001`, etc. |

> **Setup separation:** When `SeparateSetup = true`, ReqNRoll automatically detects the boundary between GIVEN steps and WHEN/THEN steps. No manual `StartAction()` call is needed.

---

## Key Differences from xUnit3

| Aspect | xUnit3 | TUnit |
|--------|--------|-------|
| **Test runner** | xUnit v3 (out-of-process) | TUnit (Microsoft Testing Platform) |
| **Execution** | `dotnet test` | `dotnet run` or `dotnet test` (with `TestingPlatformDotnetTestSupport`) |
| **NuGet package** | `Reqnroll.xunit.v3` | `Reqnroll.TUnit` |
| **Adapter package** | `TestTrackingDiagrams.ReqNRoll.xUnit3` | `TestTrackingDiagrams.ReqNRoll.TUnit` |
| **Binding assembly** | `TestTrackingDiagrams.ReqNRoll.xUnit3` | `TestTrackingDiagrams.ReqNRoll.TUnit` |
| **Namespace collision** | None | Must set `<RootNamespace>` to avoid `.TUnit` suffix collision |

---

## Faking Downstream Dependencies (Correctly)

When your SUT calls downstream HTTP services, those calls must flow through `TestTrackingMessageHandler` to produce proper HTTP-style diagram arrows (with method, status code, headers, body). **Do not** mock service client interfaces and use `MessageTracker` to manually log HTTP interactions — this produces event-style (blue) arrows that are misleading.

Recommended approaches:
- **In-memory fake APIs** — `WebApplicationFactory` instances that serve canned responses (see [Example.Api](../Example.Api/))
- **[JustEat HttpClient Interception](https://github.com/justeattakeaway/httpclient-interception)** — handler-level interception, chain with `TestTrackingMessageHandler`
- **[WireMock.Net](https://github.com/WireMock-Net/WireMock.Net)** — real HTTP server on a random port, map in `PortsToServiceNames`

See the [Tracking Dependencies wiki page](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Tracking-Dependencies#faking-dependencies-getting-proper-http-tracking) for detailed examples of each approach.

---

## Troubleshooting

### "No ReqNRoll scenario is currently executing"
You are missing the `reqnroll.json` file, or it does not list `TestTrackingDiagrams.ReqNRoll.TUnit` in `bindingAssemblies`. See [Step 3](#step-3-add-the-reqnrolljson-configuration-file-critical).

### Namespace collision: `'Core' does not exist in the namespace`
Your project namespace ends in `.TUnit`, which collides with `TUnit.Core` references in Reqnroll's generated code. Set `<RootNamespace>` to a value that doesn't end in `.TUnit`. See [Step 1](#step-1-create-the-test-project).

### Reports folder is empty
- Ensure `[AfterTestRun]` calls `ReqNRollReportGenerator.CreateStandardReportsWithDiagrams`
- If any test has failed, the specifications HTML and YAML files will be blank (by design — they only generate on fully passing runs). The `FeaturesReport.html` will still be generated regardless.

### And/But keywords show as Given/When/Then
Ensure you are using `TestTrackingDiagrams.ReqNRoll.TUnit` version 2.0.78-beta or later. Earlier versions used `StepDefinitionType` (which collapses keywords) instead of `StepDefinitionKeyword`.

---
