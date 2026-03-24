<a name="top"></a>

# Test Tracking Diagrams

Effortlessly autogenerate **PlantUML sequence diagrams** from your component and acceptance tests every time you run them. Tracks the HTTP requests between your test caller, your Service Under Test (SUT), and your SUT dependencies, then converts them into diagrams embedded in searchable HTML reports and YAML specification files.

---

## Table of Contents

- [Example Output](#example-output)
- [How It Works](#how-it-works)
- [Use Cases](#use-cases)
- [Deterministic vs AI-Generated Diagrams](#deterministic-vs-ai)
- [Supported Frameworks & NuGet Packages](#supported-frameworks)
- [Recommended BDD Framework](#recommended-bdd)
- [Documentation](#documentation)

---

## <a name="example-output"></a>Example Output [↑](#top)

[<img width="770" height="1017" alt="image" src="https://github.com/user-attachments/assets/43d48a00-ba37-4951-945c-dd75de64c2bb" />](https://www.plantuml.com/plantuml/uml/j5NDJjj04BvRyZiCBWSGSKa28c2520418PI2258F2A7DxjYnukn6utKYG7so7lf8VONkseO4DnKGgJvvPhwPRtxjtpz_7QMQaSx6YUkiJOX5OmOQrHDeoj1rsgb-JB3ZEl0LfoZrDwKHderedsF6Hn6fJ8eJbIY2Bpn47hPAwvcIkXy_8JGQfUQcW994WaRTA7yOWkqNXdGKomaZDeOPiSdtMEWXxDSDR2tC9DUnah3EBS_6-fGb6MuQ2w7EI8BNpWs1jrMOjZpeUCQCKhpukWxBj9BPU639NSV8N9kSlHEM94WUi1Hu_kewfivOFu9tYccAfE6Qr3GM9KWKoXVT77sYPj17ciOSYsXgLefpJDVs40QaHcMqlAd7kMnpAZ80lrEb2U2yUnl0zZXEHgvJCLhydEqjTAu7VrdOgqlNaNQe54T3xJfbYoDYZvjtfqpZOK_96ZHteCS8clNc7ZJsWjrwK6_0UU_slk9XkP0EBp7LX4dLUajCfY6ItvLSYLX6XtoOoH4A0tITVAqycxONWDTNOtmu8qo73pshSXspBMQWOBDTqWBRWxnxVzVqgS3lZW2ZA5s5t_gzydUjy5dcC54PhKATEyvhpwMFarzVzIqxPoCiWoSOljlMsZ-PQnzhnv8bNxYMmDoQWMuK5sL0MfbDsoppVCYHPamsLBlz-kdgTDxaVimq7ru8cmORx30sE6ZvZHRd_cvJD7qMjfWVYi4-QxA3aDTtoymlP4GeOajWFE-AZzj27RL5JRKZK1a2m7sbxeKYbv_i3QOJ9GMALLPXi5B9vbJ_Pyb7vZN_0_q1003__mC0)

Each test that makes HTTP calls through the tracked pipeline automatically produces a sequence diagram showing the full request/response flow between services.

> **Tip:** You can visually separate the setup (arrange) phase from the action phase using the [`SeparateSetup`](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Diagram-Customisation#setup-separation) flag.

---

## <a name="how-it-works"></a>How It Works [↑](#top)

```
┌─────────────┐     HTTP      ┌─────────────┐     HTTP      ┌─────────────┐
│  Test Code  │ ──────────►   │     SUT     │ ──────────►   │ Dependency  │
│  (Caller)   │ ◄──────────   │  (Your API) │ ◄──────────   │  (Fakes)    │
└─────────────┘               └─────────────┘               └─────────────┘
       │                             │                             │
       │                             │   Event / Message           │
       │                             │ ─────────────────►  ┌──────┴──────┐
       │                             │                     │Event broker │
       │                             │                     │  (Fakes)    │
       │                             │                     └──────┬──────┘
       │                             │                             │
       └───── All HTTP traffic + events/messages are intercepted ──┘
                                     │
                                     ▼
                          ┌─────────────────────┐
                          │ RequestResponseLogger│
                          │  (in-memory log)     │
                          └──────────┬──────────┘
                                     │
                                     ▼
                          ┌─────────────────────┐
                          │   PlantUmlCreator    │
                          │ (generates PlantUML) │
                          └──────────┬──────────┘
                                     │
                                     ▼
                          ┌─────────────────────┐
                          │   ReportGenerator    │
                          │  (HTML + YAML files) │
                          └─────────────────────┘
```

1. **Intercept** — A `TestTrackingMessageHandler` (a `DelegatingHandler`) is inserted into the HTTP pipeline. It logs every request and response, enriching them with tracking headers (test name, test ID, trace ID, caller name). For non-HTTP interactions (events, messages, commands), `MessageTracker` logs them directly to the same in-memory store. See the [Tracking Dependencies](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Tracking-Dependencies) wiki page for a detailed guide on how to configure tracking for every common `HttpClient` pattern.

2. **Collect** — All logged `RequestResponseLog` entries are held in the static `RequestResponseLogger`. Each entry captures the method, URI, headers, body, status code, service names, and a trace ID to correlate requests across services. Events and messages are stored alongside HTTP logs with a distinct `Event` meta type.

3. **Generate** — At the end of the test run, `PlantUmlCreator` groups logs by test ID and converts them into PlantUML sequence diagram code. The code is encoded and rendered via a PlantUML server.

4. **Report** — `ReportGenerator` combines the diagrams with test metadata (features, scenarios, results, BDD steps) to produce three output files: a YAML specification, an HTML specification with diagrams, and an HTML test run report.

---

## <a name="use-cases"></a>Use Cases [↑](#top)

### Debugging failed tests locally and in CI/staging

When a test fails, the sequence diagram shows exactly which HTTP call returned an unexpected response — the status code, headers, and body are all visible in the diagram notes. This eliminates guesswork when diagnosing failures, whether you're debugging locally or triaging a failed CI pipeline run against a staging environment. Instead of adding logging, re-running, and reading through console output, the diagram gives you the full picture in a single image.

### Living documentation for stakeholders, developers, and AI

The generated HTML reports and YAML specifications serve as an always-up-to-date source of truth for how your API behaves. Because they're produced directly from passing tests, they can never drift out of sync with the actual implementation. Stakeholders can browse the HTML reports to understand feature behaviour without reading code. Developers can use them during onboarding or when working in unfamiliar areas of the codebase. AI assistants can consume the YAML specs or PlantUML source to answer questions about service interactions with high accuracy.

### Feeding AI tools for more accurate analysis

The raw PlantUML code behind each diagram is a compact, structured representation of your service's HTTP interactions. You can feed it directly into AI coding assistants, chat interfaces, or documentation generators to give them precise context about how services communicate. This produces significantly better results than asking an AI to infer behaviour from source code alone, because the diagrams capture the actual runtime flow including request/response payloads, status codes, and service names.

### Creating accurate high-level architecture diagrams

The per-test sequence diagrams provide a ground-truth foundation for building higher-level architecture and integration diagrams. Rather than drawing C4 models, system context diagrams, or integration maps from memory (which inevitably drift from reality), you or an AI can derive them from the concrete service interactions captured in the test suite. The PlantUML source is particularly useful here — an AI can aggregate the participants and message flows across multiple test diagrams to produce accurate summary diagrams.

### Reviewing pull requests

When a PR changes HTTP interactions (new downstream calls, modified payloads, changed endpoints), the sequence diagrams in the test reports make the impact immediately visible. Reviewers can compare the before and after diagrams to understand exactly what changed in the service communication, without having to mentally trace through the code.

### Regression detection

If a code change unintentionally alters the HTTP interaction pattern — an extra call to a downstream service, a missing header, a changed payload shape — the updated diagram makes it obvious. The YAML specification files are particularly useful for automated diffing in CI pipelines.

### Onboarding and knowledge transfer

New team members can browse the HTML reports to quickly understand how the system's services interact, what endpoints exist, and what the expected request/response shapes look like — all backed by real, passing tests rather than potentially stale wiki pages.

---

## <a name="deterministic-vs-ai"></a>Deterministic vs AI-Generated Diagrams [↑](#top)

A key advantage of these diagrams is that they are **deterministic** — they are derived directly from actual HTTP traffic captured during test execution, not generated by an AI model. AI-generated diagrams are non-deterministic by nature: they vary between runs, may hallucinate service interactions that don't exist, omit ones that do, or represent payloads inaccurately. The accuracy depends entirely on the model's understanding of your codebase, which is always incomplete.

Because TestTrackingDiagrams captures what actually happened over the wire, the output is a faithful, reproducible record of your system's behaviour. This makes the diagrams and PlantUML source especially valuable as **input to AI tools** — when you give an AI a deterministic, verified diagram as context, it can produce far more accurate outputs for:

- **Debugging** — The AI sees the exact request/response chain that led to a failure, rather than guessing from code paths
- **Code understanding** — The AI can reason about concrete service interactions instead of inferring them from scattered HTTP client registrations and handler code
- **Diagram generation** — The AI can aggregate verified low-level sequence diagrams into accurate high-level architecture diagrams, C4 models, or integration maps
- **Documentation** — The AI can write accurate API behaviour descriptions grounded in real data rather than its own interpretation of the source code

In short: use deterministic diagrams as the source of truth, and let AI tools build on top of that truth rather than trying to reconstruct it.

---

## <a name="supported-frameworks"></a>Supported Frameworks & NuGet Packages [↑](#top)

| Framework | Package | Test Runner | NuGet |
|---|---|---|---|
| **Core library** | `TestTrackingDiagrams` | — | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams)](https://www.nuget.org/packages/TestTrackingDiagrams) |
| **xUnit v3** | `TestTrackingDiagrams.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.xUnit3)](https://www.nuget.org/packages/TestTrackingDiagrams.xUnit3) |
| **xUnit v2** | `TestTrackingDiagrams.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.xUnit2)](https://www.nuget.org/packages/TestTrackingDiagrams.xUnit2) |
| **NUnit** | `TestTrackingDiagrams.NUnit4` | NUnit v4 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.NUnit4)](https://www.nuget.org/packages/TestTrackingDiagrams.NUnit4) |
| **BDDfy** | `TestTrackingDiagrams.BDDfy.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.BDDfy.xUnit3)](https://www.nuget.org/packages/TestTrackingDiagrams.BDDfy.xUnit3) |
| **LightBDD** | `TestTrackingDiagrams.LightBDD.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.LightBDD.xUnit2)](https://www.nuget.org/packages/TestTrackingDiagrams.LightBDD.xUnit2) |
| **ReqNRoll** | `TestTrackingDiagrams.ReqNRoll.xUnit2` | xUnit v2 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.ReqNRoll.xUnit2)](https://www.nuget.org/packages/TestTrackingDiagrams.ReqNRoll.xUnit2) |
| **ReqNRoll** | `TestTrackingDiagrams.ReqNRoll.xUnit3` | xUnit v3 | [![NuGet Version](https://img.shields.io/nuget/v/TestTrackingDiagrams.ReqNRoll.xUnit3)](https://www.nuget.org/packages/TestTrackingDiagrams.ReqNRoll.xUnit3) |

All packages target **.NET 8.0**.

---

## <a name="recommended-bdd"></a>Recommended BDD Framework [↑](#top)

If you're choosing a BDD framework to pair with TestTrackingDiagrams, we recommend **[LightBDD](https://github.com/LightBDD/LightBDD)**.

- **Composite (sub) steps** — LightBDD lets you nest steps inside other steps, creating a hierarchy of abstraction levels. These sub-steps appear in the generated reports, allowing you to read the high-level scenario at a glance and drill down into implementation details only when needed.
- **Pure C#** — Scenarios are plain method calls with refactoring, IntelliSense, and compile-time safety. No `.feature` files to keep in sync.
- **Rich built-in reporting** — LightBDD generates its own HTML reports with step timings, statuses, and categories. TestTrackingDiagrams hooks into this pipeline to embed sequence diagrams directly alongside the scenario results.
- **Parameterised and tabular steps** — First-class support for data-driven steps with inline parameters, verifiable [tabular data](https://github.com/LightBDD/LightBDD/wiki/Advanced-Step-Parameters#tabular-parameters), and [tabular attributes](https://github.com/lemonlion/LightBdd.TabularAttributes), making it easy to express complex test inputs and expected outputs.
- **DI container support** — Native integration with `Microsoft.Extensions.DependencyInjection` and Autofac, which aligns naturally with ASP.NET Core test setups.
- **Active maintenance** — LightBDD is actively maintained with regular releases and good documentation.

That said, all [supported frameworks](#supported-frameworks) work well with TestTrackingDiagrams — pick whichever fits your team best.

---

## <a name="documentation"></a>Documentation [↑](#top)

For full documentation including quick start guides, configuration, customisation, and API reference, see the **[Wiki](https://github.com/lemonlion/TestTrackingDiagrams/wiki)**.

Key pages:
- [Quick Start (xUnit)](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Quick-Start-(xUnit))
- [Framework Integration Guides](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Framework-Integration-Guides)
- [HTTP Tracking Setup](https://github.com/lemonlion/TestTrackingDiagrams/wiki/HTTP-Tracking-Setup)
- [Diagram Customisation](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Diagram-Customisation)
- [Report Configuration](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Report-Configuration)
- [API Reference](https://github.com/lemonlion/TestTrackingDiagrams/wiki/API-Reference)
- [Example Project](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Example-Project)
