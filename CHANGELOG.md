# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.3.0] - 2026-04-21

### Added
- **`ITrackingComponent` interface**: All tracking handlers and interceptors now implement `ITrackingComponent`, providing `ComponentName`, `WasInvoked`, and `InvocationCount` properties.
- **`TrackingComponentRegistry`**: Central static registry that auto-registers all tracking components on construction.
  - `ValidateAllComponentsWereInvoked()` — Throws a descriptive `InvalidOperationException` if any registered component was never invoked, catching silent misconfiguration (e.g. EF Core `DbContextOptions<T>` type mismatch).
  - `GetUnusedComponents()` / `GetRegisteredComponents()` — Programmatic inspection of component state.
  - `Clear()` — Reset alongside `RequestResponseLogger.Clear()` in test setup.
- **Invocation tracking on all extensions**: `SqlTrackingInterceptor`, `CosmosTrackingMessageHandler`, `BlobTrackingMessageHandler`, `BigQueryTrackingMessageHandler`, `RedisTracker`, and core `TestTrackingMessageHandler` all track invocation counts and auto-register with the registry.

### Documentation
- Added troubleshooting guide to EF Core Relational wiki page covering the `DbContextOptions<TBase>` vs `DbContextOptions<TDerived>` pitfall (Duende IdentityServer, ASP.NET Identity, ABP Framework).
- Added `TrackingComponentRegistry` documentation to Diagnostics and Debugging wiki page.
- Added Invocation Validation sections to all extension wiki pages (CosmosDB, BlobStorage, BigQuery, Redis, EF Core Relational).

## [2.2.1] - 2026-04-21

### Fixed
- **SqlTrackingInterceptor request/response pairing**: `LogCommandExecuting` and `LogCommandExecuted` now share the same `TraceId` and `RequestResponseId` via a `ConcurrentDictionary<DbCommand, (Guid, Guid)>` lookup. Previously each method generated its own IDs, making it impossible for the report generator to pair request and response entries — breaking internal flow popups, component diagram stats, and diagnostic report pairing warnings.

## [2.2.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.BigQuery` package**: Track Google BigQuery REST API operations in test diagrams. Includes:
  - `BigQueryTrackingMessageHandler` — A `DelegatingHandler` that intercepts all BigQuery REST calls and logs them as request/response pairs for diagram generation.
  - `BigQueryOperationClassifier` — Classifies BigQuery REST API URLs into operations (Query, Insert, Read, List, Create, Delete, Update, Cancel) with resource type extraction (table, dataset, job, model, routine, query, tabledata).
  - `BigQueryClientBuilderExtensions.WithTestTracking()` — Extension method on `BigQueryClientBuilder` for one-line integration.
  - Three verbosity levels: Raw (full HTTP), Detailed (classified labels with content), Summarised (operation names only, no content/headers).
  - Default header filtering for noisy Google API headers (Authorization, x-goog-api-client, etc.).

## [2.1.0] - 2026-04-21

### Added
- **`WithTestInfoFrom()` extension** on `SqlTrackingInterceptorOptions`: Copies `CurrentTestInfoFetcher`, `CurrentStepTypeFetcher`, and `CallingServiceName` from an existing `TestTrackingMessageHandlerOptions` instance. Works with all framework adapters (LightBDD, xUnit3, TUnit, MSTest, BDDfy, ReqNRoll) — no framework-specific subclass needed.
- **`services.AddSqlTestTracking(options)`**: DI extension that registers `SqlTrackingInterceptor` as a singleton in `IServiceCollection`.
- **`builder.WithSqlTestTracking(serviceProvider)` overload**: Resolves the interceptor from DI instead of requiring options to be passed directly. Use with `AddSqlTestTracking()` for cleaner `AddDbContext` callbacks.

## [2.0.0] - 2026-04-21

### Release
- **Official 2.0.0 release** — all beta features stabilized.

## [2.0.175-beta] - 2026-04-21

