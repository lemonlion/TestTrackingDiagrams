# Test Tracking Diagrams

Effortlessly autogenerate **PlantUML sequence diagrams** (or Mermaid) from your component and acceptance tests. Tracks HTTP requests between your test caller, your Service Under Test (SUT), and its dependencies, then converts them into diagrams embedded in searchable HTML reports and structured data files.

## Example Output

![Example sequence diagram](https://github.com/user-attachments/assets/43d48a00-ba37-4951-945c-dd75de64c2bb)

Each test that makes HTTP calls through the tracked pipeline automatically produces a sequence diagram showing the full request/response flow between services.

## How It Works

1. **Intercept** — A `TestTrackingMessageHandler` (`DelegatingHandler`) is inserted into the HTTP pipeline, logging every request and response with tracking headers.
2. **Collect** — All logged entries are held in the static `RequestResponseLogger`, capturing method, URI, headers, body, status code, and service names.
3. **Generate** — At the end of the test run, `PlantUmlCreator` (or `MermaidCreator`) groups logs by test ID and converts them into sequence diagram code.
4. **Report** — `ReportGenerator` combines the diagrams with test metadata to produce HTML reports and structured data files.

## Quick Start

```
dotnet add package TestTrackingDiagrams.xUnit3
```

See the [Quick Start guide](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Quick-Start-(xUnit)) for full setup instructions.

## Supported Frameworks

| Framework | Package |
|---|---|
| Core library | `TestTrackingDiagrams` |
| xUnit v3 | `TestTrackingDiagrams.xUnit3` |
| xUnit v2 | `TestTrackingDiagrams.xUnit2` |
| NUnit v4 | `TestTrackingDiagrams.NUnit4` |
| MSTest v3 | `TestTrackingDiagrams.MSTest` |
| TUnit | `TestTrackingDiagrams.TUnit` |
| BDDfy | `TestTrackingDiagrams.BDDfy.xUnit3` |
| LightBDD | `TestTrackingDiagrams.LightBDD.xUnit3` / `.xUnit2` / `.TUnit` |
| ReqNRoll | `TestTrackingDiagrams.ReqNRoll.xUnit3` / `.xUnit2` / `.TUnit` |

### Extensions

| Extension | Package |
|---|---|
| Azure Cosmos DB | `TestTrackingDiagrams.Extensions.CosmosDB` |
| EF Core (Relational) | `TestTrackingDiagrams.Extensions.EfCore.Relational` |
| Redis | `TestTrackingDiagrams.Extensions.Redis` |
| Local PlantUML (IKVM) | `TestTrackingDiagrams.PlantUml.Ikvm` |

## Use Cases

- **Debugging failed tests** — see the exact HTTP call that returned an unexpected response
- **Living documentation** — HTML reports and data files that stay in sync with your tests
- **AI-assisted analysis** — feed deterministic PlantUML to AI tools for accurate reasoning
- **PR reviews** — sequence diagrams make HTTP interaction changes immediately visible
- **Onboarding** — new team members can browse reports to understand service interactions
- **CI integration** — surface results in GitHub Actions / Azure DevOps job summaries

## Documentation

For full documentation, see the **[Wiki](https://github.com/lemonlion/TestTrackingDiagrams/wiki)**.

Key pages:

- [Quick Start (xUnit)](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Quick-Start-(xUnit))
- [Framework Integration Guides](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Framework-Integration-Guides)
- [HTTP Tracking Setup](https://github.com/lemonlion/TestTrackingDiagrams/wiki/HTTP-Tracking-Setup)
- [Diagram Customisation](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Diagram-Customisation)
- [Report Configuration](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Report-Configuration)
- [API Reference](https://github.com/lemonlion/TestTrackingDiagrams/wiki/API-Reference)
- [Example Project](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Example-Project)
