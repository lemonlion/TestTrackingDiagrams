# Integration Guide: ReqNRoll with xUnit v2

> **Example project:** A complete working example is available at [`Example.Api/tests/Example.Api.Tests.Component.ReqNRoll.xUnit2/`](../Example.Api/tests/Example.Api.Tests.Component.ReqNRoll.xUnit2/). You can reference it alongside this guide for a fully working implementation.

---

## Overview

This guide walks you through integrating **TestTrackingDiagrams** with [ReqNRoll](https://reqnroll.net/) (the successor to SpecFlow) using **xUnit v2** as the test runner. After completing this guide, your ReqNRoll BDD tests will automatically generate:

- **PlantUML sequence diagrams** from HTTP traffic between your service and its dependencies
- **HTML reports** with embedded diagrams and Gherkin steps (Given/When/Then/And/But)
- **YAML specification files** with Gherkin steps included

> **xUnit v2 vs v3:** This guide is for projects using xUnit v2. If you are using xUnit v3, see the [ReqNRoll xUnit3 integration guide](integration-reqnroll-xunit3.md) instead. The main differences are the NuGet packages and the absence of `<OutputType>Exe</OutputType>` in this v2 guide.

---

## Prerequisites

- .NET 10.0 SDK or later
- An ASP.NET Core API project to test (your "Service Under Test")
- Basic familiarity with ReqNRoll / SpecFlow and Gherkin syntax

---

## Step 1: Create the Test Project

Create a new xUnit test project:

```bash
dotnet new xunit -n MyApi.Tests.Component.ReqNRoll
```

Your `.csproj` `<PropertyGroup>` should look like this (note: **no** `<OutputType>Exe</OutputType>` — that is only for xUnit v3):

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
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
dotnet add package TestTrackingDiagrams.ReqNRoll.xUnit2
dotnet add package Reqnroll --version 3.3.3
dotnet add package Reqnroll.xUnit --version 3.3.3
dotnet add package xunit --version 2.9.3
dotnet add package xunit.runner.visualstudio --version 2.8.2
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.NET.Test.Sdk
```

Your `<ItemGroup>` should look like this:

```xml
<ItemGroup>
    <PackageReference Include="TestTrackingDiagrams.ReqNRoll.xUnit2" Version="1.23.9" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.12" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="Reqnroll" Version="3.3.3" />
    <PackageReference Include="Reqnroll.xUnit" Version="3.3.3" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
</ItemGroup>
```

> **Note:** `Reqnroll.xUnit` is the xUnit v2 runner. Do **not** use `Reqnroll.xunit.v3` — that is for xUnit v3.

---

## Step 3: Add the reqnroll.json Configuration File (Critical)

Create a `reqnroll.json` file in your test project root. **This step is mandatory** — without it, the library's hooks will not be discovered and all tests will fail with `InvalidOperationException: No ReqNRoll scenario is currently executing`.

```json
{
  "$schema": "https://schemas.reqnroll.net/reqnroll-config-latest.json",
  "bindingAssemblies": [
    {
      "assembly": "TestTrackingDiagrams.ReqNRoll.xUnit2"
    }
  ],
  "formatters": {
    "html": { "outputFilePath": "reqnroll_report.html" }
  }
}
```

**Why is this needed?** ReqNRoll only auto-discovers `[Binding]` classes in the test project's own assembly. The library's hooks (`ReqNRollTrackingHooks`, `ReqNRollTestRunHooks`) live in the `TestTrackingDiagrams.ReqNRoll.xUnit2` assembly, so you must explicitly register it.

The `formatters.html` section enables ReqNRoll's native HTML report. When enabled, the library additionally enhances the report with sequence diagram images attached to each scenario — on top of the standard custom reports that are always generated in the Reports directory. See [ReqNRoll Report Enhancement](#reqnroll-report-enhancement) below.

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
using TestTrackingDiagrams.ReqNRoll.xUnit2;

namespace MyApi.Tests.Component.ReqNRoll.Hooks;

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

namespace MyApi.Tests.Component.ReqNRoll.StepDefinitions;

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

## <a name="reqnroll-report-enhancement"></a>ReqNRoll Report Enhancement

The standard custom reports (HTML specifications, HTML test run report, YAML specs) are always generated in the Reports directory regardless of this setting. When ReqNRoll's built-in HTML formatter is additionally enabled (via the `formatters.html` section in `reqnroll.json`), the library also enhances the generated native ReqNRoll report with sequence diagram images. Each scenario in that report receives:

- A **Sequence Diagram** image attachment (rendered by the PlantUML server)
- A **PlantUML Code** text attachment with the raw diagram source

This works by injecting Cucumber Message `attachment` records into the report's embedded `window.CUCUMBER_MESSAGES` JSON array after the formatter has written the file. Each attachment is linked to the last scenario step (the final Given/When/Then step) so that Cucumber React's rendering pipeline can find and display them. The `ReqNRollReportEnhancer` is registered automatically when you call `ReqNRollReportGenerator.CreateStandardReportsWithDiagrams()` — no additional setup is needed beyond enabling the formatter.

### How to enable

Add the `formatters` section to your `reqnroll.json`:

```json
{
  "formatters": {
    "html": { "outputFilePath": "reqnroll_report.html" }
  }
}
```

After tests complete, the enhanced report will be at `bin/Debug/net10.0/reqnroll_report.html`.

> **Note:** The enhancement runs via `AppDomain.ProcessExit` to ensure the ReqNRoll formatter has finished writing the HTML file before post-processing it.

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
You are missing the `reqnroll.json` file, or it does not list `TestTrackingDiagrams.ReqNRoll.xUnit2` in `bindingAssemblies`. See [Step 3](#step-3-add-the-reqnrolljson-configuration-file-critical).

### Reports folder is empty
- Ensure `[AfterTestRun]` calls `ReqNRollReportGenerator.CreateStandardReportsWithDiagrams`
- If any test has failed, the specifications HTML and YAML files will be blank (by design — they only generate on fully passing runs). The `FeaturesReport.html` will still be generated regardless.

### And/But keywords show as Given/When/Then
Ensure you are using `TestTrackingDiagrams.ReqNRoll.xUnit2` version 1.20.0 or later. Earlier versions used `StepDefinitionType` (which collapses keywords) instead of `StepDefinitionKeyword`.

### Migrating from xUnit v2 to xUnit v3
If you later want to migrate to xUnit v3, see the [ReqNRoll xUnit3 guide](integration-reqnroll-xunit3.md). The key changes are:
1. Replace `Reqnroll.xUnit` with `Reqnroll.xunit.v3`
2. Replace `xunit` with `xunit.v3`
3. Add `<OutputType>Exe</OutputType>` to your csproj
4. Update `reqnroll.json` to reference `TestTrackingDiagrams.ReqNRoll.xUnit3`
5. Change your `using` statements from `TestTrackingDiagrams.ReqNRoll.xUnit2` to `TestTrackingDiagrams.ReqNRoll.xUnit3`
6. Clean `bin/` and `obj/` directories before building