### Fixed
- **ExpectedTestCount guard was blocking all reports**: The partial-run guard introduced in v2.0.174-beta returned early from `CreateStandardReportsWithDiagrams`, preventing all report generation (TestRunReport, ComponentDiagram, etc.) during partial runs. Now it only disables Specifications (HTML + data) generation — TestRunReport and all other reports still generate normally during filtered/partial test runs.

## [2.0.174-beta] - 2026-04-20

### Changed
- **ExpectedTestCount guard moved to core pipeline**: The partial-run guard that prevents Specifications reports from being overwritten during filtered test runs is now a property on `ReportConfigurationOptions.ExpectedTestCount` and enforced in the core `ReportGenerator.CreateStandardReportsWithDiagrams()`. Previously this was LightBDD-specific (`StandardPipelineFormatter.ExpectedTestCount`). All frameworks (xUnit2/3, NUnit4, TUnit, MSTest, BDDfy, ReqNRoll) can now opt in by setting `options.ExpectedTestCount = () => count`. LightBDD adapters continue to wire this automatically via assembly reflection.

## [2.0.173-beta] - 2026-04-20

### Fixed
- **Per-call activity diagrams showing wrong spans**: `InternalFlowSegmentBuilder.BuildSegments()` now filters spans by the specific request log's `ActivityTraceId` in addition to timestamp windowing. Previously, all spans from all trace IDs within a test were pooled and separated only by timestamps — causing spans from one HTTP call to bleed into another call's popup when timing overlapped or was coarse (Windows ~15.6ms timer resolution). Each call's internal flow popup now correctly shows only spans belonging to that call's W3C trace.
- **Root span excluded from per-call diagrams**: Added a 50ms tolerance before the segment start timestamp to capture the `TestTrackingDiagrams.Request` root span, whose `Activity.Start()` fires before the log's `Timestamp = DateTimeOffset.UtcNow` is recorded. Combined with per-call TraceId filtering, this ensures the tolerance doesn't accidentally include unrelated spans.

## [2.0.172-beta] - 2026-04-20

### Fixed
- **LightBDD specs generation guard**: `StandardPipelineFormatter` now correctly prevents Specifications report generation when fewer tests ran than expected (partial run). The comparison operator was inverted (`>` instead of `<`), allowing partial runs to generate the report.
- **Loading message on sequence diagrams**: Unrendered diagrams inside collapsed features no longer show "Waiting for page load to complete..." after the page has loaded. A `body.plantuml-ready` CSS class now switches the message to "Rendering diagram..." once DOMContentLoaded fires, regardless of IntersectionObserver timing.
- **Note collapse breaking zoom toggle**: Collapsing or expanding notes via the radio buttons re-renders the diagram SVG (destroying the zoom button). The zoom button is now re-added via `requestAnimationFrame` after every note-state re-render.
- **Jump-to-failure scroll position**: The "Next Failure" button now scrolls to the scenario's `<summary>` (title) rather than the `<details>` element, and uses `block: 'start'` so the title is visible at the top of the viewport.
- **Showcase test not skipped**: `ShowcaseReportTests.Showcase_drives_through_report_features_capturing_frames` was running as a regular `[Fact]` on every test run (~60s). Now marked with `Skip` like all other GIF/screenshot generation tests.

## [2.0.171-beta] - 2026-04-20

### Fixed
- Activity diagram popups (internal flow) now correctly set `data-queued` and `data-rendered` attributes, preventing the "Waiting for page load to complete..." loading text from showing indefinitely after the diagram has rendered.
- Zoom toggle button reliability improved: the render callback now triggers `addZoomButton` via `requestAnimationFrame` after SVG insertion, and the IntersectionObserver path also defers to `requestAnimationFrame` to ensure layout is complete before checking diagram dimensions.

## [2.0.170-beta] - 2026-04-20

### Added
- Integration tests verifying specifications reports are blanked when any test fails, are not blanked for skipped/bypassed-only runs, and that the test run report is unaffected by failures.

## [2.0.169-beta] - 2026-04-20

### Changed
- **Breaking:** `ReportConfigurationOptions.FeaturesReportShowStepNumbers` renamed to `TestRunReportShowStepNumbers`.
- **Breaking:** `ComponentDiagramOptions.EmbedInFeaturesReport` renamed to `EmbedInTestRunReport`.
- Renamed all remaining "FeaturesReport" references (comments, test data, test class name) to "TestRunReport" for consistency with the actual report output filename.

## [2.0.168-beta] - 2026-04-20

### Added
- Diagram loading placeholders ("Waiting for page load to complete\u2026" and "Rendering diagram\u2026") now pulse with a gentle fade-in/fade-out animation so they feel alive rather than static.

## [2.0.167-beta] - 2026-04-20

### Changed
- First-state loading placeholder changed from "Waiting for page\u2026" to "Waiting for page load to complete\u2026" for clarity.

## [2.0.166-beta] - 2026-04-20

### Changed
- Diagram loading now shows two distinct states: "Waiting for page load to complete\u2026" while CDN scripts download, then "Rendering diagram\u2026" once the diagram enters the render queue. Previously a single "Loading diagram\u2026" message covered both phases. The `data-queued` attribute is now set when a diagram enters the render queue, while `data-rendered` is set only after the SVG has been fully rendered.

## [2.0.165-beta] - 2026-04-20

### Changed
- PlantUML CDN scripts (`viz-global.js`, `plantuml.js`) now load with the `defer` attribute, allowing the browser to parse and render the HTML report while the scripts download in parallel. Previously these blocking scripts stalled all page rendering. Unrendered diagram containers show a "Loading diagram\u2026" placeholder until the WASM engine is ready.
- Diagram zoom buttons are now added lazily via `IntersectionObserver` instead of eagerly scanning all diagram containers on page load. This eliminates hundreds of forced layout reflows (`getBoundingClientRect`) on large reports. A per-container `MutationObserver` waits for the SVG to render before checking whether the diagram needs a zoom toggle.

## [2.0.164-beta] - 2026-04-20

### Reverted
- Removed search data compression (data-search-z / data-row-search-z) introduced in v2.0.162-beta. The gzip+base64 approach only achieved ~22% reduction on real-world reports (not the projected 80%) and added a 30-second decompression delay on page load for large reports. Search attributes are back to plain text (data-search / data-row-search).

## [2.0.161-beta] - 2026-04-19

### Fixed
- Parameterized group row click no longer hides content when multiple features have parameterized groups — the `pgrp` prefix counter was resetting per feature, causing duplicate HTML element IDs across features. `selectRow()` uses global `document.querySelectorAll`, so clicking a row in one feature’s group would hide/show panels from a different feature’s identically-prefixed group.

## [2.0.160-beta] - 2026-04-19

### Added
- Collapsed diagram notes now show a plus (+) button in the top-right corner that expands the note — mirrors the existing bottom-center ▼ expand button. The minus (−) button remains when notes are expanded or truncated. Clicking either the plus button or the ▼ button returns the note to expanded state and restores the minus button.

## [2.0.159-beta] - 2026-04-19

### Fixed
- Parameterized test groups now correctly display the "Happy Path" badge and CSS class — previously the `happy-path` class was only applied to non-parameterized scenarios, so the "Happy Paths Only" filter button and happy-path styling were broken for parameterized groups

## [2.0.158-beta] - 2026-04-19

### Changed
- **BREAKING**: LightBDD adapter now delegates to the standard `ReportGenerator.CreateStandardReportsWithDiagrams()` pipeline — the same pipeline used by every other framework adapter (xUnit, NUnit, MSTest, TUnit, BDDfy, ReqNRoll). This eliminates a chronic source of feature-parity drift where new options and features had to be wired into both the main pipeline and a parallel LightBDD-specific pipeline.
- Removed `UnifiedReportFormatter`, `UnifiedSpecificationsDataFormatter`, `UnifiedTestRunDataFormatter`, and `PostReportActionsFormatter` — replaced by a single `StandardPipelineFormatter` that calls the shared pipeline once
- `ReportWritersConfigurationExtensions.CreateStandardReportsWithDiagramsInternal` reduced from ~180 lines to ~15 lines
- LightBDD now automatically gets component diagram generation, diagnostics, CI summary/artifacts — features that were previously unavailable in the LightBDD path

## [2.0.157-beta] - 2026-04-19

### Fixed
- LightBDD report generation now respects `GenerateSpecificationsReport`, `GenerateTestRunReport`, `GenerateSpecificationsData`, and `GenerateTestRunReportData` option flags — previously all reports were always generated regardless of these settings
- LightBDD adapter now passes `GroupParameterizedTests`, `MaxParameterColumns`, and `TitleizeParameterNames` options through to HTML report generation — previously these options were silently ignored
- LightBDD test run report title now incorporates `FixedNameForReceivingService` / `ComponentDiagramOptions.Title` via `GetTestRunReportTitle()` — previously always defaulted to "Test Run Report"
- LightBDD schema generation was unreachable dead code (placed after `return` statement in v2.0.156-beta) — moved before the return so it actually executes

### Changed
- `ReportGenerator.GetTestRunReportTitle()` visibility changed from `internal` to `public` so LightBDD adapter can use it

## [2.0.156-beta] - 2026-04-19

### Fixed
- LightBDD adapter now extracts all scenario parameters from `INameInfo.NameFormat`, including parameters substituted inline into the scenario name (e.g. `NewPasscode "{0}"`) — previously only bracket-appended parameters were detected
- LightBDD report formatters now generate reports when running a subset of tests (e.g. single test filtering) — previously the `ExpectedTestCount` guard prevented any output when the count didn't match the full assembly total
- LightBDD adapter now generates `TestRunReport.schema.json` (was missing because the schema writer was not registered in the LightBDD report configuration)

## [2.0.155-beta] - 2026-04-19

### Fixed
- ParameterParser now correctly extracts parameters from multiple separate bracket groups (e.g. `[version: "V1"] [claimName: "Sdes"]` as produced by LightBDD for unmatched inline data parameters)
- ExtractBaseName now strips all trailing bracket groups, not just the last one — fixes parameterized group titles retaining leftover brackets
- "All diagrams identical across test cases" badge no longer displays when there is only one test case in a parameterized group

## [2.0.154-beta] - 2026-04-18

### Fixed
- Three broken documentation links in README (CosmosDB, EF Core Relational, Redis) now point to wiki pages instead of non-existent docs/ files
- .NET targeting statement corrected from ".NET 10.0" to multi-target ".NET 8.0, .NET 9.0, and .NET 10.0"

### Added
- Four missing extension packages added to README Extensions table: Blob Storage, DispatchProxy, MediatR, OpenTelemetry
- Wiki links for the four new extensions added to README Documentation section

## [2.0.153-beta] - 2026-04-18

### Added
- GitHub issue templates (bug report and feature request) with structured forms
- Pull request template with checklist (TDD, tests, docs, changelog, version)
- CodeQL security scanning workflow (runs on push, PR, and weekly schedule)

### Changed
- Added explicit least-privilege `permissions: contents: read` to CI and CI Summary Preview workflows

## [2.0.152-beta] - 2026-04-18

### Changed
- Renamed README title from "Test Tracking Diagrams" to "TestTrackingDiagrams" (PascalCase, matching .NET package naming convention)
- Added TTD icon prefix to README title
- Updated nuget-readme.md title to match

## [2.0.151-beta] - 2026-04-18

### Added
- Default TTD favicon for all HTML reports — reports now show the TTD icon in the browser tab without any configuration
- `DefaultFavicon.DataUri` constant containing the base64-encoded SVG icon
- Favicon added to component diagram reports (previously had no favicon support)

### Changed
- `CustomFaviconBase64` now overrides the default TTD favicon instead of switching between a custom favicon and no favicon

## [2.0.150-beta] - 2026-04-18

### Changed
- Updated NuGet package descriptions across all 15 packages to reflect broader tracking capabilities (HTTP, database, cache, events, and more) instead of just "request-responses"
- Core package description now highlights all supported dependency types (Cosmos DB, SQL via EF Core, Redis, events/messages, arbitrary method calls)

## [2.0.149-beta] - 2026-04-18

### Removed
- Removed incorrect Mermaid references from nuget-readme.md (Mermaid is not currently supported)

## [2.0.148-beta] - 2026-04-18

### Changed
- Updated README.md to reflect all tracking capabilities beyond HTTP — now covers Cosmos DB, EF Core SQL, Redis, TrackingProxy, and events/messages
- Updated nuget-readme.md with the same broader language
- Revised ASCII architecture diagram to show CosmosDB, SQL DB, Redis, and proxy dependencies alongside HTTP and events
- Replaced HTTP-specific wording in How It Works, Use Cases, and Deterministic vs AI-Generated Diagrams sections with inclusive language covering all tracked interaction types
- Step 1 (Intercept) now uses a table summarising all six tracking mechanisms

## [2.0.139-beta] - 2026-04-18

### Added
- `.editorconfig` for consistent code style enforcement
- `.gitattributes` for cross-platform line ending normalisation
- `global.json` to pin .NET SDK version (replaces inline CI hack)
- `CHANGELOG.md` following Keep a Changelog format
- `CONTRIBUTING.md` with development workflow and PR guidelines
- `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1)
- `SECURITY.md` with vulnerability reporting guidance
- NuGet package icon (sequence diagram motif with checkmark)
- XML doc comments on all core public API types: `ReportConfigurationOptions`, `TestTrackingMessageHandlerOptions`, `ComponentDiagramOptions`, `ServiceCollectionExtensions`, `WebApplicationFactoryExtensions`

### Fixed
- Removed copy-pasted `.gitignore` entry referencing unrelated project
- Removed 5 orphaned `Example.Api/` files with unresolved git merge conflict markers
- Removed untracked prototype/PoC files from repository root (`generate-poc.ps1`, `prototype-parameterized-grouping.html`)
- Moved `themes/` directory under `examples/` to match reorganised structure
- Removed internal design documents from tracked `docs/` directory
- Updated copyright year in LICENSE to 2023-2026

### Changed
- CI workflows now use committed `global.json` instead of inline SDK pinning

## [2.0.138-beta] - 2026-04-18

### Fixed
- LightBDD.xUnit3 example: added missing `using TestTrackingDiagrams.LightBDD` for `HappyPathAttribute`, `LightBddTestTrackingMessageHandlerOptions`, and `TrackingDiagramOverride`
- ReqNRoll xUnit2/xUnit3 examples: added `TestTrackingDiagrams.ReqNRoll.Core` to `reqnroll.json` binding assemblies so ReqNRoll discovers `[Binding]` hooks

## [2.0.137-beta] - 2026-04-18

### Changed
- Reorganised repository from flat structure into `src/`, `tests/`, `examples/` directories
- Updated all project references and solution file for new structure

## [2.0.0-beta] - 2026

### Added
- Complete rewrite with multi-framework support (xUnit v2/v3, NUnit 4, MSTest, TUnit)
- BDD framework integrations (BDDfy, LightBDD, ReqNRoll)
- Extension packages for CosmosDB, EF Core Relational, Redis, OpenTelemetry, MediatR, Blob Storage, DispatchProxy
- C4-style component diagram generation
- Interactive HTML reports with search, filtering, and zoom
- PlantUML IKVM package for offline rendering
- CI summary integration (GitHub Actions job summaries)
- Inline SVG rendering option
- Internal flow tracking
- Event annotations
- Custom theme support

## [1.x] - 2023–2025

### Notes
- Initial release series. See [GitHub Releases](https://github.com/lemonlion/TestTrackingDiagrams/releases) for detailed history.

[Unreleased]: https://github.com/lemonlion/TestTrackingDiagrams/compare/v2.0.139-beta...HEAD
[2.0.139-beta]: https://github.com/lemonlion/TestTrackingDiagrams/compare/v2.0.138-beta...v2.0.139-beta
[2.0.138-beta]: https://github.com/lemonlion/TestTrackingDiagrams/compare/v2.0.137-beta...v2.0.138-beta
[2.0.137-beta]: https://github.com/lemonlion/TestTrackingDiagrams/releases/tag/v2.0.137-beta
