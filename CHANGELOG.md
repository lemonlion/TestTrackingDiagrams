# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.30.6] - 2026-05-06

### Fixed
- **Cross-platform caller file path extraction**: `Track.That()` source-location extraction now handles Windows-style backslash paths correctly on Linux. Previously, `Path.GetFileName()` on Linux would not strip directory components from paths containing `\`, causing assertion location tooltips to display the full path instead of just the filename.

## [2.30.5] - 2026-05-06

### Added
- **LightBDD inline parameter highlighting**: Step text in the HTML report now renders LightBDD parameters inline within the prose (e.g. "customer has `105` in account" with the value highlighted) instead of appending them as separate badges after the step text. This matches LightBDD's native HTML report behaviour and eliminates the previous duplicative display. Parameters are color-coded by verification status (success/failure/exception) and show a tooltip with the parameter name. When `TextSegments` are present on a step, separate inline parameter badges are suppressed.

## [2.30.4] - 2026-05-06

### Added
- **Assertion source-location tooltips**: `Track.That()`, `Track.That<T>()`, and `Track.ThatAsync()` now capture `[CallerFilePath]` and `[CallerLineNumber]`. The source location is embedded as a PlantUML comment (`'__assertionLoc__:Filename.cs:L42`) in the diagram source. When assertion notes are visible in the HTML report, hovering over an assertion note displays a native browser tooltip showing the source file and line number (e.g. "MyTests.cs L42").
- **AssertionRewriter: caller info pass-through**: When `OriginalFilePath` is set on the rewriter, the generated `Track.That()`/`Track.ThatAsync()` calls include explicit `callerFilePath` and `callerLineNumber` named arguments pointing to the original source file and line, so tooltips reference the correct file even though compilation runs from rewritten intermediate files.

## [2.30.3] - 2026-05-06

### Changed
- **Mobile: zoom controls hidden** — The diagram zoom slider is now hidden on mobile viewports (≤768px) since mobile devices use native pinch-to-zoom.
- **Mobile: context menu as bottom sheet** — The right-click diagram context menu now renders as a bottom-anchored sheet on mobile with larger tap targets (12px padding, 15px font), full-width layout, and slide-up animation. Submenus open on tap instead of hover.

## [2.30.2] - 2026-05-05

### Fixed
- **Report header: CI metadata alignment** — When CI metadata is present, the CI metadata + pie chart group now aligns to the top of the test-summary panel (previously it was vertically centered). When CI metadata is absent, the pie chart remains centered both vertically and horizontally.

## [2.30.1] - 2026-05-05

### Fixed
- **AssertionRewriter: `TrackAssertionsAttribute` now auto-generated at build time** (fixes #31, #33): The `.targets` file now emits `TrackAssertionsAttribute` and `SuppressAssertionTrackingAttribute` source files into the intermediate output path and includes them as `<Compile>` items before compilation. Previously, these types were supposed to be delivered via NuGet `contentFiles` but the packaging was broken — the nupkg contained no `contentFiles/` directory and the nuspec had no `<contentFiles>` metadata section. Users can now just add `[assembly: TrackAssertions]` without defining the attribute type manually.
- **AssertionRewriter: removed broken `contentFiles` packaging** from the `.csproj`. The `<None Pack="true" PackagePath="contentFiles\...">` items were never producing valid content files in the nupkg.

### Documentation
- **Wiki: AssertionRewriter correctly described as MSBuild task** (fixes #32): Replaced 4 incorrect references to "source generator" with accurate MSBuild task terminology. Updated the compatibility note to explain it runs `BeforeTargets="CoreCompile"` and coexists with all source generators. Updated Quick Start version reference and added note that attribute types are auto-generated.

## [2.30.0] - 2026-05-05

### Changed
- **Mouse wheel zoom now requires Ctrl key**: Previously, scrolling the mouse wheel over a selected diagram would zoom without requiring any modifier key. Now `Ctrl+wheel` (or `Cmd+wheel` on macOS) is always required to zoom diagrams, regardless of selection state. Plain mouse wheel scrolling passes through to the page as normal. This prevents accidental zoom when scrolling through reports.

## [2.29.20-beta] - 2026-05-05

### Fixed
- **Parameterized scenario group names not humanized**: When xUnit3 or TUnit tests use `[MemberData]`/`[MethodDataSource]` (tabular input), the scenario group heading in reports now correctly shows "My test scenario name" instead of raw method names like `My_test_scenario_name` or `MyTestScenarioName`. The `OutlineId` is now passed through `ScenarioTitleResolver.FormatScenarioDisplayName()` before being used as the group display name.

## [2.29.19-beta] - 2026-05-05

### Removed
- **`CurrentTestInfo.SafeFetcher`** (TUnit): Reverted. The non-throwing fetcher prevented `TestInfoResolver` from falling through to `TestIdentityScope.Current`/`GlobalFallback` and caused `TestTrackingMessageHandler` to track warmup traffic under garbage identities. The throwing behavior of `Fetcher` is intentional — the exception is caught by all consumers and triggers the correct fallback behaviour. Closes #28 (not planned).

## [2.29.18-beta] - 2026-05-05

### Fixed
- **Vertical drag shaking on zoomed diagrams**: Use `e.clientY` instead of `e.pageY` for drag-to-scroll. `pageY` includes `window.scrollY`, causing oscillation after `scrollBy()` shifts the page.

### Changed
- **Removed `[Obsolete]` from framework adapter `ServiceCollectionExtensions`**: The per-framework `TrackDependenciesForDiagrams` overloads (TUnit, xUnit3, xUnit2, NUnit4, MSTest, LightBDD, BDDfy, ReqNRoll) are no longer marked obsolete. They provide type-safe convenience overloads and are used by the project templates. Fixes #30.

### Removed
- **`CurrentTestInfo.SafeFetcher`** (TUnit): Reverted. The non-throwing fetcher prevented `TestInfoResolver` from falling through to `TestIdentityScope.Current`/`GlobalFallback` and caused `TestTrackingMessageHandler` to track warmup traffic under garbage identities. The throwing behavior of `Fetcher` is intentional — the exception is caught by all consumers and triggers the correct fallback behaviour.

## [2.29.17-beta] - 2026-05-06

### Added
- **Project templates** (`TestTrackingDiagrams.Templates`): 12 `dotnet new` templates for scaffolding component test projects pre-configured with dependency tracking, report generation, and automatic assertion rewriting. Templates: `ttd-xunit3`, `ttd-xunit2`, `ttd-tunit`, `ttd-nunit4`, `ttd-mstest`, `ttd-lightbdd-xunit3`, `ttd-lightbdd-xunit2`, `ttd-lightbdd-tunit`, `ttd-bddfy-xunit3`, `ttd-reqnroll-xunit3`, `ttd-reqnroll-xunit2`, `ttd-reqnroll-tunit`.

## [2.29.16-beta] - 2026-05-05

### Added
- **AssertionRewriter package** (`TestTrackingDiagrams.AssertionRewriter`): Roslyn-based MSBuild task that automatically wraps `.Should()` expression statements in `Track.That(() => ...)` at compile time. Opt in with `[assembly: TrackAssertions]`. Supports `[SuppressAssertionTracking]` attribute and `#pragma warning disable` regions for selective opt-out.

### Changed
- **E2E test parallelization**: Split 388 Playwright tests from a single sequential collection into 6 parallel collections (Zoom, Notes, Search, Diagrams, Reports, Scenarios) + existing FullPipeline. Increased `maxParallelThreads` from 4 to 8. Reduces local E2E execution time from ~20 min to ~9.5 min.

## [2.29.15-beta] - 2026-05-06

### Fixed
- **LightBDD namespace stripping**: Fully-qualified type names (e.g. `Example.Api.Tests.Component.LightBDD.xUnit3.MuffinBatchExpectation`) are now stripped to short names in both scenario display names (timeline labels) and step text. Previously only step text was stripped, leaving namespaces visible in timeline scenario labels and example values.

### Changed
- **Expandable parameter truncation**: Preview text is now truncated to 300 characters (was unlimited), and full JSON content is truncated to 10,000 characters to prevent massive DOM nodes from slow-rendering large payloads.
- **Steps rendered above parameter table**: In scenario detail panels, the step list and failure diagnostics now appear above the parameter table (previously below), improving readability for scenarios with large data tables.
- **Collapsible step tables**: Steps containing inline parameter tables (e.g. ReqNRoll `<table>` arguments) are now rendered inside collapsible `<details>` elements. Column headers are highlighted on hover for easier scanning of wide tables.

## [2.29.14-beta] - 2026-05-05

### Changed
- **CI metadata stacked above pie chart**: When CI metadata is present, it now appears vertically stacked above the summary pie chart (both centered horizontally and vertically within the header row) instead of side-by-side.
- **CI metadata box styling**: The CI metadata panel now has a gray background, rounded corners, and padding matching the test execution summary panel.
- **Violet theme**: CI metadata panel uses the violet background in the violet theme.

## [2.29.13-beta] - 2026-05-05

### Changed
- **Zoom UI simplified**: Removed the zoom toggle button and double-click-to-zoom. Only the zoom slider remains for controlling diagram zoom level.
- **Click-to-deselect**: Clicking a selected diagram now deselects it (previously clicking always selected).
- **Zoom slider repositioned**: Slider now floats at `2em` from top and left of the container (was `6px` from top with `6px` padding-left).
- **Vertical drag scrolls page**: When dragging a zoomed diagram vertically, the page scrolls instead of the container (horizontal drag still pans the container).

## [2.29.12-beta] - 2026-05-05

### Fixed
- **Humanize bug for PascalCase+underscore names**: `StringCasing.Titleize()` now correctly splits PascalCase words in underscore-separated names (e.g. `ParameterizedDiagnostic_Feature` → "Parameterized Diagnostic Feature" instead of "Parameterizeddiagnostic Feature").
- **Preview text no longer includes type name prefix**: `GeneratePreview()` now returns `{ Prop: val, ... }` instead of `TypeName { Prop: val, ... }`. Nested type names are still preserved (e.g. `Ingredients: IngredientSet { ... }`).
- **`GenerateDictionaryPreview` shows nested values**: For dictionaries with nested dict/list values, the preview now shows `{ Key: { nested }, ... }` instead of just `{ Key1, Key2, Key3 }`.
- **Combined table only renders for assertion scenarios**: The combined input→output table is now only shown when there are both setup (Given) and assertion (Then) phase tabular parameters. Pure-input scenarios render tables inline within their steps instead.

### Changed
- **ReqNRoll feature name aligned**: Changed from "Muffins Creation" to "Parameterized Diagnostic Feature" with endpoint `/diagnostic` to match xUnit3/LightBDD.

## [2.29.11-beta] - 2026-05-05

### Fixed
- **`FormatPreviewValue` now handles classes without custom ToString()**: When a nested property value is a class (not a record), and its `.ToString()` returns just the type name, the renderer now recursively generates a rich preview (e.g. `IngredientSet { Flour = Plain Flour, ... }`) instead of displaying the full qualified type name. This fixes LightBDD and any xUnit3 test using plain classes for test data.
- **`GenerateNestedPreview` matches record ToString() style**: Nested previews no longer wrap string values in quotes, matching the output style of C# record auto-generated ToString().

### Changed
- **`GroupScalarsByPrefix` preserves original key names**: Grouped scalar columns now keep their full original names (e.g. `ExpectedIngredientCount`) instead of stripping the prefix and adding spaces (`Ingredient Count`). This matches how xUnit3/LightBDD render typed object property names.
- **ReqNRoll feature file aligned**: Step text changed from "with the following baking profile:" to "the following baking:" so the derived group name is "Baking" (matching the xUnit3 property name). Table headers now use PascalCase (`DurationMinutes`, `PanType`) matching C# conventions. Added `ExpectedHasBakingInfo` column.

## [2.29.10-beta] - 2026-05-05

### Changed
- **ExampleValueGrouper parent nesting**: When multiple step tables are detected and a parent concept is derivable from the step text (e.g. "a muffin recipe ... with the following ingredients:"), all table groups are now nested under a single parent column ("Recipe") as an expandable (R4) cell. This aligns ReqNRoll rendered output with xUnit3/LightBDD wrapper type rendering.
- **ExampleValueGrouper scalar prefix grouping**: Unconsumed Example columns with a common prefix (e.g. `ExpectedIngredientCount`, `ExpectedToppingCount`) are automatically grouped into a sub-table (R3) column ("Expected") with simplified display names ("Ingredient Count", "Topping Count").
- **Unified column headers across all 3 frameworks**: xUnit3, LightBDD, and ReqNRoll now all render as "Recipe Name | Recipe | Expected" for the muffin recipe example tests.
- **Example test data unified**: All 3 framework example tests now use the same 3 recipes (Classic, Rustic Wholesome, Spiced Deluxe) with identical data values.

### Fixed
- **param-expand toggle prefix mismatch**: The `<details class="param-expand">` toggle event handler was constructing the wrong prefix (`'pg' + scenario.id`) instead of reading the correct prefix from the table's `data-prefix` attribute. This caused the detail panel to not become visible when clicking on an expandable cell within a parameterized row.

## [2.29.9-beta] - 2026-05-05

### Added
- **ReqNRoll structured data rendering via `ExampleValueGrouper`**: ReqNRoll Scenario Outline step tables are now automatically grouped into structured objects for rich rendering in the parameterized examples table. Single-row step tables render as sub-tables (R3), multi-row step tables render as expandable details (R4) — matching the same visual output produced by xUnit3 MemberData with complex object parameters.
- **`IDictionary<string, object?>` support in `ParameterValueRenderer`**: All rendering methods (`IsSmallComplexObject`, `IsComplexValue`, `RenderSubTable`, `GeneratePreview`, `GenerateHighlightedJson`, `TryGetFlattenableProperties`, `FlattenToStringValues`, `FlattenToRawValues`) now handle dictionaries as object-like values rather than collections, enabling dictionary-based structured parameter rendering.
- **`ExampleRawValues` property on `ReqNRollScenarioInfo`**: Carries structured raw values (dictionaries/lists) alongside the existing flat `ExampleValues` string dictionary.

### Changed
- **`ReqNRollTrackingHooks.AfterScenario`**: Now calls `ExampleValueGrouper.BuildStructured()` to produce grouped `ExampleValues` and `ExampleRawValues` from flat Example columns and step table data.
- **E2E tests updated**: `ReqNRoll_pipeline_renders_all_scalar_column_headers` and `ReqNRoll_pipeline_all_cells_are_plain_scalar` updated to verify the new structured column headers and rich cell rendering.

## [2.29.8-beta] - 2026-05-05

### Changed
- **Framework-specific argument capture decorators for LightBDD.xUnit3 and LightBDD.TUnit**: Each package now registers its own `IScenarioDecorator` that extracts raw test method arguments directly from the underlying framework's test context (`XUnit3ArgumentExtractor` / `TUnitArgumentExtractor`), falling back to LightBDD's generic `IScenario.Descriptor.Parameters` extraction if the framework context is unavailable. This ensures the same argument extraction logic used by the non-LightBDD adapters is also used when running LightBDD on those frameworks.
- **Added `TUnitArgumentExtractor` in `TestTrackingDiagrams.TUnit`**: Shared helper for extracting raw arguments from TUnit's `TestContext.Metadata.TestDetails.TestMethodArguments`, analogous to `XUnit3ArgumentExtractor` in the xUnit3 package.
- **Added project references from LightBDD framework packages to their base packages**: `LightBDD.TUnit` → `TestTrackingDiagrams.TUnit`, `LightBDD.xUnit2` → `TestTrackingDiagrams.xUnit2`. Enables sharing of argument extractors, `CurrentTestInfo`, and other infrastructure.
- **Core `ArgumentCaptureScenarioDecorator.TryCaptureFromDescriptor()` now `internal static`**: Framework-specific decorators can call this as a fallback when native extraction fails.
- **`CreateStandardReportsWithDiagramsInternal` accepts `registerDefaultDecorator` parameter**: Framework packages that register their own decorator pass `false` to avoid redundant registration.

## [2.29.7-beta] - 2026-05-05

### Changed
- **Framework-agnostic `ArgumentCaptureScenarioDecorator` moved to `LightBDD.Core`**: The decorator now uses `IScenario.Descriptor.Parameters` (LightBDD's own API) to capture raw arguments, making it fully framework-agnostic. Enables rich sub-table rendering for all LightBDD adapters (xUnit3, TUnit, xUnit2) with zero framework-specific code.
- **`LightBddConfiguration.CreateStandardReportsWithDiagrams()` overload added to all LightBDD packages** (xUnit3, TUnit, xUnit2): Consistent API across all frameworks — registers both the report pipeline and the argument capture decorator automatically.
- **`ReportWritersConfiguration.CreateStandardReportsWithDiagrams()` deprecated** across all packages: The old overload still works but produces a compiler warning directing users to the `LightBddConfiguration` overload.
- **`[CaptureLightBddArguments]` attribute deprecated** (xUnit3): The assembly-level attribute is no longer needed when using the new API.
- **BDDfy adapter now captures raw test method arguments**: `DiagramCapturingProcessor` captures `TestMethodArguments` from xUnit3's test context, passing them through `ParameterParser.ExtractStructuredParametersWithRaw()` — the same pipeline used by the non-BDDfy xUnit3 adapter. Enables rich rendering of complex objects in BDDfy parameterized tests.
- **Consolidated shared xUnit3 argument extraction code**: Created `XUnit3ArgumentExtractor` in `TestTrackingDiagrams.xUnit3` — a single shared helper for extracting raw arguments from `XunitTest`/`XunitTestCase`. Used by all three xUnit3-based packages (xUnit3, BDDfy.xUnit3, LightBDD.xUnit3) instead of duplicating the extraction pattern.
- **Added project references for code sharing**: `TestTrackingDiagrams.BDDfy.xUnit3` and `TestTrackingDiagrams.LightBDD.xUnit3` now reference `TestTrackingDiagrams.xUnit3` directly, enabling shared infrastructure (argument extraction, `CurrentTestInfo`) and eliminating code duplication that would be difficult to keep in sync.
- **BDDfy `CurrentTestInfo` delegates to xUnit3 implementation**: Eliminates near-identical copy in the BDDfy package.

## [2.29.6-beta] - 2026-05-05

### Fixed
- **LightBDD adapter: raw argument capture is now automatic — no `[assembly: CaptureLightBddArguments]` attribute required**: Previously, users had to add an explicit assembly-level attribute to enable rich rendering of complex objects (records, lists, nested types) in parameterized LightBDD reports. Without it, class-based types that don't override `ToString()` (e.g. `List<ToppingData>`) rendered as opaque type names. The new `ArgumentCaptureScenarioDecorator` (an `IScenarioDecorator`) is registered automatically when using the `LightBddConfiguration.CreateStandardReportsWithDiagrams()` overload. It captures raw test method arguments from xUnit3's `TestContext.Current` during scenario execution, before arguments are cleared. The legacy `[assembly: CaptureLightBddArguments]` attribute still works but is no longer necessary.

### Changed
- **New `LightBddConfiguration.CreateStandardReportsWithDiagrams(options)` overload**: Direct extension on `LightBddConfiguration` that registers both the report pipeline and the argument capture decorator in one call. Replaces the previous pattern of `configuration.ReportWritersConfiguration().CreateStandardReportsWithDiagrams(options)` — use `configuration.CreateStandardReportsWithDiagrams(options)` instead.
- **LightBDD example updated to class-based types**: Example now uses init-property classes (not records) with 3 recipe variations (Classic, Rustic Wholesome, Spiced Deluxe), matching real-world usage where `ToString()` returns just the type name.

## [2.29.5-beta] - 2026-05-05

### Fixed
- **`Track.That()` now resolves dotted property chains on captured variables**: Previously, `Track.That(() => x.Should().HaveCount(expected.ExpectedIngredientCount))` would resolve `expected` to its full `ToString()` representation (e.g. `'MuffinBatchExpectation { ExpectedIngredientCount =...'.ExpectedIngredientCount`) instead of navigating the property chain to the leaf value. `ClosureValueResolver` now detects dotted property access (e.g. `expected.ExpectedIngredientCount`, `config.Inner.Value`) in the assertion args, walks the chain via reflection, and resolves to the leaf value (e.g. `'5'`). Falls back gracefully when properties don't exist or intermediate values are null.
- **Assertion value substitution now processes longer keys first**: `SubstituteResolvedValues` now sorts resolved-value keys by length descending before substitution, preventing shorter keys (e.g. `expected`) from partially matching before their longer dotted variants (e.g. `expected.ExpectedIngredientCount`).

## [2.29.4-beta] - 2026-05-04

### Fixed
- **LightBDD adapter now captures raw parameter objects for rich rendering**: Previously, LightBDD's result API only exposes `FormattedValue` (ToString() representation), causing complex objects like `List<ToppingData>` to render as type names rather than their actual contents. Added `CaptureLightBddArgumentsAttribute` (assembly-level xUnit3 `BeforeAfterTestAttribute`) and `CapturedScenarioArguments` static store to capture raw test method arguments during execution. The LightBDD report mapper now looks up these captured values and populates `ExampleRawValues`, enabling the same rich R3/R4 rendering (sub-tables, expandable JSON) that xUnit3 and other adapters already support.
- **ReqNRoll scenario outline row ordering now deterministic**: Scenario outline examples sharing the same title were ordered non-deterministically because `.ThenBy(x => x.ScenarioTitle)` provided no differentiation. Added a secondary sort by `ExampleValues` content to ensure stable alphabetical ordering across runs.

## [2.29.3-beta] - 2026-05-04

### Fixed
- **Complex object parameters (nested records, collections) render poorly in string-based path**: When using LightBDD or other frameworks that only provide `ToString()` representations (no raw objects), nested record values like `IngredientSet { Flour = Plain, ... }` were displayed as raw text and `List<T>` properties showed as `System.Collections.Generic.List'1[Namespace.Type]`. Fixed by making `RenderSubTableFromParsed` recursively render nested record values as nested sub-tables and clean collection type names (e.g. `List<ToppingData>`). Also improved the expandable (R4) JSON view and preview text for the same cases.

## [2.29.2-beta] - 2026-05-04

### Changed
- **Zoom: no vertical scrollbar constraint**: Zoomed diagrams now expand to their full natural height instead of being capped at `80vh` with a vertical scrollbar. Only a horizontal scrollbar appears when the diagram is wider than the container.
- **Zoom controls float at top-left while scrolling**: The zoom button and slider now use `position: sticky` so they remain visible at the top-left of the diagram as the user scrolls down through a tall zoomed diagram.

## [2.29.1-beta] - 2026-05-04

### Fixed
- **Parameterized test parameters garbled when values contain square brackets**: `ParameterParser.Parse()` used `LastIndexOf('[')` to find the parameter bracket group, which incorrectly matched `[` characters inside parameter values (e.g. `Items[0].BatchId`). This caused rows 3-5 of parameterized tests with array-index field names to display blank parameter columns with the entire remainder dumped into a single "Arg 0" column. Fixed by requiring ` [` (space-bracket) to identify parameter bracket groups, consistent with `ExtractBaseName()` which already used this pattern.

## [2.29.0-beta] - 2026-05-04

### Added
- **Gradual zoom (beta)**: Diagrams now support smooth, incremental zooming instead of only toggling between fit-to-width and full size.
  - **Diagram selection**: Click a diagram to select it (blue glow indicator). Click elsewhere or press Escape to deselect.
  - **Zoom slider**: A horizontal range slider appears alongside the zoom toggle button on zoomable diagrams. Drag to smoothly zoom between fit-to-width and 100% natural size.
  - **Keyboard zoom**: `Ctrl`+`+` / `Ctrl`+`-` zoom the selected diagram in 5% increments.
  - **Mouse wheel zoom**: `Ctrl`+scroll wheel zooms the diagram under the cursor.
  - **Zoom-to-cursor**: All zoom methods (slider, keyboard, mouse wheel, double-click) scroll the diagram to keep the artifact under the cursor in the same viewport position, so you can zoom into a specific area without losing your place.

## [2.28.45] - 2026-05-04

### Added
- **`TestRunReportTitle` property** on `ReportConfigurationOptions`: allows full customization of the test run report page title. When set, the value is used verbatim — no " - Test Run Report" suffix is appended. When `null` (default), the existing auto-derived logic applies: `ComponentDiagramOptions.Title` → `FixedNameForReceivingService` → `"Test Run Report"`.
- **HTML `<title>` element**: the test run report HTML now includes a `<title>` tag in `<head>`, setting the browser tab title to match the report heading.

## [2.28.44] - 2026-05-04

### Fixed
- **Flaky unit tests under parallel execution**: Fixed shared static state pollution across xUnit parallel test classes. `TrackThatTests` now uses a unique `_testId` per instance (via `Guid.NewGuid()`) instead of a shared constant, preventing log cross-contamination between test methods. `TrackThatIntegrationTests` now filters `trackedLogs` by `testId` before passing to `GenerateHtmlReport`, preventing assertion logs from other parallel tests leaking into report assertions. `MessageTrackerTests` "does_nothing" tests now use unique `CallerName` markers and filter by marker instead of global log count. Added `[Collection("TestIdentityScope")]` to `TestIdentityScopeTests` and `TestInfoResolverTests` to serialize tests that mutate `TestIdentityScope.GlobalFallback`.

## [2.28.43] - 2026-05-04

### Reverted
- **Browser zoom fixed-pixel `max-width` on SVG diagrams** (introduced in v2.28.40): Reverted the SVG `max-width` from a fixed pixel snapshot (`container.clientWidth + 'px'`) back to `100%`. Removed the `snapshotMaxWidth()` function, the `window.resize` recalculation listener, and all pixel-snapshot calls in `addZoomButton`, `toggleDiagramZoom`, and `restoreZoomState`.

## [2.28.42] - 2026-05-04

### Fixed
- **Note hover buttons acting on wrong note**: In diagrams with `entity`, `queue`, or `database` participant types, clicking the minus/plus hover button on one note would collapse/expand a different note further down. The root cause was that `findNoteGroups` misidentified participant shapes (which also render as path+text SVG groups) as note groups, causing note index misalignment. Fixed by adding fold-triangle geometry detection — every PlantUML note has a small triangular fold path at one corner that participants never have. This works regardless of theme or note fill color.
- **`applyAssertionFilter` missing visibility parameter in `setNoteState`**: When toggling individual note state via hover buttons, `applyAssertionFilter` was called without the `showing` parameter, defaulting to assertion-hidden mode regardless of the actual setting.
- **Note state rollback on render failure**: If a PlantUML WASM re-render fails or times out after a note state change, the `_noteSteps` value is now rolled back to the previous state and buttons are resynchronized, preventing visual/state desynchronization.
- **Double-click on note buttons no longer triggers zoom toggle**: Added `dblclick` event suppression on all note hover button rects to prevent fast double-clicks from bubbling to the diagram zoom toggle handler.

## [2.28.41] - 2026-05-03

### Fixed
- **Track.TestIdResolver not set when DiagrammedTestRun is not instantiated**: The v2.28.40 fix only set `Track.TestIdResolver` in `DiagrammedTestRun` constructors, which are never called when users compose their own test setup (e.g. using `DiagrammedTestRun.TestContexts` statically). Moved the resolver initialization into `DiagrammedComponentTest` constructors/SetUp methods (xUnit3, NUnit4, TUnit, MSTest) and `TestTrackingAttribute.Before()` (xUnit2), which run before each test executes.

## [2.28.40] - 2026-05-03

### Fixed
- **Browser zoom now scales diagrams**: Large diagrams that start fit-to-width no longer stay the same physical size when using browser zoom (Ctrl+/-). The SVG `max-width` is now set as a fixed pixel value (snapshot of container width at render time) instead of `100%`, so browser zoom scales the diagram proportionally. A horizontal scrollbar appears when the zoomed diagram overflows. Genuine window resize recalculates the snapshot. The toggle to "natural size" is unaffected.
- **Track.That assertions silently dropped in non-BDDfy packages**: `Track.That()` assertions were not appearing in reports for projects using the standalone framework packages (xUnit3, xUnit2, NUnit4, TUnit, MSTest). The root cause was that `Track.TestIdResolver` was never configured, causing `ResolveTestId()` to return null and `LogAssertion()` to silently discard the assertion. Fixed by setting `Track.TestIdResolver` in each framework's `DiagrammedTestRun` constructor/Setup method. The BDDfy.xUnit3 package was already working correctly.

## [2.28.39] - 2026-05-03

### Fixed
- **Assertion Show/Hide scope bug**: Scenario-level Assertions Show/Hide radio buttons no longer affect other scenarios. Previously, clicking Show on one scenario would cause assertion notes to appear in other scenarios on subsequent re-renders. Assertion visibility is now stored per-container instead of globally.
- **Assertion radio button detection**: Report now correctly shows Assertions radio buttons when assertion notes exist in diagram source (previously only detected via tracked logs)

## [2.28.38] - 2026-05-03

### Changed
- **Radio button labels**: Changed from past-tense (Expanded/Collapsed/Truncated/Shown/Hidden) to imperative (Expand/Collapse/Truncate/Show/Hide) for clearer action-oriented UI

## [2.28.37] - 2026-05-03

### Added
- **Assertion value resolution**: `Track.That()` now resolves runtime values of captured variables via closure inspection and displays them in assertion notes (e.g. `expected` → `'hello-world'`). Falls back to source text for computed expressions, complex objects, or when resolution is not possible.
- **`Track.DiagnosticMode`**: When enabled, records reasons for value resolution fallbacks in `Track.DiagnosticLog` and the DiagnosticReport.html
- **`ClosureValueResolver`**: New internal component that inspects delegate closures to extract captured variable values safely

## [2.28.36] - 2026-05-03

### Fixed
- **Assertion expression formatting**: Removed null-forgiving operators (`!`) from rendered assertion text (e.g. `_auditLogResponse!.StatusCode` no longer shows the `!`)
- **Assertion expression formatting**: Strip leading `_` prefix from field names before humanising (e.g. `_auditLogResponse` renders as "Audit log response")

### Changed
- **Lambda assertion arguments**: Lambda expressions in assertion args are now wrapped in square brackets for readability (e.g. `OnlyContain(x => x.Foo == bar)` renders as "only contain [x => x.Foo == bar]")

## [2.28.35] - 2026-05-03

### Fixed
- **`Track.That()` assertions not appearing in diagrams**: `Track.That()` silently discarded assertions when called from LightBDD, BDDfy, or ReqNRoll test contexts because it could not resolve the test ID from framework-specific execution contexts

### Added
- **`Track.TestIdResolver`**: New static delegate on `Track` that framework integrations use to resolve the current test ID. Set automatically by LightBDD, BDDfy, and ReqNRoll adapters during configuration — no user action needed

## [2.28.34] - 2026-05-03

### Removed
- **Selenium test project**: Removed `TestTrackingDiagrams.Tests.Selenium` — fully replaced by the Playwright-based `TestTrackingDiagrams.Tests.EndToEnd` suite (identical 293-test coverage)
- Removed Selenium CI matrix jobs (4 jobs) — Playwright E2E jobs now provide full browser test coverage in CI

## [2.28.33] - 2026-05-03

### Changed
- **Setup partition background color**: Default changed from `#E2E2F0` to `#F6F6F6` for better visual distinction from participant fills
- **`SetupHighlightColor` configuration**: New property on `DiagramsFetcherOptions` and `ReportConfigurationOptions` allows users to customise the Setup partition background color

### Fixed
- **Collapsible note safety-net**: Removed all hardcoded color exclusions from `hasNoteFill()` — the fill-frequency filter now works universally regardless of PlantUML theme or user-configured colors
- **Safety-net robustness**: Added positional fallback with text-content validation when the fill-frequency filter cannot find a matching count, preventing incorrect note-group mapping in edge cases

## [2.28.32] - 2026-05-03

### Fixed
- `DependencyCategory` is now preserved through the deferred log path (`TrackingLogMode.Deferred`) — previously lost when `PendingLogEntry` was flushed

## [2.28.31] - 2026-05-03

### Added
- `DependencyCategory` property on `TrackingProxyOptions` — allows non-HTTP proxied dependencies (SMTP, gRPC, FTP, etc.) to render with the correct participant shape and colour in component diagrams instead of defaulting to HTTP API styling

## [2.28.30] - 2026-05-05

### Added
- **`Track.That()` inline assertion tracking**: New API (`Track.That(() => expr.Should()...)`) that captures assertion expressions and renders them as styled PlantUML `hnote` annotations in sequence diagrams. Green notes (✓) for passing assertions, red notes (✗) for failures (with failure message). Supports sync, async, and value-returning overloads via `[CallerArgumentExpression]`.
- **`AssertionExpressionFormatter`**: Converts raw `[CallerArgumentExpression]` strings (e.g. `result.Count.Should().Be(3)`) into readable English summaries (e.g. `result count should be 3`). Handles `.Should().` splitting, PascalCase word boundary detection, generic type arguments (`BeOfType<string>()`), `.And.` chaining, lambda arguments, and enum prefix stripping.
- **Assertions toggle UI**: Report-level and scenario-level "Assertions: Show/Hide" radio buttons (hidden by default) that toggle assertion note visibility without page reload. Uses the same queue-based re-render pattern as Details and Headers toggles.
- **Conditional `<<assertionNote>>` PlantUML style**: The custom style block is only emitted when assertion notes are present in the diagram, avoiding unnecessary styling overhead.

## [2.28.29] - 2026-05-04

### Added
- **Playwright E2E test suite**: Migrated all 27 Selenium browser test files to Playwright in `TestTrackingDiagrams.Tests.EndToEnd`. 309 tests pass (28 skipped wiki screenshot/gif tests). Tests run with full parallel execution support.
- **AGENTS.md**: Added agent customization file with Playwright test conventions.

### Fixed
- **Playwright `WaitForFunctionAsync` timeouts under parallel load**: Added `PollingInterval = 200` to all `WaitForFunctionAsync` calls. The default `requestAnimationFrame`-based polling fails when multiple browser contexts run simultaneously in headless Chromium; explicit interval-based polling resolves the issue.
- **SVG interaction helpers**: `DispatchContextMenu()`, JS-dispatched `mouseenter`/`dblclick` for note hover rects, and `FillSearchBar()` with manual `keyup` dispatch — all needed because Playwright's native mouse events don't reliably trigger handlers on SVG elements or `onkeyup`-bound inputs.

## [2.28.28] - 2026-05-01

### Fixed
- **Spanner and Dapper component diagram arrows no longer show full SQL text**: At Raw verbosity, the `Method` field in `RequestResponseLog` entries now contains just the SQL keyword (`Select`, `Insert`, `Update`, `Delete`) instead of the full SQL query text. This was causing component diagram arrows to be hundreds of characters long (e.g. `"Spanner: Insert, InsertOrUpdate, SELECT CustomerId, CustomerName, PreferredMilkType, LikesExtraToppings..."`) instead of the expected short summary (`"Spanner: Insert, InsertOrUpdate, Select"`). Affected extensions: `SpannerTracker` and `TrackingDbCommand` (Dapper). The sequence diagram content is unaffected — full SQL text still appears in the note/content body.

### Added
- **`SpannerOperationClassifier.GetRawKeyword()`**: New method that extracts just the SQL keyword from command text or returns the operation name for non-SQL operations (mutations). Used internally by `SpannerTracker` for the `Method` field.
- **`DapperOperationClassifier.GetRawKeyword()`**: Delegates to `UnifiedSqlClassifier.GetRawKeyword()` for consistent keyword extraction.

## [2.28.27] - 2026-05-01

### Fixed
- **CI: Unit test assertion updated after note button index refactor**: `MakeNotesCollapsible_passes_onTruncate_to_state_1` asserted the old variable name `idx` in the `setNoteState` call; updated to `srcIdx` to match the v2.28.26 refactor.

## [2.28.26] - 2026-05-01

### Fixed
- **Note hover buttons no longer affect the wrong note when header-only notes become empty**: When "Headers: Hidden" was active or a note was collapsed, notes whose entire content was gray HTTP headers (e.g. GET/DELETE request notes) would produce empty PlantUML notes with no SVG text elements. `findNoteGroups` skipped these empty notes, causing an index mismatch between SVG groups and source note blocks - so clicking a button on note N would actually collapse/expand note N-1. Fixed with two layers: (1) `buildSourceWithNoteStates` now inserts a non-breaking space placeholder when a note would otherwise be empty, ensuring PlantUML always renders text for every note; (2) `makeNotesCollapsible` now computes a `sourceIndexMap` when fewer SVG groups are detected than source blocks, mapping each SVG group to the correct source note by checking which notes are visible in the current rendered state.

### Added
- **8 new Selenium tests** (`NoteButtonIndexTests`) verifying note hover button index alignment: groups-vs-blocks equality with headers visible/hidden, minus-button targeting correct note after hide, multiple header-only notes maintaining alignment, collapse/expand with empty notes, and double-click cycling the correct note.

## [2.28.25] - 2026-05-01

### Added
- **4 additional Selenium tests** (`FailureClusterLinkTests`) targeting real-world cluster link navigation bugs found in a v2.22.12 report: parameterized group parent `<details>` opening, parameterized row viewport scroll, sequential click navigation without manual scroll-back, and triple sequential click navigation.

### Changed
- Updated `Selenium.WebDriver.ChromeDriver` from 136.0.7103.9200 to 147.0.7727.11700 to match current Chrome stable.

## [2.28.24] - 2026-05-01

### Fixed
- **Failure cluster links now navigate to the correct scenario when duplicate display names exist across features**: `GenerateScenarioAnchorId` previously produced identical `id` attributes for scenarios with the same display name in different features (e.g. "Health check fails" in both "Order API" and "Payment API"). `getElementById` always returned the first occurrence, so the second cluster link opened the wrong feature. Anchor IDs are now pre-computed with deduplication — the second occurrence receives a `-2` suffix (e.g. `scenario-health-check-fails-2`), the third gets `-3`, etc. The cluster links, scenario permalinks, and element IDs all use the same deduplicated map.

### Added
- **15 new Selenium tests** (`FailureClusterLinkTests`) verifying failure cluster link navigation: basic scroll-to-scenario, parent feature opening, multi-feature navigation, sequential link clicks, parameterized row activation and detail panel visibility, duplicate display name handling, URL hash updates, multi-cluster navigation, and already-expanded-feature edge case.

## [2.28.23] - 2026-05-01

### Fixed
- **`TrackDependenciesForDiagrams` no longer produces unpaired `override.com` entries in diagnostic reports** (fixes [#24](https://github.com/lemonlion/TestTrackingDiagrams/issues/24)): `TestTrackingMessageHandler` now auto-excludes requests to `override.com` (ASP.NET Core TestServer's internal base address) from tracking. These requests are still forwarded normally to the inner handler but produce no log entries. A new `ExcludedHosts` property on `TestTrackingMessageHandlerOptions` (default: `["override.com"]`) allows customising which hosts are excluded. Set to an empty collection to restore previous behavior.

### Added
- **`services.RemoveDbContext<TContext>()` extension method** (fixes [#25](https://github.com/lemonlion/TestTrackingDiagrams/issues/25)): New DI helper in `TestTrackingDiagrams.Extensions.EfCore.Relational` that removes all registrations related to a DbContext type — including `IDbContextOptionsConfiguration<TContext>` (an internal EF Core type that survives `RemoveAll<DbContextOptions<T>>()`). Call this in `ConfigureTestServices` before re-registering the DbContext with a tracking interceptor to ensure no production configuration callbacks survive.

### Documentation
- **EF Core Extension wiki**: Replaced `RemoveAll<DbContextOptions<T>>()` in Option A with `RemoveDbContext<T>()` and added migration warning about the insufficient removal pattern.
- **HTTP Tracking Setup wiki**: Added `ExcludedHosts` to the `TestTrackingMessageHandlerOptions` reference table.
- **Tracking Dependencies wiki**: Added note about automatic `override.com` exclusion in v2.28.23+.

## [2.28.22] - 2026-05-01

### Fixed
- **`CurrentTestInfo.Fetcher` no longer short-circuits the resolution chain when test context is unavailable**: All 8 framework `CurrentTestInfo.Fetcher` implementations (xUnit3, xUnit2, NUnit4, TUnit, MSTest, LightBDD, ReqNRoll, BDDfy) now throw `InvalidOperationException` when the test context is unavailable, instead of returning `TestIdentityScope.UnknownIdentity`. This allows the existing try/catch fallthrough pattern in `MessageTracker.GetTestInfo()`, `TestInfoResolver.Resolve()`, and `RequestResponseLogger.LogPair()` to correctly fall through to `TestIdentityScope.Current` and `GlobalFallback`. Previously, returning `("Unknown", "unknown")` satisfied the non-null check and caused resolution to stop immediately — silently misattributing events instead of reaching the correct fallback.
- **Defense-in-depth: `UnknownIdentity` treated as unresolved in all resolvers**: `TestInfoResolver.Resolve()` (both overloads), `MessageTracker.GetTestInfo()`, and `RequestResponseLogger.LogPair()` now explicitly check for `UnknownTestId` and fall through — regardless of whether the delegate throws or returns the sentinel value. This guards against custom fetcher implementations that return `UnknownIdentity` instead of throwing.

## [2.28.21] - 2026-05-01

### Added
- **`dependencyCategory` parameter on `RequestResponseLogger.LogPair`**: Both overloads now accept an optional `string? dependencyCategory` parameter that is passed through to `RequestResponseLog.DependencyCategory`. This allows manually logged interactions (blob uploads, custom service calls, etc.) to render with the correct participant shape and colour in sequence diagrams (e.g. `database` shape for blob storage) instead of the default generic `entity`.

## [2.28.20] - 2026-04-30

### Changed
- **Release workflow: ~60% faster builds via solution filter and parallelisation**: Replaced the sequential `for proj in src/*/` build and pack loops with a single `dotnet build release.slnf` / `dotnet pack release.slnf` invocation using a new solution filter file (`release.slnf`) that includes all 49 src projects. MSBuild now parallelises compilation across all available cores using its dependency-graph-aware scheduler (GitHub runners have 4 vCPUs). Added a dedicated `dotnet restore` step so NuGet restore happens once with a shared cache. Removed `apt-get clean` and `docker image prune` from the disk cleanup step (the fast `rm -rf` commands alone free 30+ GB and Docker isn't used). Expected total workflow time reduction: ~8m 46s → ~3-4m.

## [2.28.19] - 2026-04-30

### Fixed
- **Eliminated all CI build warnings**: Fixed 10,879 compiler warnings across the solution:
  - Suppressed CS1591 (missing XML doc comment) in `Directory.Build.props` for all src projects — these are informational warnings for undocumented members that don't affect functionality.
  - Fixed CS0419 (ambiguous cref) in `TestPhaseContext.cs`, `KafkaTrackingInterceptor.cs`, and `DiagrammedTestRun.cs` by qualifying overloaded method references with parameter types.
  - Fixed CS1573 (missing param tag) in `MessageTracker.TrackMessageRequest` and `TrackMessageResponse` by adding `<param>` tags for `noteOnRight` and `statusLabel`.
  - Fixed CS1574 (unresolved cref) in `BlobClientOptionsExtensions.cs` by replacing invalid `BlobClientOptions.Transport` cref with inline code reference.
  - Fixed CS1587 (XML comment not on valid element) across 23 files in 8 framework adapter packages by moving XML doc comments before attributes instead of after.
  - Suppressed NU1902/NU1904 (NuGet package vulnerability audit) in SqlClient extension — transitive deps from `Microsoft.Data.SqlClient 2.x` have known vulnerabilities but cannot be updated without breaking consumer compatibility.

## [2.28.18] - 2026-04-30

### Fixed
- **`TrackSendMessage` missing event note styling**: `TrackSendMessage` now sets `MetaType = RequestResponseMetaType.Event` on both request and response log entries, matching `TrackSendEvent`. Previously it used `Default`, causing message payload notes to render with plain white backgrounds instead of the light blue `<<eventNote>>` styling (`BackgroundColor #cfecf7`, `FontSize 11`, `RoundCorner 10`) that visually distinguishes async messaging from synchronous HTTP calls.

## [2.28.17] - 2026-04-30

### Fixed
- **Zoom state lost after note collapse/expand or truncation/headers change**: When a diagram was zoomed to natural size and then notes were collapsed/expanded (via radio buttons or hover buttons) or truncation/headers were toggled, the SVG re-render destroyed inline zoom styles. The zoom button icon also showed the wrong state after re-render. Added `restoreZoomState()` function that re-applies zoom inline styles (`maxWidth: none`, `overflow: auto`, `maxHeight: 80vh`, `cursor: grab`) whenever `addZoomButton` runs after a re-render, and ensures the button icon reflects the current zoom state.

## [2.28.16] - 2026-04-30

### Added
- **`TestIdentityScope.GlobalFallback` for pre-existing background threads**: New static, thread-safe fallback that provides test identity to threads started before `TestIdentityScope.Begin()` was called — such as Cosmos DB Change Feed Processor polling threads, Hangfire workers, and hosted service loops. These threads have their own execution context and never inherit `AsyncLocal` values. `SetGlobalFallback(testName, testId)` sets the fallback; `ClearGlobalFallback()` clears it in teardown. The resolution chain is now four levels: HTTP headers → `CurrentTestInfoFetcher` delegate → `TestIdentityScope.Current` (AsyncLocal) → `TestIdentityScope.GlobalFallback` (static). Both `TestInfoResolver.Resolve()` and `MessageTracker.GetTestInfo()` check `GlobalFallback` as the last resort before returning null. This eliminates the common boilerplate of maintaining a manual shared mutable field with lock in test fixtures.

### Documentation
- **Wiki: Background Thread Correlation** — added Solution 3 (GlobalFallback) with usage example, resolution order table, parallel execution warning, and comparison table vs ActiveTestTracker pattern.

## [2.28.15] - 2026-04-30

### Added
- **`DependencyCategories` constants class**: New static class with 24 public constants for all dependency category strings (CosmosDB, SQL, BigQuery, Redis, ServiceBus, BlobStorage, HTTP, MediatR, MessageQueue, MongoDB, DynamoDB, Elasticsearch, Spanner, Bigtable, Database, S3, CloudStorage, Grpc, PostgreSQL, SqlServer, MySQL, SQLite, Oracle, AtlasDataApi). Replaces magic strings across 40+ files in `DependencyPalette`, all extension trackers/handlers, and options classes.
- **`TrackingDefaults` constants class**: New static class with shared default values — `CallerName` ("Caller") used across 30 options files, and `PlantUmlJsCdnBase` (CDN URL) used in `NodeJsPlantUmlRenderer` and `DiagramContextMenu`.
- **`DependencyCategoriesTests`**: 4 new tests verifying all palette keys have matching constants and all constants are registered in the palette.

### Changed
- All dependency category string literals across 27+ extension tracker/handler files now reference `DependencyCategories.*` constants.
- All `CallerName` default values across 30 options files now reference `TrackingDefaults.CallerName`.
- `DependencyPalette.CategoryToType` dictionary keys now use `DependencyCategories.*` constants.
- `ComponentDiagramGenerator` HTTP string literals now use `DependencyCategories.HTTP`.
- CDN URL in `NodeJsPlantUmlRenderer` and `DiagramContextMenu` now references `TrackingDefaults.PlantUmlJsCdnBase`.

## [2.28.14] - 2026-04-30

### Added
- **`TestIdentityScope.UnknownTestName`, `UnknownTestId`, `UnknownIdentity`**: New public constants for the sentinel test identity values (`"Unknown"` / `"unknown"`) used when no test context is available. All 8 framework adapter `CurrentTestInfo.Fetcher` implementations, `DiagnosticReportGenerator`, and `TestTrackingAttribute` (xUnit v2) now use these constants instead of magic strings. Consumers can reference `TestIdentityScope.UnknownTestId` when filtering diagnostic logs or implementing custom fallback logic.

## [2.28.13] - 2026-04-30

### Added
- **Empty TestContexts warning**: `ReportDiagnostics.Analyse()` now emits a warning when log entries exist but no test contexts (features) were provided. `ReportGenerator` also outputs a console warning and still generates the diagnostic report (when `DiagnosticMode=true`) even when features are empty. This surfaces the most common cause of empty reports: forgetting `DiagrammedTestRun.TestContexts.Enqueue(TestContext.Current)` in `DisposeAsync()`.

### Documentation
- **New wiki page: Background Thread Correlation** — covers `TestIdentityScope`, instance-scoped `ActiveTestTracker`, understanding Unknown entries, `LazyHttpContextAccessor` pattern
- **New wiki page: Service Bus Tracking Patterns** — MessageTracker setup, BeforePublish/AfterPublish bridging, atomic tracking, dual-caller attribution, function trigger correlation
- **HTTP Tracking Setup: Handler Pipeline Ordering** — handler chain diagrams, `PrimaryHandler` vs `AdditionalHandlers`, `IHttpContextAccessor` timing, `CreateTestTrackingClient` behaviour, `_ =>` vs `sp =>` gotcha
- **Multi-Host Test Architectures** — added sections on consistent test identity, HttpContextAccessor wiring order, LazyHttpContextAccessor, initialization order summary
- **Diagnostics and Debugging: Troubleshooting** — 8 new troubleshooting entries for empty reports, Unknown entries, missing Service Bus, function trigger attribution, CosmosDB Unknown, wrong service names
- **Quick Start (xUnit)** — added callout warning about TestContexts.Enqueue for custom fixtures
- **Integration CosmosDB Extension** — added fault injection code example with fixture pattern, background thread correlation link
- **What's New in 2.0** — added "Upgrading from 2.27.x to 2.28.x" migration section (CallerName rename, package alignment)
- **Tracking Custom Dependencies** — added interface-based blob tracking example using auto-resolving `LogPair`
- **Home and Sidebar** — added links to new Service Bus Tracking Patterns and Background Thread Correlation pages

## [2.28.12] - 2026-04-30

### Fixed
- **AtlasDataApi handler: HttpContextAccessor options fallback**: `AtlasDataApiTrackingMessageHandler` now reads `HttpContextAccessor` from the options object when not passed via the constructor parameter, matching the pattern used by all other extension handlers. Previously, setting `options.HttpContextAccessor` had no effect — only the constructor parameter worked.
- **BigQuery handler: HttpContextAccessor options fallback**: Same fix as AtlasDataApi — `BigQueryTrackingMessageHandler` now reads from `options.HttpContextAccessor` as a fallback.
- **EventHubs tracker: double-assignment bug**: `EventHubsTracker` had a duplicate assignment that overwrote the `options.HttpContextAccessor` fallback with just the constructor parameter. Fixed to use the correct `?? options.HttpContextAccessor` pattern.

### Added
- **`ITrackingComponent.HasHttpContextAccessor`**: New default interface member (`bool HasHttpContextAccessor => false;`) indicates whether a tracking component has an `IHttpContextAccessor` configured. Implemented explicitly on all 25+ tracking components. Shown in the diagnostic report's Tracking Components table.
- **`AtlasDataApiTrackingMessageHandlerOptions.HttpContextAccessor`**: New property for setting `IHttpContextAccessor` on the options object (matching CosmosDB, CloudStorage, and other extensions).
- **`BigQueryTrackingMessageHandlerOptions.HttpContextAccessor`**: Same as above for BigQuery.
- **`UnmatchedClientNameRegistry`**: Static registry that records `clientName` values passed to `TestTrackingMessageHandler` that didn't match any `ClientNamesToServiceNames` key. The diagnostic report reads this to surface configuration mismatches.
- **Diagnostic report: Unmatched HTTP Client Names section**: When `DiagnosticMode=true`, shows all unmatched client names with request counts and a fix suggestion explaining exact-match semantics and typed HttpClient naming conventions.
- **Diagnostic report: Grouped tracking components**: Components with the same `ComponentName` are now aggregated into a single row showing total invocations, instance count, and active count. Multiple instances (common with `ICollectionFixture`) are shown with an expandable `<details>` element.
- **Diagnostic report: HttpContextAccessor column**: The Tracking Components table now includes an HttpContextAccessor column showing `✓ configured`, `⚠ null`, or `—` for each component type.
- **Diagnostic report: Smart "never invoked" warnings**: Distinguishes between fully-inactive component types (likely misconfiguration) and partially-inactive types (expected with collection fixtures).

### Documentation
- **Wiki: ClientNamesToServiceNames exact match semantics** (`HTTP-Tracking-Setup.md`): Added section documenting that matching uses exact dictionary lookup, typed HttpClient naming conventions, and the new diagnostic report section.
- **Wiki: AtlasDataApi HttpContextAccessor** (`Integration-AtlasDataApi-Extension.md`): Updated setup example to show the new `HttpContextAccessor` option.
- **Wiki: BigQuery HttpContextAccessor** (`Integration-BigQuery-Extension.md`): Same as above.
- **Wiki: Diagnostic report improvements** (`Diagnostics-and-Debugging.md`): Updated TrackingComponentRegistry section to document grouped table, accessor column, smart warnings, and `HasHttpContextAccessor` interface member.

## [2.28.11] - 2026-04-30

### Added
- **`DiagramMethod` and `DiagramStatusCode` wrapper types**: Named alternatives to `OneOf<HttpMethod, string>` and `OneOf<HttpStatusCode, string>` that avoid the `CS0104` ambiguous reference error when a project also references the popular `OneOf` NuGet package. Both types inherit from the existing `OneOf<T1,T2>` and are assignment-compatible everywhere the base type is used. Use `DiagramMethod method = "Blob Upload";` instead of `TestTrackingDiagrams.Tracking.OneOf<HttpMethod, string> method = "Blob Upload";`.
- **`RequestResponseLogger.LogPair()` auto-resolving overload**: New overload that doesn't require `testName`/`testId` parameters — resolves test identity from an optional `testInfoFetcher` delegate, then falls back to `TestIdentityScope.Current`. Simplifies custom dependency tracking in background processing scenarios.
- **`MessageTracker.TrackSendMessage()`**: Atomic request+response tracking with standard (non-event) arrow styling. Unlike `TrackSendEvent()` which produces event-styled blue arrows, `TrackSendMessage()` produces standard arrows matching HTTP call styling. Ideal for the common `AfterPublish` handler pattern where you want to log a successful send atomically.
- **Diagnostic report: Unknown entries breakdown**: The `DiagnosticReport.html` now includes a dedicated "Unknown Entries Breakdown" section when log entries with test ID `"unknown"` are present. Groups entries by service name and method with counts and timestamp ranges, making it easy to identify which background operations (change feed processor, hosted services, etc.) are producing unattributed tracking entries.

### Documentation
- **Wiki: OneOf type ambiguity avoidance** (`Tracking-Custom-Dependencies.md`): Added section covering `DiagramMethod`/`DiagramStatusCode`, `using` aliases, and fully-qualified name patterns.
- **Wiki: Auto-resolving LogPair** (`Tracking-Custom-Dependencies.md`): Added section showing the new overload with `TestIdentityScope` fallback.
- **Wiki: IDistributedCache tracking example** (`Tracking-Custom-Dependencies.md`): Complete manual decorator example for `IDistributedCache` with hit/miss tracking and dual test-identity resolution.
- **Wiki: CosmosDB InMemoryEmulator integration** (`Integration-CosmosDB-Extension.md`): Added section covering `WrapHandler()` pattern, two-phase HttpContextAccessor setup, recommended verbosity, and fault injection visibility.
- **Wiki: When ReplaceWithTracked won't work** (`Integration-DispatchProxy-Extension.md`): Added section documenting the DI bypass limitation with detection guidance via DiagnosticReport.
- **Wiki: Multi-Host Test Architectures** (new page): Covers dual-host pattern (WebApplicationFactory + FunctionTestServer), shared InMemoryMessaging, DI ordering gotcha, cross-container tracker bridging, and shared singleton patterns.
- **Wiki: JustEat.HttpClientInterception integration** (`HTTP-Tracking-Setup.md`): Added complete `IHttpMessageHandlerBuilderFilter` recipe for combining TTD tracking with JustEat HTTP mocking.

## [2.28.10] - 2026-04-30

### Changed
- **Mobile: Diagram toggle buttons render as compact squares**: The "Sequence Diagrams", "Activity Diagrams", and "Flame Chart" buttons now have `max-width: 5.5em; text-align: center` at ≤768px, causing the two-word labels to wrap vertically into square-shaped buttons (e.g. "Sequence\nDiagrams"). Desktop layout is unaffected — the rule is entirely inside a media query.

## [2.28.9] - 2026-04-30

### Fixed
- **Gray header color lost on wrap overflow**: Reduced the header chunk size from 100 to 80 characters in `BatchGray` and `FormatFormUrlEncodedContent`. At the previous 100-char chunk size, PlantUML's `wrapWidth 800` (set by the library) would wrap lines at the pixel boundary, and the continuation text lost its `<color:gray>` color tag — rendering overflow header text in black instead of gray.

## [2.28.8] - 2026-04-30

### Fixed
- **Mobile: Details/Headers options overflowing off-screen**: The report-level "Details:" and "Headers:" toggle sections in `.toolbar-right` now wrap within the viewport on mobile. Added `flex-wrap: wrap` and `width: 100%` to `.toolbar-right`, and removed the fixed `margin-left: 1.5em` from `.headers-radio` at ≤768px so toggles flow naturally onto the next line.
- **Mobile: Scenario-level diagram toggle overflow**: The `.diagram-toggle` row (Sequence Diagrams / Activity Diagrams / Flame Chart + Details/Headers) now wraps at ≤768px. The `.diagram-toggle-spacer` that pushed Details/Headers to the far right is hidden on mobile, allowing items to flow naturally within the container.
- **Mobile: Summary chart (green circle) not centred**: Changed `.summary-chart` from `align-self: flex-start` to `align-self: center` at ≤768px so the pass/fail donut chart is horizontally centred when the header row stacks vertically.

## [2.28.7] - 2026-04-30

### Fixed
- **Activity diagram loading text duplication**: Fixed bug where activity diagrams displayed both "Rendering Diagrams..." (from CSS `::before` pseudo-element) and "Loading..." (from inner div text) simultaneously. Removed redundant inner text from plantuml-browser divs in `InternalFlowHtmlGenerator` and `ComponentDiagramReportGenerator`.

## [2.28.6] - 2026-04-30

### Added
- **`TestIdentityScope` for background thread tracking**: New `AsyncLocal`-based ambient scope that propagates test identity into background threads, hosted services, change-feed subscribers, and other code paths where neither `HttpContext` nor the test framework's `TestContext` is available. Use `TestIdentityScope.Begin(testName, testId)` to wrap background processing that is logically part of a test. All tracking extensions now check `TestIdentityScope.Current` as a third-level fallback after HTTP headers and `CurrentTestInfoFetcher`. Resolution order: HTTP headers → delegate → `TestIdentityScope`.
- **`TestInfoResolver` triple-resolution**: Both `Resolve()` overloads and `MessageTracker.GetTestInfo()` now fall back to `TestIdentityScope.Current` when the delegate returns null or throws, before returning null (which causes tracking to be silently skipped).

### Documentation
- **CosmosDB Extension wiki**: Added deferred `HttpContextAccessor` assignment pattern for `WrapHandler` scenarios where DI doesn't exist at fixture construction time.
- **Tracking Custom Dependencies wiki**: Added "Tracking Background Processing with TestIdentityScope" section with resolution order table, usage examples, nesting behavior, and AsyncLocal propagation notes. Added "Suppressing Tracking During Fixture Setup" section showing `TestPhaseContext.Current = TestPhase.Setup` combined with `TrackDuringSetup = false`.

## [2.28.5] - 2026-04-30

### Added
- **Mobile-responsive HTML reports**: `TestRunReport.html` and `Specifications.html` now adapt to mobile and tablet viewports without any visible change to the existing desktop layout. Added `<!DOCTYPE html>`, `<meta charset="utf-8">`, and `<meta name="viewport">` to the HTML template. Two CSS `@media` breakpoints (768px and 480px) stack the header row, toolbar, and filter rows vertically, shrink the summary chart, make wide tables horizontally scrollable, and reduce button/badge font sizes on small screens. The jump-to-failure FAB remains accessible on mobile.
- **10 new Selenium tests** (`MobileResponsiveTests`) verifying viewport meta tag presence, vertical stacking of header/toolbar/filters at 375px width, no horizontal page overflow, table scroll behavior, filter box full-width, jump-to-failure visibility, and correct restoration of row layout at 1920px desktop width.

## [2.28.4] - 2026-04-30

### Changed
- **Selenium DiagramNoteTests split into 4 parallel classes**: The monolithic `DiagramNoteTests` class (59 tests) has been split into `DiagramNoteBasicTests` (16 tests), `DiagramNoteLongNoteTests` (15 tests), `DiagramNotePartitionTests` (12 tests), and `DiagramNoteSplitTests` (16 tests), all extending a shared `DiagramNoteTestBase` base class. Each class gets its own `IClassFixture<ChromeFixture>` instance, enabling xUnit to run them in parallel with 4 Chrome browsers instead of 1. Reduces Selenium wall-clock time from ~2m37s to ~37s locally.

## [2.28.3] - 2026-04-30

### Fixed
- **Selenium `StaleElementReferenceException` in DiagramNoteTests**: `GetSvgHtml()`, `DoubleClickFirstNoteAndWait()`, `ClickNoteButton()`, `ClickDownArrowAndWait()`, and `Long_note_up_arrow_click_goes_to_truncated` now use retry-based `WebDriverWait` loops that catch `StaleElementReferenceException` when the SVG DOM is replaced between element lookup and attribute/interaction. `ClickNoteButton()` and `ClickDownArrowAndWait()` also use `HoverNoteRect(0)` (existing retry helper) instead of raw `FindElement` + `MoveToElement`, and JS-dispatched clicks instead of native `.Click()` to avoid SVG path element interception.

## [2.28.2] - 2026-04-30

### Added
- **Comprehensive XML documentation**: Added `/// <summary>` XML doc comments to every public type across all 49 packages — core library, 25 extension packages, and 15 framework adapter packages. This enables full IntelliSense support for NuGet consumers.
- **XML documentation file generation**: Enabled `<GenerateDocumentationFile>` in `Directory.Build.props` for all src projects, so `.xml` doc files are included in NuGet packages automatically.

## [2.28.1] - 2026-04-29

### Deprecated
- **`CallingServiceName` → `CallerName`**: The `CallingServiceName` property on all 29 options classes (`TestTrackingMessageHandlerOptions`, `MessageTrackerOptions`, `TrackingProxyOptions`, `MediatorTrackingOptions`, `SqlTrackingOptionsBase`, and all 24 extension options) has been deprecated with an `[Obsolete]` attribute. Use `CallerName` instead — it is functionally identical. The deprecated property proxies to `CallerName` via `get => CallerName; set => CallerName = value;`, so existing code continues to work with a compile-time `CS0618` warning. `CallingServiceName` will be removed in a future major version.

### Changed
- All internal code, tests, examples, and documentation now use `CallerName` exclusively.
- All 49 wiki pages updated to reference `CallerName`.

## [2.28.0] - 2026-04-28

### Added
- **Direct database tracking extensions for 5 popular databases**: New NuGet packages providing first-class SQL operation tracking without depending on EF Core or Dapper:
  - **`TestTrackingDiagrams.Extensions.Npgsql`** — PostgreSQL tracking via Npgsql's built-in `DiagnosticSource` instrumentation. Subscribes to the `"Npgsql"` diagnostic listener and correlates `BeforeExecuteCommand`/`AfterExecuteCommand` events.
  - **`TestTrackingDiagrams.Extensions.SqlClient`** — SQL Server tracking via `Microsoft.Data.SqlClient`'s `DiagnosticSource`. Subscribes to `"SqlClientDiagnosticListener"` and handles both `WriteCommand*` and legacy `Execute*` event patterns.
  - **`TestTrackingDiagrams.Extensions.MySqlConnector`** — MySQL tracking via MySqlConnector's `DiagnosticSource`. Subscribes to the `"MySqlConnector"` diagnostic listener.
  - **`TestTrackingDiagrams.Extensions.Sqlite`** — SQLite tracking via `DbConnection` wrapping decorator pattern (no `DiagnosticSource` available). Intercepts all 6 execution paths (ExecuteReader/NonQuery/Scalar × sync/async), plus transaction begin/commit/rollback.
  - **`TestTrackingDiagrams.Extensions.Oracle`** — Oracle tracking via `DbConnection` wrapping decorator pattern (no `DiagnosticSource` available). Same 6-method interception + transaction tracking as SQLite.
- **`UnifiedSqlClassifier`** in core package: Shared SQL operation parser supporting all major dialects (SQL Server brackets, PostgreSQL/MySQL quotes, MySQL backticks, Spanner hints). Classifies 16 operation types including DDL, upserts (5 patterns), stored procedures, and transactions.
- **`SqlDiagnosticTracker` base class** in core package: Abstract tracker with command correlation (ConcurrentDictionary-based), phase-aware tracking, test info resolution, and variant attachment. Shared by all 5 database extensions.
- **`SqlTrackingOptionsBase`** in core package: Common configuration for all SQL trackers (service name, verbosity, parameter logging, phase-aware setup/action tracking, excluded operations).
- **DI integration**: Each DiagnosticSource extension provides `AddXxxTestTracking(options?)` for dependency injection. Each wrapping extension provides `DecorateAll<DbConnection>` with type-check guards. All extensions also support static `EnsureTracking()` or `connection.WithTestTracking()` for non-DI usage.
- **`DependencyPalette`**: Added category mappings for `"PostgreSQL"`, `"SqlServer"`, `"MySQL"`, `"SQLite"`, and `"Oracle"` — all resolve to the `Database` participant shape.

### Changed
- **`SqlOperationClassifier` (EfCore.Relational)**: Refactored to delegate to `UnifiedSqlClassifier` internally. Same public API, no breaking changes. Benefits from unified improvements (e.g., CALL proc name parenthesis stripping).
- **`DapperOperationClassifier`**: Refactored to delegate to `UnifiedSqlClassifier` internally. Same public API, no breaking changes. Now correctly classifies `COMMIT` and `ROLLBACK` operations (previously returned `Other`). Stored procedures invoked via `EXEC` now populate `TableName` with the proc name.
- **`UnifiedSqlClassifier.ExtractProcName`**: Now strips parenthesised arguments from CALL syntax (e.g., `CALL my_proc(1)` → `my_proc`).

## [2.27.20] - 2026-04-28

### Fixed
- **`TrackConsumeEvent()` payload note placement**: The request payload note was rendered on the left (`note left`), which placed it outside the diagram when the broker was the leftmost participant. Consume event notes now render on the right (`note right`), correctly placing the payload between the broker and consumer participants. A new `NoteOnRight` property on `RequestResponseLog` controls this; `TrackConsumeEvent` sets it automatically.
- **`IsCurrentRequestFromMyHost()` fails for keyed/manually constructed trackers**: The method resolved `MessageTracker` from `HttpContext.RequestServices` via `GetService(typeof(MessageTracker))`, which returned `null` for keyed singletons (`AddKeyedSingleton("Kafka", ...)`) and manually constructed trackers. Now compares `IHttpContextAccessor` references instead — each DI container has its own singleton accessor, so the comparison works for keyed, non-keyed, and manually constructed trackers alike.

## [2.27.19] - 2026-04-28

### Added
- **`TrackConsumeEvent()` method on `MessageTracker`**: Models message consumption (broker → consumer) as a delivery + acknowledgement pair. Arrow direction is `CallingServiceName` → `consumerName`, with the payload on the delivery arrow and a customisable ack label (default: `"Ack"`). This is the consumption counterpart to `TrackSendEvent()`.
- **`CallerDependencyCategory` property on `MessageTrackerOptions`**: Controls the PlantUML participant shape of the `CallingServiceName` participant independently of `DependencyCategory`. For consumption scenarios, set `CallerDependencyCategory = "MessageQueue"` so the broker renders as a `queue` without affecting the SUT's shape.
- **`CallerDependencyCategory` field on `RequestResponseLog`**: Propagated through the rendering pipeline to `PlantUmlCreator`, enabling correct shape and colour resolution for caller participants.
- **`IsCurrentRequestFromMyHost()` method on `MessageTracker`**: Returns `true` only when the current `HttpContext` belongs to the same DI container that created the tracker. Use in multi-`WebApplicationFactory` scenarios to prevent duplicate tracking from shared in-memory message stores.
- **PlantUmlCreator caller shape rendering**: Caller participants with `CallerDependencyCategory` set are now rendered using `DependencyPalette` shapes and colours, rather than always defaulting to `actor`/`entity`. Arrow colours also fall back to the caller's category when the service has no category.
- **Comprehensive wiki documentation**: DependencyCategory reference table with all 18+ recognised values, participant naming rules, arrow direction conventions, `TrackConsumeEvent` usage guide, and cross-host duplicate guard pattern.

## [2.27.18] - 2026-04-28

### Fixed
- **Empty report overwrite during xUnit v3 test discovery**: `dotnet test` launches the test host twice — once for discovery, once for execution. Both invoke `ITestPipelineStartup.StartAsync`/`StopAsync`, and the discovery pass collected zero features, causing `CreateStandardReportsWithDiagrams` to overwrite the existing `TestRunReport.html` with a structurally complete but empty report (filters and buttons visible, but no scenarios). The report now skips generation entirely when there are zero scenarios across all features, preserving the previous report until the execution pass writes the real one.

## [2.27.17] - 2026-04-28

### Added
- **Deterministic scenario stable IDs** (`ScenarioStableId.Compute()`): Each scenario in the JSON, XML, and YAML report output now includes a `stableId` field — a deterministic 16-character hex identifier derived from the feature name, scenario display name, and outline ID (for parameterized scenarios). Unlike the runtime `id` (which varies by test framework and can be random per run), the `stableId` is consistent across runs, making it suitable for cross-run matching (e.g., flaky test detection, trend tracking).
- **Updated report schemas**: JSON Schema and XSD both include the new `stableId` field as a required property on scenarios.

## [2.27.16] - 2026-04-28

### Added
- **New package: `TestTrackingDiagrams.Extensions.Kafka.BuildInterception`** — Automatically intercepts `ConsumerBuilder<TKey,TValue>.Build()` and `ProducerBuilder<TKey,TValue>.Build()` via [Harmony](https://github.com/pardeike/Harmony) runtime patching, wrapping the result with `TrackingKafkaConsumer` / `TrackingKafkaProducer` when tracking is enabled. This enables **zero production code changes** — no `.BuildTracked()`, no package reference in the API project, no DI changes. The Harmony dependency is isolated in the addon package.
- **`KafkaBuildInterceptor.EnableConsumerTracking<TKey,TValue>()`** — Enables consumer tracking and patches `ConsumerBuilder<TKey,TValue>.Build()` in a single call.
- **`KafkaBuildInterceptor.EnableProducerTracking<TKey,TValue>()`** — Enables producer tracking and patches `ProducerBuilder<TKey,TValue>.Build()` in a single call.
- **`KafkaBuildInterceptor.EnableTracking<TKey,TValue>()`** — Convenience method that enables both consumer and producer tracking and patches both `Build()` methods.
- **`KafkaBuildInterceptor.DisableConsumerTracking<TKey,TValue>()`** / **`DisableProducerTracking<TKey,TValue>()`** — Disables tracking for a specific type pair (Harmony patch remains but becomes a no-op).
- **`KafkaBuildInterceptor.Reset()`** — Clears all tracking state and removes all Harmony patches.

## [2.27.15] - 2026-04-28

### Fixed
- **Flaky MessageTracker test**: `TrackSendEvent_does_nothing_when_no_test_info` no longer races with parallel tests — replaced global `RequestAndResponseLogs.Length` assertion with ID-snapshot comparison.
- **Selenium StaleElementReferenceException**: `Short_note_no_up_arrow_when_expanded`, `Scenario_truncation_change_respected_by_note_buttons`, and `Reducing_truncation_makes_short_note_become_long` now use a retry-based `HoverNoteRect` helper that re-queries elements after SVG re-renders.
- **Selenium assertion failure**: `Long_note_dblclick_from_collapsed_goes_to_truncated_not_expanded` now waits for both plus buttons to appear before asserting count.

## [2.27.14] - 2026-04-28

### Added
- **Redis DI extension** (`AddRedisTestTracking()`): Decorates all `IDatabase` registrations with `RedisTrackingDatabase` via `DecorateAll<IDatabase>`, enabling zero-prod-change Redis tracking through `ConfigureTestServices`.
- **Dapper DI extension** (`AddDapperTestTracking()`): Decorates all `DbConnection` registrations with `TrackingDbConnection` via `DecorateAll<DbConnection>`, enabling zero-prod-change SQL tracking through `ConfigureTestServices`.
- **EventHubs DI extensions** (`AddEventHubsProducerTestTracking()`, `AddEventHubsConsumerTestTracking()`, `AddEventHubsTestTracking()`): Decorates `EventHubProducerClient` and `EventHubConsumerClient` registrations for zero-prod-change tracking.
- **ServiceBus `Action<>` overload** (`AddServiceBusTestTracking(Action<ServiceBusTrackingOptions>?)`): New configuration overload consistent with other extensions. The existing `ServiceBusTrackingOptions` parameter overload is preserved.

### Changed
- **ServiceBus tracking types now inherit from SDK classes**: `TrackingServiceBusClient : ServiceBusClient`, `TrackingServiceBusSender : ServiceBusSender`, `TrackingServiceBusReceiver : ServiceBusReceiver`. This enables `DecorateAll<ServiceBusClient>` to work transparently — production code typed as `ServiceBusClient` receives the tracking subclass without any code changes.
- **EventHubs tracking types now inherit from SDK classes**: `TrackingEventHubProducerClient : EventHubProducerClient`, `TrackingEventHubConsumerClient : EventHubConsumerClient`. Non-virtual properties (`EventHubName`, `FullyQualifiedNamespace`, `IsClosed`) are shadowed with `new`.
- **PubSub tracking types now inherit from SDK classes**: `TrackingPublisherClient : PublisherClient`, `TrackingSubscriberClient : SubscriberClient`. This enables `DecorateAll<PublisherClient>` / `DecorateAll<SubscriberClient>` in the updated `AddPubSubTestTracking()`.
- **ServiceBus DI extension refactored**: Uses `DecorateAll<ServiceBusClient>` instead of manual descriptor replacement. Preserves original service lifetime. The `ServiceBusClient` registration is now decorated in-place rather than replaced with a separate `TrackingServiceBusClient` registration.
- **PubSub DI extension enhanced**: `AddPubSubTestTracking()` now decorates `PublisherClient` and `SubscriberClient` registrations in addition to registering the `PubSubTracker` singleton.
- **PubSub options**: Added `IHttpContextAccessor` property to `PubSubTrackingOptions` for consistency with other extension options.

## [2.27.13] - 2026-04-28

### Changed
- **Selenium tests: Shared ChromeDriver via IClassFixture** — All 19 Selenium test classes now share a Chrome browser instance at the class level using `IClassFixture<ChromeFixture>` / `IClassFixture<ChromeFixture1280X900>`, reducing Chrome process launches from ~207 (one per test) to ~19 (one per class). This lowers memory pressure and eliminates redundant browser startup/shutdown overhead.

## [2.27.12] - 2026-04-28

### Fixed
- **CI: Release workflow "No space left on device"**: Freed additional disk space (Swift, GraalVM, PowerShell, hostedtoolcache, Docker images) and changed build/pack to target only `src/` projects instead of the full 77-project solution. Test projects are no longer built during release — only the single core test project is built implicitly by `dotnet test`.

## [2.27.11] - 2026-04-28

### Fixed
- **Eliminated all CI build warnings**: Fixed ~80 compiler and analyzer warnings across the solution, including nullability annotations (`CS8600`–`CS8625`), obsolete API usage (`CS0618`), xUnit analyzer rules (`xUnit2013`, `xUnit2017`, `xUnit2018`, `xUnit1051`), and unused field warnings (`CS0414`). No functional changes.

## [2.27.10] - 2026-04-28

### Fixed
- **`CurrentTestInfo.Fetcher` no longer throws `NullReferenceException` outside test context**: All 8 framework `CurrentTestInfo.Fetcher` implementations (xUnit3, xUnit2, NUnit4, TUnit, MSTest, LightBDD, ReqNRoll, BDDfy) now return `("Unknown", "unknown")` when the test context is unavailable (e.g. during hosted service processing, background threads, Service Bus message handlers). Previously, the delegates accessed the test context without null checks, causing `NullReferenceException` when invoked outside of test execution.
- **`MessageTracker.GetTestInfo()` now catches delegate exceptions**: `MessageTracker` was the only tracking component that invoked `CurrentTestInfoFetcher` without exception handling. All other extensions route through `TestInfoResolver.Resolve()`, which wraps the call in a try-catch. `MessageTracker.GetTestInfo()` now matches this behaviour — a throwing delegate returns `null` (tracking silently skipped) instead of propagating the exception to callers.
- **NUnit4: `TestContextEnumerableExtensions` build error on `IEnumerable<ParameterInfo>.Length`**: Fixed pre-existing compilation error caused by NUnit 4.5.1's `IMethodInfo.GetParameters()` returning `IEnumerable<ParameterInfo>` (which lacks a `Length` property). The result is now materialised to an array before pattern matching.

## [2.27.9] - 2026-04-28

### Added
- **Kafka: Static interceptor for internally-built consumers/producers** (`KafkaTrackingInterceptor`): Enables tracking for consumers and producers built via `new ConsumerBuilder<TKey,TValue>(...).Build()` inside `BackgroundService` or other non-DI code paths. Use `EnableConsumerTracking<TKey,TValue>()` / `EnableProducerTracking<TKey,TValue>()` in test setup, then replace `.Build()` with `.BuildTracked()` in production code (one-token change, no-op when not in test context). Also provides `.Tracked()` extension on existing `IConsumer` / `IProducer` instances.
- **Kafka: Consumer and producer factory interfaces**: `IKafkaConsumerFactory<TKey,TValue>` and `IKafkaProducerFactory<TKey,TValue>` with default implementations (`KafkaConsumerFactory`, `KafkaProducerFactory`) and tracking decorators (`TrackingKafkaConsumerFactory`, `TrackingKafkaProducerFactory`). Inject the factory in services that build consumers/producers internally for clean DI-based tracking.
- **`AddKafkaConsumerFactoryTestTracking<TKey,TValue>()`**: DI extension that registers a default consumer factory (if none exists) and decorates it with tracking.
- **`AddKafkaProducerFactoryTestTracking<TKey,TValue>()`**: DI extension that registers a default producer factory (if none exists) and decorates it with tracking.
- **`BuildTracked()` extension** on `ConsumerBuilder<TKey,TValue>` and `ProducerBuilder<TKey,TValue>` — builds and wraps with tracking if the static interceptor is active.
- **`Tracked()` extension** on `IConsumer<TKey,TValue>` and `IProducer<TKey,TValue>` — wraps an existing instance with tracking if the static interceptor is active. Prevents double-wrapping.

## [2.27.8] - 2026-04-28

### Added
- **Kafka: Transactional producer tracking**: All five transactional producer methods (`InitTransactions`, `BeginTransaction`, `CommitTransaction`, `AbortTransaction`, `SendOffsetsToTransaction`) are now tracked when `TrackTransactions = true`. Each has its own `KafkaOperation` enum value and classifier labels (full names in Detailed/Raw, shortened `Init Txn`/`Begin Txn`/`Commit Txn`/`Abort Txn`/`Send Offsets` in Summarised).
- **`TrackTransactions`** option on `KafkaTrackingOptions` (default `false`) — single flag to enable/disable tracking of all transactional producer operations.
- **`LogTransaction()`** method on `KafkaTracker`.
- **5 new `KafkaOperation` enum values**: `InitTransactions`, `BeginTransaction`, `CommitTransaction`, `AbortTransaction`, `SendOffsetsToTransaction`.

## [2.27.7] - 2026-04-28

### Fixed
- **Kafka: Consumer `Commit()` now tracked** (all 3 overloads): Previously, `KafkaOperation.Commit` was defined in the enum and the tracker had a `LogCommit()` method, but `TrackingKafkaConsumer` never called it. All three `Commit()` overloads now log when `TrackCommit = true`.
- **Kafka: Consumer `Unsubscribe()` now tracked**: Previously just delegated without logging. Now logs when `TrackUnsubscribe = true`.
- **Kafka: Producer `Flush()` now tracked** (both overloads): Previously just delegated without logging. Now logs when `TrackFlush = true`.
- **Kafka: Consumer now logs message Key**: Previously, `TrackingKafkaConsumer` only logged the message Value. Now uses the same `BuildContent` pattern as the producer, logging both Key and Value (controlled by `LogMessageKey` / `LogMessageValue`). Content is also correctly suppressed in Summarised mode.
- **CI: Split IKVM tests into own matrix job with disk cleanup**: The PlantUml.Ikvm test project copies hundreds of native runtime files per platform, exhausting disk space on GitHub Actions runners. IKVM tests now run in a dedicated job with pre-build cleanup of unused SDK/tooling directories.

### Added
- **`TrackFlush`** option on `KafkaTrackingOptions` (default `false`) — enables tracking of `IProducer.Flush()` calls.
- **`TrackUnsubscribe`** option on `KafkaTrackingOptions` (default `false`) — enables tracking of `IConsumer.Unsubscribe()` calls.
- **`LogFlush()`** and **`LogUnsubscribe()`** methods on `KafkaTracker`.

## [2.27.6] - 2026-04-28

### Fixed
- **Spanner: Raw and Detailed verbosity now produce different content**: Previously, both Raw and Detailed verbosity levels showed identical SQL text in the content body (and in phase variants). Raw now always includes parameter values when parameters exist, regardless of the `LogParameters` setting. The `LogParameters` option is now marked `[Obsolete]`.
- **Spanner: Phase variant content now correctly differentiates by verbosity level**: When using `SetupVerbosity`/`ActionVerbosity` overrides, the variant content now uses the appropriate content for each verbosity level (Raw includes parameters, Detailed shows plain SQL, Summarised omits content).

## [2.27.5] - 2026-04-27

### Fixed
- **Phase-aware verbosity overrides (`SetupVerbosity`/`ActionVerbosity`) now work for non-BDD test frameworks** (fixes #23): When the test phase is `Unknown` at capture time (i.e. no BDD framework sets the phase automatically) and verbosity overrides are configured, all extension trackers now pre-compute both Setup and Action rendering variants (`PhaseVariant`). The PlantUML renderer selects the correct variant based on `IsActionStart` marker position, so `SetupVerbosity = Summarised` / `ActionVerbosity = Detailed` (or any combination) works without requiring `StartSetup()`.

### Added
- **`PhaseVariant` record type** on `RequestResponseLog` — holds pre-computed `Method`, `Uri`, `Content`, `Headers`, and `Skip` for a specific verbosity level, allowing the renderer to pick the right variant per phase.
- **`PhaseVariantExtensions.AttachVariants<T>()` / `WithVariants<T>()`** — shared generic helper that all extension trackers use to attach variants when phase is `Unknown` and overrides are configured. Avoids duplicating variant logic across 24 extensions.
- **`StartSetup()` on all 8 framework `TrackingDiagramOverride` wrappers** (xUnit3, xUnit2, NUnit4, MSTest, TUnit, LightBDD, ReqNRoll, BDDfy) — delegates to `DefaultTrackingDiagramOverride.StartSetup()`. Users who prefer explicit phase boundaries can now call `StartSetup()` before setup code, though it is not required for verbosity overrides to work.

## [2.27.4] - 2026-04-27

### Fixed
- **Spanner and Bigtable services render as `participant` instead of `database` shape in diagrams**: Changed `DependencyCategory` from generic `"Database"` to `"Spanner"` and `"Bigtable"` respectively, and added both (plus generic `"Database"` fallback) to `DependencyPalette.CategoryToType`. These services now correctly render with the `database` shape and red color in sequence diagrams.

## [2.27.3] - 2026-04-27

### Fixed
- **Spanner gRPC interceptor: test identity not resolved in WebApplicationFactory scenarios**: Added `IHttpContextAccessor` overload to `SpannerConnectionExtensions.WithTestTracking()` so the interceptor can read test identity from HTTP request headers (propagated by `TestTrackingMessageHandler`) instead of relying solely on `AsyncLocal`, which does not propagate through the TestServer's request pipeline.

## [2.27.2] - 2026-04-27

### Fixed
- **Flaky `PendingRequestResponseLogsTests` under parallel execution**: Added missing `[CollectionDefinition("PendingLogs")]` so the three test classes sharing the `"PendingLogs"` collection are properly serialized by xUnit. Without this, xUnit silently ignored the `[Collection]` attribute.
- **`FlushAll_with_no_pending_entries_is_noop`**: Replaced assertion on total `RequestAndResponseLogs.Length` with testId-filtered assertion, eliminating race condition with concurrent test projects.
- **`AtlasDataApiTrackingMessageHandlerTests`**: Replaced `RequestResponseLogger.Clear()` with per-test `_testId` filtering. Tests no longer wipe the shared static log queue or assert on unfiltered total counts.
- **`TrackingDbCommandTests`**: Same fix — replaced `Clear()` and unfiltered `[0]`/`[1]` indexing with `GetLogsForTest()` filtered by unique `_testId`.
- **`MongoDbTrackingSubscriberTests`**: Replaced `Assert.Empty(RequestResponseLogger.RequestAndResponseLogs)` with testId-filtered assertion in `NoLogging_WhenCurrentTestInfoFetcherIsNull`.
- **Removed `RequestResponseLogger.Clear()` from all test constructors/teardown**: `ServiceBusTrackerTests`, `TrackingDbConnectionTests`, `TrackingDbTransactionTests`, `DbConnectionExtensionsTests`, `MongoDbTrackingSubscriberTests`. The `Clear()` call is destructive to concurrent tests and unnecessary when assertions filter by testId.

## [2.27.1] - 2026-04-27

### Fixed
- **`TestTrackingMessageHandler`** — exception from `CurrentTestInfoFetcher` (e.g. during app startup when no test is active) no longer crashes the HTTP call. The request is forwarded without tracking instead.
- **`DeferredLogFlushHandler`** — same fix: a throwing fetcher no longer prevents the HTTP response from being returned. Pending log entries remain queued for the next successful invocation.
- Partial context headers (test name present but no test ID) now gracefully skip tracking instead of throwing.

## [2.27.0] - 2025-07-25

### Added
- **`SpannerTrackingInterceptor`** — new gRPC interceptor that captures **all** Spanner operations at the transport layer, including Spanner-specific methods (`CreateInsertCommand`, `CreateSelectCommand`, `CreateInsertOrUpdateCommand`, etc.) that bypass ADO.NET wrapping. Extracts SQL text, table names, and mutation details from protobuf messages.
- **`SpannerConnectionStringBuilder.WithTestTracking()` extension** — configures gRPC interception via `SessionPoolManager.CreateWithSettings()` with `SpannerSettings.Interceptor`. Zero production code changes required.
- **`SpannerTracker.CreateServerObservers()`** — returns delegate tuple `(Action<string, IMessage, DateTimeOffset>, Action<string, IMessage, IMessage?, TimeSpan, StatusCode?, DateTimeOffset>)` for wiring to `Spanner.InMemoryEmulator`'s `FakeSpannerServer.OnRequestReceived` / `OnResponseSent` callbacks. Enables server-side observation as an alternative to client-side gRPC interception.

### Changed
- **Spanner extension now depends on `Google.Cloud.Spanner.V1` (5.\*) and `Grpc.Core.Api` (2.\*)** for protobuf type extraction in `SpannerTrackingInterceptor` and `CreateServerObservers()`.

### Documentation
- **Spanner wiki page**: Added Option D (gRPC Interception — recommended) and Option E (Server-Side Observation) setup guides with architecture diagrams, comparison tables, and "What Gets Captured" reference. Added warnings to Options A/B/C about limitations. Updated See Also with gRPC Extension and Phase-Aware Tracking links.

## [2.26.3] - 2025-07-24

### Added
- **`HttpContextAccessor` property on all extension options classes**: Every tracking extension options class now exposes `IHttpContextAccessor? HttpContextAccessor`. When set, the tracker reads it automatically via `?? options.HttpContextAccessor`, providing an alternative to the constructor parameter for dual-resolution test identity. This applies to: `TestTrackingMessageHandlerOptions`, `ServiceBusTrackingOptions`, `MassTransitTrackingOptions`, `ElasticsearchTrackingOptions`, `RedisTrackingDatabaseOptions`, `DapperTrackingOptions`, `EventHubsTrackingOptions`, `BlobTrackingMessageHandlerOptions`, `CloudStorageTrackingMessageHandlerOptions`, `CosmosTrackingMessageHandlerOptions`, `DynamoDbTrackingMessageHandlerOptions`, `EventBridgeTrackingMessageHandlerOptions`, `S3TrackingMessageHandlerOptions`, `SnsTrackingMessageHandlerOptions`, `SqsTrackingMessageHandlerOptions`, `StorageQueueTrackingMessageHandlerOptions`.
- **Auto-resolution of `IHttpContextAccessor` from DI**: DI extensions and convenience methods now auto-resolve `IHttpContextAccessor` from the service provider when available, eliminating the need for manual `httpContextAccessor: sp.GetRequiredService<IHttpContextAccessor>()` wiring:
  - `CreateTestTrackingClient()` (both overloads) — resolves from `factory.Services`
  - `AddServiceBusTestTracking()` — resolves from `IServiceProvider` in the factory lambda
  - `ReplaceWithTrackingProxy` simplified overload — accepts optional `IHttpContextAccessor?` parameter
  - All `DelegatingHandler`-based extensions (BlobStorage, CloudStorage, CosmosDB, DynamoDB, EventBridge, S3, SNS, SQS, StorageQueues) — convenience methods pass `options.HttpContextAccessor` to handler constructors
  - MassTransit, Elasticsearch, Redis, Dapper, EventHubs — convenience methods pass `options.HttpContextAccessor` to tracker constructors

### Documentation
- **All 16 extension wiki pages**: Added `HttpContextAccessor` row to options tables and updated dual-resolution notes with v2.26.3 auto-resolution info.
- **HTTP Tracking Setup wiki**: Rewrote "How to Use It" section with simplified examples showing auto-resolution. Replaced "MediatR Auto-Resolution" with broader "Auto-Resolution (v2.26.2+)" section covering all extensions. Updated extensions table with auto-resolution version info.
- **Diagnostics and Debugging wiki**: Added new "Other dependencies not appearing in per-test reports" section generalizing the gRPC troubleshooting pattern to all extensions.

## [2.26.2] - 2026-04-27

### Added
- **`CurrentTestInfo` static class in every framework package**: Each framework adapter package now provides a `static class CurrentTestInfo` with a get-only `Fetcher` property (`Func<(string Name, string Id)>`). This provides a uniform, discoverable API for setting `CurrentTestInfoFetcher` on any tracking options class — the syntax is identical regardless of framework:
  ```csharp
  CurrentTestInfoFetcher = CurrentTestInfo.Fetcher
  ```
  Available in: `TestTrackingDiagrams.xUnit3`, `TestTrackingDiagrams.xUnit2`, `TestTrackingDiagrams.NUnit4`, `TestTrackingDiagrams.MSTest`, `TestTrackingDiagrams.TUnit`, `TestTrackingDiagrams.LightBDD` (Core/xUnit2/xUnit3/TUnit), `TestTrackingDiagrams.ReqNRoll` (Core/xUnit2/xUnit3/TUnit), `TestTrackingDiagrams.BDDfy.xUnit3`.
- **`XUnit2TestTrackingMessageHandlerOptions.TestInfoFetcher`**: xUnit v2 options class now exposes a static `TestInfoFetcher` field (previously the delegate was only set inline in the constructor), aligning it with all other framework adapters.

### Documentation
- **All wiki pages**: Replaced verbose framework-specific `CurrentTestInfoFetcher` lambda examples with the new `CurrentTestInfo.Fetcher` syntax. Simplified "CurrentTestInfoFetcher by Framework" sections to a single code snippet plus a using-directive table.

## [2.26.1] - 2026-07-14

### Added
- **gRPC Extension — `AddTrackedGrpcClient<TClient>()` DI extension**: New `IServiceCollection` extension method that registers a singleton tracked gRPC client with `IHttpContextAccessor` auto-resolved from DI, matching the existing pattern used by BigQuery, Bigtable, MongoDB, Kafka, and other extensions. Eliminates the need for manual `HttpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>()` wiring.
- **gRPC Extension — auto-resolve `IHttpContextAccessor` in `CreateTestTrackingGrpcClient`**: Both `CreateTestTrackingGrpcClient` and `CreateTestTrackingGrpcClientWithChannel` now auto-resolve `IHttpContextAccessor` from `factory.Services` when not explicitly set on `GrpcTrackingOptions`. This ensures dual-resolution test identity works out of the box for the test → SUT direction.

## [2.26.0] - 2026-07-14

### Added
- **New `TestTrackingDiagrams.Extensions.AtlasDataApi` package**: MongoDB Atlas Data API extension with a `DelegatingHandler` (`AtlasDataApiTrackingMessageHandler`) that intercepts and classifies REST API operations. Supports 10 classified operations (FindOne, Find, InsertOne, InsertMany, UpdateOne, UpdateMany, DeleteOne, DeleteMany, ReplaceOne, Aggregate) with directional arrows (← read, → write, ↔ read-modify-write), three verbosity levels (Raw, Detailed, Summarised), JSON body metadata extraction (dataSource, database, collection, filter), ExcludedOperations filtering, and DI registration via `AddAtlasDataApiTestTracking()`.
- **MongoDB Extension — 11 new classified operations**: Added Change Streams (`Watch` via `$changeStream` pipeline detection), Transactions (`CommitTransaction`, `AbortTransaction`), Admin (`DropDatabase`, `ServerStatus`, `DbStats`, `CollStats`), DDL (`RenameCollection`, `ListIndexes`), and Legacy (`MapReduce`). Total classified operations: 28 (up from 17).
- **MongoDB Extension — directional arrows**: Detailed verbosity labels now include directional arrows: `←` for reads (Find, Aggregate, Count, Distinct, ListCollections, ListDatabases, ListIndexes), `→` for writes (Insert, Delete, DropIndex, DropCollection, DropDatabase, RenameCollection), `↔` for read-modify-write (FindAndModify, Update, BulkWrite). Schema and admin operations show no arrow.
- **MongoDB Extension — enriched metadata**: `MongoDbOperationInfo` now includes `DocumentCount` (from insert arrays), `DocumentId` (from filter `_id`), `PipelineStages` (from aggregate pipelines), and `IsGridFs` (from `fs.files`/`fs.chunks` collections). Detailed labels show enriched info like `Insert (×5) → users` and `Aggregate ($match, $group) ← orders`.
- **MongoDB Extension — `ExcludedOperations`**: New `HashSet<MongoDbOperation>` option to suppress specific operations from tracking (e.g. `ServerStatus`, `ListDatabases`).
- **MongoDB Extension — `LogFilterText`**: New `bool` option (default: `true`) to control whether filter document text is included in Detailed mode request content.
- **MongoDB Extension — `AddMongoDbTestTracking()` DI extension**: New service collection extension method that registers `MongoDbTrackingSubscriber` as a singleton with `IHttpContextAccessor` auto-resolved from DI.
- **MongoDB Extension — `endSessions` ignored by default**: Added `endSessions` to the default `IgnoredCommands` set to suppress session cleanup noise.
- **MongoDB Extension — response metadata extraction**: In Detailed mode, successful command replies now extract `n`, `nModified`, and `nUpserted` counts from the BSON reply and append them to response content.

## [2.25.2] - 2026-04-27

### Fixed
- **gRPC dependency tracking not resolving test identity from HTTP context**: `GrpcTrackingInterceptor` accepts an `IHttpContextAccessor` for dual-resolution test identity (HTTP headers first, delegate fallback), but none of the public API entry points (`GrpcTrackingChannel.Create`, `AsGrpcTrackingCallInvoker`, `CreateTestTrackingGrpcClient`, `WithTestTracking`) forwarded it. When a gRPC client ran inside the SUT's request pipeline (e.g. API calling a downstream gRPC service during a test HTTP request), test identity could not be resolved from the propagated HTTP context headers — causing gRPC dependency calls to be logged with "Unknown" test identity and not appearing in per-test reports. Added `IHttpContextAccessor? HttpContextAccessor` property to `GrpcTrackingOptions`; all entry points and the interceptor constructor now read from this property. Consumers set it once on the options object (typically via `sp.GetRequiredService<IHttpContextAccessor>()`) and the interceptor automatically resolves test identity from HTTP context headers, falling back to `CurrentTestInfoFetcher` when no HTTP context is available.

## [2.25.1] - 2026-04-27

### Fixed
- **Selenium tests**: Fixed `WaitForDiagramSvg` timeout in CI by explicitly calling `_renderDiagramsInContainer` before polling for SVG — `IntersectionObserver` doesn't fire reliably in headless Chrome. Applied to all 5 affected test files (`DiagramNoteTests`, `ContextMenuExtendedTests`, `ScenarioInteractionTests`, `DiagramZoomTests`, `DependencyColoringTests`).
- **`Collapsed_note_shows_plus_button_in_top_right`**: Fixed false assertion failure by scoping button selectors to the target scenario instead of the entire page — other scenarios' minus buttons (in truncated state) were being matched.

## [2.25.0] - 2026-04-27

### Added
- **`RequestResponseLogger.MaxContentLength`**: New global static property that truncates content at capture time when set. Content exceeding the limit is trimmed to the specified character count with a `…truncated (N chars total)` marker appended. Applies to all extensions (HTTP, BigQuery, Spanner, Bigtable, Cosmos, Redis, etc.) since truncation happens in the core `Log()` method. Default is `null` (no limit). Set during test setup, e.g. `RequestResponseLogger.MaxContentLength = 2000;`

## [2.24.1] - 2026-04-27

### Added
- **`BigtableTrackingOptions.ExcludedOperations`**: Added `HashSet<BigtableOperation>` property to suppress tracking of specific Bigtable operations, matching the pattern used by Spanner, Dapper, Elasticsearch, and EventBridge extensions.

## [2.24.0] - 2026-07-14

### Added
- **New `TestTrackingDiagrams.Extensions.Spanner` package**: Google Cloud Spanner extension with ADO.NET connection wrapping (`TrackingSpannerConnection`, `TrackingSpannerCommand`, `TrackingSpannerTransaction`) and a direct `SpannerTracker` for gRPC-style usage. Classifies 18 Spanner operations (Query, Read, Insert, Update, Delete, InsertOrUpdate, Replace, Commit, Rollback, BeginTransaction, BatchDml, PartitionQuery, PartitionRead, Ddl, CreateSession, DeleteSession, StreamingRead, Other) with three verbosity levels (Raw, Detailed, Summarised). Includes DI registration via `AddSpannerTestTracking()` and connection extension `WithTestTracking()`.
- **New `TestTrackingDiagrams.Extensions.Bigtable` package**: Google Cloud Bigtable extension with a direct `BigtableTracker` implementing `ITrackingComponent`. Classifies 7 Bigtable operations (ReadRows, MutateRow, MutateRows, CheckAndMutateRow, ReadModifyWriteRow, SampleRowKeys, Other) with directional diagram labels (← for reads, → for writes) and short table name extraction from full Bigtable resource paths. Includes DI registration via `AddBigtableTestTracking()`.
- **`BigQueryTracker`**: New direct tracker class for the BigQuery extension, providing `LogRequest`/`LogResponse` pair logging without HTTP interception. Useful for scenarios where BigQuery operations are tracked at a layer above or below the HTTP pipeline.
- **`BigQueryServiceCollectionExtensions.AddBigQueryTestTracking()`**: New DI extension in the BigQuery package that registers a singleton `BigQueryTracker` with `IHttpContextAccessor` auto-resolved from DI.

## [2.23.13] - 2026-04-27

### Fixed
- **SqlTrackingInterceptor**: Fixed `UriFormatException` when `Verbosity` is `Detailed` or `Raw` and the SQL Server connection uses comma-separated port notation (e.g. `127.0.0.1,33262` from Docker containers). The `DataSource` comma is now normalised to a colon for valid URI construction. ([#22](https://github.com/lemonlion/TestTrackingDiagrams/issues/22))

## [2.23.12] - 2026-04-27

### Added
- **`TestInfoResolver.CreateHttpFallbackFetcher()`**: New convenience method that creates a `Func<(string Name, string Id)>` encapsulating the dual-resolution pattern (httpContext headers first, fallback delegate second). Eliminates the ~10-line boilerplate previously needed when setting `CurrentTestInfoFetcher` on tracking options.
- **`ServiceCollectionDecoratorExtensions.DecorateAll<TService>()`**: New DI helper that wraps all existing registrations of a service type with a decorator. Removes the original registration and adds the decorated version, preserving the original service lifetime. Handles all descriptor types (factory, instance, type).
- **`ServiceCollectionDecoratorExtensions.DecorateAllOpen()`**: New DI helper that scans for all closed-generic registrations matching an open generic type and replaces each with a decorator type. Additional constructor parameters are resolved from DI via `ActivatorUtilities`.
- **`KafkaServiceCollectionExtensions.AddKafkaProducerTestTracking<TKey,TValue>()`**: New DI extension in the Kafka package that decorates all `IProducer<TKey,TValue>` registrations with `TrackingKafkaProducer` for test diagram tracking. Automatically resolves `IHttpContextAccessor` from DI.
- **`KafkaServiceCollectionExtensions.AddKafkaConsumerTestTracking<TKey,TValue>()`**: Same pattern for `IConsumer<TKey,TValue>` with `TrackingKafkaConsumer`.
- **`PubSubServiceCollectionExtensions.AddPubSubTestTracking()`**: New DI extension in the PubSub package that registers a singleton `PubSubTracker` with `IHttpContextAccessor` auto-resolved from DI.

## [2.23.11] - 2026-04-27

### Added
- **GraphQL query formatting in sequence diagram notes**: GraphQL request bodies are now automatically detected and formatted with proper indentation in diagram notes, replacing the previous single-line JSON string representation. The GraphQL query is parsed and pretty-printed with brace-depth indentation, while arguments inside parentheses (e.g. HotChocolate filtering/sorting) stay inline. Four configurable display modes via `GraphQlBodyFormat` on `ReportConfigurationOptions`:
  - `Json` — Previous behaviour: JSON pretty-print with query as a single-line string value.
  - `FormattedQueryOnly` — Formatted GraphQL query only; HTTP headers and metadata are suppressed.
  - `Formatted` — Formatted GraphQL query with HTTP headers shown above.
  - `FormattedWithMetadata` (default) — Formatted GraphQL query with HTTP headers, plus `variables` and `extensions` sections rendered below.
- When `FocusFields` are in use, GraphQL formatting automatically falls back to `Json` mode so JSON field highlighting works correctly.

## [2.23.10] - 2026-04-27

### Fixed
- **gRPC Activity spans not appearing in Activity Diagrams / Flamecharts**: Fixed three issues preventing gRPC calls from producing activity spans:
  1. `InternalFlowSpanCollector.FilterByAutoInstrumentation()` excluded `"TestTrackingDiagrams.Grpc"` spans because the source was not in `WellKnownAutoInstrumentationSources`. Added `"TestTrackingDiagrams.Grpc"` and `"Grpc.Net.Client"` to the well-known list.
  2. `AsyncUnaryCall` disposed its `Activity` immediately on method return (before the async response arrived), producing near-zero-duration spans. The Activity is now kept alive and stopped in `WrapUnaryResponse` after the response is logged, so spans cover the full call duration.
  3. No trace context was propagated to the server. A `traceparent` metadata header is now injected into all outgoing gRPC calls, allowing server-side ASP.NET Core spans to share the same TraceId.

## [2.23.9] - 2026-04-27

### Fixed
- **Hover buttons not appearing on notes with Creole separator markup**: Fixed a bug where `findNoteGroups()` failed to detect note groups in SVG when PlantUML's Creole `..text..` separator syntax was used inside notes (e.g. `..Continued From Previous Diagram..`). The Creole syntax causes PlantUML to insert `<line>` elements between the note's background `<path>` and `<text>` elements. The algorithm now uses bounding-box containment to skip `<line>`, `<rect>`, and `<circle>` elements that are visually inside the note, while correctly stopping at lifeline/arrow elements between different note groups.
- **Pre-existing Selenium test fix**: Fixed `Partition_long_note_expand_click_works` test that was failing due to SVG note fold path intercepting the button click — now uses JS-dispatched click.

### Added
- **Comprehensive Selenium regression tests for split-diagram hover buttons**: Added 6 new Selenium tests for 3-diagram split scenarios with Creole continuation notes, plus 6 tests for 2-diagram split initial render. Tests cover hover rects, toggle icons, hover visibility, double-click state cycling, and state change preservation across all diagram parts within a single scenario.

## [2.23.8] - 2026-04-27

### Added
- **Activity Diagram & Flamechart support for gRPC calls**: `GrpcTrackingInterceptor` now creates `System.Diagnostics.Activity` spans around each gRPC call, populates `ActivityTraceId`, `ActivitySpanId`, and `Timestamp` on all `RequestResponseLog` entries, and lazily starts the `InternalFlowActivityListener`. This enables gRPC calls to appear in Activity Diagrams and Flamecharts alongside HTTP calls — previously they were invisible because the interceptor never created activities or set trace context on log entries.

## [2.23.7] - 2026-04-27

### Added
- **`CreateTestTrackingGrpcClient` convenience extension on `WebApplicationFactory`**: New `factory.CreateTestTrackingGrpcClient<TEntryPoint, TClient>(options)` extension method mirrors the HTTP `CreateTestTrackingClient` API for gRPC. A single call handles the `GrpcResponseVersionHandler` (HTTP/2 fix for TestServer), `GrpcChannel` creation, `GrpcTrackingInterceptor` installation, and typed gRPC client construction — making it impossible to accidentally forget tracking. Also provides a `CreateTestTrackingGrpcClientWithChannel` variant that returns the underlying `GrpcChannel` for disposal.
- **`GrpcResponseVersionHandler`**: Built-in `DelegatingHandler` that fixes the HTTP response version mismatch when testing gRPC services in-process via `TestServer`. `TestServer` returns HTTP/1.1, but gRPC requires HTTP/2 — this handler copies the request version onto the response. Previously, every test project had to implement their own `ResponseVersionHandler`; now it's included in the `TestTrackingDiagrams.Extensions.Grpc` package.

## [2.23.6] - 2026-04-26

### Added
- **`GrpcTrackingChannel` factory for incoming gRPC tracking**: New `GrpcTrackingChannel.Create()` and `CreateWithChannel()` static methods that create a tracked `CallInvoker` from an `HttpMessageHandler` and base address. This enables rich gRPC-aware diagrams for test-to-SUT gRPC calls — with protobuf JSON deserialization, operation classification (`UnaryCall`, `ServerStreamingCall`, etc.), `grpc://` URIs, and gRPC status code mapping — instead of raw HTTP/2 `POST` requests with binary bodies. Also provides `HttpMessageHandler.AsGrpcTrackingCallInvoker()` extension method for terser syntax.

## [2.23.5] - 2026-04-26

### Added
- **Automatic GraphQL operation detection in diagram arrows**: HTTP POST requests containing GraphQL request bodies are now automatically detected and the diagram arrow label is enriched with the operation type and name (e.g. `POST: /graphql\n(query GetUser)`, `POST: /api/data\n(mutation CreateOrder)`). Detection is purely body-based (no URL assumption) using a regex that identifies the GraphQL `"query"` JSON key and parses the operation type (`query`/`mutation`/`subscription`) and optional operation name. Anonymous shorthand queries (`{ user { name } }`) are labelled as `(query)`. The `operationName` JSON field is respected when present. No configuration or extra packages required — works automatically for all HTTP-tracked GraphQL traffic.

## [2.23.4] - 2026-04-26

### Added
- **Static `TestInfoFetcher` property on all framework adapter options classes**: `XUnitTestTrackingMessageHandlerOptions`, `NUnitTestTrackingMessageHandlerOptions`, `TUnitTestTrackingMessageHandlerOptions`, `MSTestTestTrackingMessageHandlerOptions`, `BDDfyTestTrackingMessageHandlerOptions`, `LightBddTestTrackingMessageHandlerOptions`, and `ReqNRollTestTrackingMessageHandlerOptions` now expose a `public static readonly Func<(string Name, string Id)> TestInfoFetcher` property. Extension options (e.g. `SqlTrackingInterceptorOptions`, `CosmosTrackingMessageHandlerOptions`) can reference this directly instead of writing verbose inline lambdas with null guards.

## [2.23.3] - 2026-04-26

### Fixed
- **Failure cluster links not scrolling to second scenario**: Clicking a failure cluster link after previously clicking another would change the URL hash but not scroll to the target scenario. The onclick handler now explicitly calls `scrollIntoView` and `preventDefault` instead of relying on native anchor navigation, which fails when `<details>` elements are dynamically opened.

## [2.23.2] - 2026-04-26

### Fixed
- **Aligned scenario-steps and example-diagrams borders in parameterized groups**: The steps panel and diagrams panel now have matching left borders in parameterized/multi-parameter scenarios. Previously, the steps panel was indented 1em further right due to its `.param-detail-panels` wrapper having `margin-left: 1em`.

## [2.23.1] - 2026-04-26

### Fixed
- **Search now includes feature names, descriptions, labels, and tags**: The report search bar now indexes feature display names, feature descriptions, feature labels, scenario categories, and scenario labels as plain text in the `data-search` attribute. Previously, searching for a feature name like "Pancakes Creation" would return no results. This applies to both regular scenarios and parameterized groups.

## [2.23.0] - 2026-04-26

### Added
- **Dual-resolution test identity across all extensions**: All 22 tracking extensions now support resolving test identity from HTTP request headers (propagated by `TestTrackingMessageHandler`) in addition to the existing `CurrentTestInfoFetcher` delegate. This fixes a systemic issue where extensions running inside the SUT's request pipeline (e.g. via `WebApplicationFactory`) could not resolve the test context because the test framework's `AsyncLocal` does not flow from the test thread to the server thread.
  - New `TestInfoResolver` static helper in the core package centralises the dual-resolution logic: try HTTP headers first, fall back to delegate.
  - Every tracker/handler constructor now accepts an optional `IHttpContextAccessor? httpContextAccessor` parameter (backward-compatible; defaults to `null`).
  - `MediatorTrackingExtensions` auto-resolves `IHttpContextAccessor` from DI when creating the tracking proxy.
  - `TrackingProxyOptions` gains an `HttpContextAccessor` property for extensions using `TrackingProxy<T>` (MediatR, DispatchProxy).
  - `SqlTrackingInterceptor` refactored to use the shared `TestInfoResolver`.
  - 13 new unit tests for `TestInfoResolver` covering header precedence, delegate fallback, null/exception handling, and the nullable-tuple overload.
- **Affected extensions**: Kafka, CosmosDB, ServiceBus, EventHubs, EventBridge, SQS, SNS, MediatR, MassTransit, Redis, MongoDB, BlobStorage, S3, BigQuery, CloudStorage, StorageQueues, DynamoDB, Elasticsearch, Dapper, gRPC, DispatchProxy (via TrackingProxy), and EfCore.Relational (refactored).

## [2.22.32] - 2026-04-26

### Fixed
- **Dependency filter**: `ExtractDependencies` now matches all PlantUML participant types (`actor`, `boundary`, `control`, `entity`, `database`, `collections`, `queue`, `participant`). Previously only `entity` and `participant` were matched, so dependencies rendered as `database`, `collections`, or `queue` (e.g. Cosmos DB, Redis, ServiceBus) were silently excluded from the dependency filter buttons in the HTML report.

## [2.22.31] - 2026-04-26

### Fixed
- **DiagramNoteTests**: Fixed `StaleElementReferenceException` in `Short_note_no_up_arrow_when_expanded` by re-querying hover rects after `SetScenarioState` re-renders the SVG.

## [2.22.30] - 2026-04-26

### Fixed
- **Wiki Feature 8 (CI Summary)**: Removed `!pragma teoz true` from failure diagram source so the PlantUML server renders a compact, readable diagram inside the CI summary panel.
- **Wiki Feature 11 (DiagramFocus)**: Replaced hand-crafted PlantUML source with real TTD-generated diagram via `PlantUmlCreator.GetPlantUmlImageTagsPerTestId()` using `RequestResponseLog` entries and `FocusFields`. Response note now correctly appears on the right side (matching real TTD output).
- **Wiki Feature 12 (Failure Diagnostics)**: Fixed failure cluster expansion using `setAttribute('open', '')` instead of unreliable `click()` on `<details>` element in headless Chrome. Cluster is now visibly expanded from the start of the GIF.

## [2.22.29] - 2026-04-26

### Fixed
- **Wiki Feature 8 (CI Summary)**: Expand all `<details>` and scroll to show embedded PlantUML diagram image.
- **Wiki Feature 9 (JSON Report)**: Rewrote JSON viewer using inline style toggling; CSS sibling selectors were unreliable in headless Chrome.
- **Wiki Feature 11 (DiagramFocus)**: Changed highlighted fields from `<color:blue><b>` to `<back:#FFEB3B>` (yellow background) for much more visible highlighting.
- **Wiki Feature 12 (Failure Diagnostics)**: Removed initial overview hold so GIF starts directly at the failure cluster section.

## [2.22.28] - 2026-04-26

### Fixed
- **Wiki Feature 8 (CI Summary)**: Replaced fake header overlay with real `CiSummaryGenerator.GenerateMarkdown()` output rendered in GitHub Actions-styled page.
- **Wiki Feature 9 (JSON Report)**: Changed JSON viewer from dark theme (mostly black) to GitHub-styled light theme with visible syntax highlighting and toolbar.
- **Wiki Feature 11 (DiagramFocus)**: Set diagram Details to "Expanded" before screenshot so response notes with highlighted fields are visible.
- **Wiki Feature 12 (Failure Clustering)**: Changed test data so 4 failures share the same error message (`Connection refused (Stock Service:5001)`), enabling the `FailureClusterer` to produce a visible cluster. GIF now leads with the cluster section.

## [2.22.27] - 2026-04-25

### Fixed
- **Race condition in ParameterGrouper.Analyze**: R2 flattening mutated shared `Scenario` objects' `ExampleValues`/`ExampleRawValues` dictionaries. When `Parallel.Invoke` in `CreateStandardReportsWithDiagrams` generated multiple reports concurrently, thread B could encounter a `KeyNotFoundException` reading a key that thread A's flattening had already replaced. `Analyze` now deep-clones all scenario dictionaries at entry, so each parallel call works on independent copies.

### Added
- **TUnit end-to-end integration tests**: Added TUnit to the integration test matrix (`TestProjects.All`) with full C# attribute → TUnit adapter → report HTML pipeline verification. 8 new tests in `TUnitParameterizedRenderingTests.cs` covering R1 (scalar `[Arguments]`), R2 (flattened `[MethodDataSource]` records), R3 (sub-table for complex params), and R4 (expandable nested objects).
- **TUnit parameterized example tests**: Added `Parameterized_Feature.cs` to the TUnit example project with 10 test cases covering all 4 rendering rules using real `[Arguments]` and `[MethodDataSource]` attributes with `OrderScenario`, `ShippingAddress`, and `CustomerOrder` records.
- **TUnit `dotnet run` support**: `TestProjectRunner` now detects Microsoft.Testing.Platform projects (TUnit) and uses `dotnet run` instead of `dotnet test`, which is required on .NET 10+.
- **ReportParser `ExtractParameterizedGroupsAsync`**: New helper for asserting parameterized rendering in HTML reports, extracting scenario names, column headers, row counts, sub-table and expandable presence.

## [2.22.26] - 2026-04-25

### Added
- **Realistic PlantUML diagrams in integration tests**: `GenerateReport` helper now embeds per-scenario PlantUML sequence diagrams instead of empty strings, eliminating "Decompression error" in generated HTML reports and making them viewable in a browser.
- **TUnit R3/R4 integration tests**: Added `TUnit_scalar_plus_small_complex_object_renders_R3_subtable` and `TUnit_scalar_plus_deeply_nested_object_renders_R4_expandable` covering sub-table and expandable rendering for TUnit adapter patterns.

## [2.22.25] - 2026-04-24

### Fixed
- **Truncated record ToString() parsing**: `TryParseRecordToString` now handles records truncated by xUnit/MSTest display name limits (ending in `··...` or `...` instead of ` }`), extracting all fully-parsed properties. Previously, truncated records fell back to R0 (raw string in a single cell) instead of R2 (flattened columns).

### Added
- **Comprehensive parameterized rendering integration tests**: 29 new tests in `ParameterizedRenderingIntegrationTests.cs` covering all 8 framework adapters (xUnit2, xUnit3, TUnit, NUnit4, MSTest, LightBDD, ReqNRoll, BDDfy) with scalar, complex record, truncated record, nullable, nested, and edge case scenarios. Tests generate full HTML reports and verify correct R0/R1/R2/R3/R4 rendering.
- **5 new parser unit tests** for truncated record handling (mid-property-name, mid-value, mid-quoted-string, plain ellipsis).

## [2.22.24] - 2026-04-24

### Added
- **String-based record `ToString()` parsing for parameterized groups**: When test parameters are complex objects without raw .NET object references (e.g. display-name-only parsing), the report now parses C# record `ToString()` representations (e.g. `TypeName { Prop = Val, ... }`) and decomposes them into individual table columns (R2 flattening), sub-tables (R3), or expandable details (R4).
- **Defensive error handling**: All string-based record parsing is wrapped in try-catch with `Debug.WriteLine` warnings, ensuring malformed values gracefully fall back to plain text rendering with no cascading failures.

## [2.22.23] - 2026-04-24

### Fixed
- **DependencyCategory defaults for all trackers**: `MessageTracker` and all 15 extension trackers now set the correct `DependencyCategory` on `RequestResponseLog` entries, so PlantUML diagrams render the correct participant shapes (e.g. `queue` for message brokers, `database` for databases, `collections` for storage) instead of defaulting to `entity`.
- **MessageTrackerOptions.DependencyCategory**: New property (default `"MessageQueue"`) allowing callers to override the category used by `MessageTracker`.
- **DependencyPalette**: Added 7 new category mappings: `MessageQueue`, `MongoDB`, `DynamoDB`, `Elasticsearch`, `S3`, `CloudStorage`, `gRPC`.

## [2.22.22] - 2026-04-24

### Fixed
- **Wiki GIF tests**: Rewrote all `WikiGifTests` with correct CSS selectors, rich test data (50+ scenarios across 8 features), and proper PlantUML rendering (force-call `_renderDiagramsInContainer` for headless Chrome where `IntersectionObserver` doesn't fire).
- **GIF file sizes**: Added `-resize 960x -fuzz 2% -layers optimize` to ImageMagick stitching, reducing GIF sizes from ~280MB total to ~7.3MB total. Increased `WaitForExit` timeout from 120s to 600s to prevent truncated GIFs.
- **Feature01 (Interactive Report)**: Reduced Hold() durations to target ~34s (was ~47s). Shows 6+ filter combinations (Failed, Passed, Dependency, P95, Happy Paths, Categories).
- **Feature09 (CI Summary)**: Rewritten to use real report with JS-injected GitHub Actions header instead of broken custom HTML page.

### Changed
- **What's New In 2.0 wiki**: Removed former Feature 13 (Category Filtering) and renumbered remaining features.

## [2.22.21] - 2026-04-24

### Added
- **`MessageTrackerOptions.UseHttpContextCorrelation`**: When `true`, the `MessageTracker` reads test info from `IHttpContextAccessor` request headers first (the same dual-layer correlation used by the legacy constructor), falling back to `CurrentTestInfoFetcher` when `HttpContext` is null. This enables safe migration from the legacy constructor without losing interactions tracked via HTTP-propagated headers.
- **`TestTrackingMessageHandlerOptions.ClientNamesToServiceNames`**: Maps `IHttpClientFactory` client names to human-readable service names for diagrams. Useful when HTTP mocking makes port-based mapping via `PortsToServiceNames` unreliable. Pass the client name via `new TestTrackingMessageHandler(options, clientName: builder.Name)`. Resolution order: `FixedNameForReceivingService` > `ClientNamesToServiceNames` > `PortsToServiceNames` > `localhost:port`.

### Documentation
- **Event-Annotations wiki**: Added migration warning for `UseHttpContextCorrelation`, fixed all `CurrentTestInfoFetcher` examples to use null-safe patterns (replaced `TestContext.Current.Test!.TestDisplayName` with null-check), added `UseHttpContextCorrelation` to the options properties table.
- **HTTP-Tracking-Setup wiki**: Added `ClientNamesToServiceNames` to the `TestTrackingMessageHandlerOptions` reference table.
- **Tracking-Dependencies wiki**: Added simplified `ClientNamesToServiceNames` example alongside existing Pattern 8.
- **Generated-Reports wiki**: Added "Validating Tracking Configuration Changes" section with gold standard comparison workflow.

## [2.22.20] - 2026-04-24

### Changed
- **TTD Version row in Test Execution Summary is now hidden**: The TTD Version row in the HTML report summary table is now `display:none` so it doesn't clutter the visible report. The version is still present in the HTML (and in the `<meta name="generator">` tag) for diagnostic purposes.

## [2.22.19] - 2026-04-23

### Fixed
- **Failure cluster links broken for parameterized test scenarios**: Links in the failure clusters section did not navigate to parameterized test rows because individual `<tr>` rows only had `data-scenario-id` attributes, not `id` attributes. Added `id` to parameterized `<tr>` rows and updated the onclick handler to walk up ancestor `<details>` elements (opening them) and trigger row selection via `click()`.

## [2.22.18] - 2026-04-23

### Fixed
- **Flaky `No_unused_component_warning_when_no_components_registered` test**: `MessageTrackerTests` and `TestTrackingMessageHandlerTests` construct `MessageTracker`/`TestTrackingMessageHandler` instances whose constructors call `TrackingComponentRegistry.Register()`, but these test classes were not in the `"TrackingComponentRegistry"` xUnit collection. This allowed them to run in parallel with `ReportDiagnosticsTests`, polluting the static registry between `Clear()` and `Analyse()`. Added `[Collection("TrackingComponentRegistry")]` to both classes.

## [2.22.17] - 2026-04-23

### Fixed
- **Scenario-steps `<details>` not aligned with example-diagrams `<details>`**: `.scenario-steps` had `padding: 0.5em 1em` on the container, pushing its content inward compared to `.example-diagrams` which has no container padding. Removed container padding and moved it to `.scenario-steps > summary` (`padding: 1em`, matching `.example-diagrams > summary`) and child steps (`margin-left/right: 1em`), so both sections are now left-aligned.

## [2.22.16] - 2026-04-23

### Fixed
- **Component diagram not embedded in TestRunReport when ComponentDiagramOptions is null**: The embed decision in `CreateStandardReportsWithDiagrams` used `options.ComponentDiagramOptions?.EmbedInTestRunReport == true` which evaluates to `false` when `ComponentDiagramOptions` is `null` (the default). This contradicted the PlantUML generation path on the same method which already falls back to `new ComponentDiagramOptions()` (where `EmbedInTestRunReport` defaults to `true`). Extracted the decision into `ShouldEmbedComponentDiagram()` using the same `?? new ComponentDiagramOptions()` null-coalesce pattern — no mutation of the original options object, avoiding the WAF NRE that the v2.22.6 fix caused.

## [2.22.15] - 2026-04-23

### Added
- **Phase-aware tracking configuration (Setup vs Action)**: All tracking extensions now support configuring behavior differently for the Setup phase (Given/Arrange) vs the Action phase (When/Act). This allows reducing noise from setup operations while keeping full detail for the actions under test.
  - **`TestPhase` enum**: New `Unknown`, `Setup`, `Action` values representing the current test phase.
  - **`TestPhaseContext`**: AsyncLocal-based ambient context (similar to `TrackingTraceContext`) that holds the current `TestPhase`. BDD frameworks (BDDfy, LightBDD, ReqNRoll) set this automatically based on the step type keyword. Non-BDD tests use `TrackingDiagramOverride.StartSetup()` and `TrackingDiagramOverride.StartAction()`.
  - **`PhaseConfiguration`**: Utility class providing `ShouldTrack()` (phase-aware enable/disable), `GetEffectiveVerbosity<T>()` (phase-aware verbosity override), and `ResolvePhaseFromStepType()` (keyword-to-phase mapping).
  - **Phase on `RequestResponseLog`**: Each log entry now carries a `Phase` property indicating which test phase produced it.
  - **`TrackingDiagramOverride.StartSetup()`**: New method (with `Action` delegate overload) to explicitly mark the Setup phase.
- **Phase properties on all extension options**: Every tracking extension options class now has `SetupVerbosity?`, `ActionVerbosity?`, `TrackDuringSetup` (default `true`), and `TrackDuringAction` (default `true`) properties. When a phase-specific verbosity is set, it overrides the default `Verbosity` during that phase. Setting `TrackDuringSetup = false` suppresses all tracking during setup.
- **Phase-aware tracker implementations**: All 21 tracker implementations (EF Core, Redis, Kafka, gRPC, MassTransit, MongoDB, CosmosDB, Elasticsearch, Dapper, BigQuery, BlobStorage, CloudStorage, DynamoDB, EventBridge, S3, SNS, SQS, StorageQueues, EventHubs, PubSub, ServiceBus) now check `PhaseConfiguration.ShouldTrack()` before recording and use `PhaseConfiguration.GetEffectiveVerbosity()` to resolve the active verbosity level.
- **Phase-aware core handlers**: `TestTrackingMessageHandler`, `MessageTracker`, `TrackingProxy`, and `RequestResponseLogger` all support phase-based filtering and verbosity.
- **30 new unit tests** for `TestPhaseContext` (5 tests) and `PhaseConfiguration` (25 tests) covering all phase combinations, verbosity override precedence, and step-type keyword resolution.

## [2.22.14] - 2026-04-23

### Changed
- **Scenario-steps sections now have a rounded border**: `.scenario-steps` `<details>` elements are now surrounded by a slightly rounded `1px solid` border matching the `.example-diagrams` styling (`border-radius: 1em`, `border-color: rgb(224, 224, 224)`). The summary also received `background-color: white` and `border-radius: 1em` for visual consistency.
- **Parameterized detail-panel steps now use collapsible `<details>/<summary>`**: Steps within `param-detail-panel` were previously rendered as a plain `<div>`. They now use the same `<details class="scenario-steps" open><summary class="h4">Steps</summary>` pattern as normal scenario steps, making them collapsible and visually consistent.
- **Violet theme border override updated**: The violet theme's `.scenario-steps` override changed from `border-left-color` to `border-color` to match the new full-border design.

## [2.22.13] - 2026-04-23

### Fixed
- **Flaky TrackingComponentRegistry tests under parallel execution**: `TrackingComponentRegistry.Clear()` used a `while (TryTake)` drain loop on `ConcurrentBag` which is not atomic — items added from other threads could survive the clear due to thread-local storage stealing delays. Replaced with `Interlocked.Exchange` to atomically swap in a fresh empty bag. Also added `[CollectionDefinition]` for the `"TrackingComponentRegistry"` xUnit collection to ensure proper test serialisation.

## [2.22.12] - 2026-04-23

### Fixed
- **Note toggle buttons not working with SeparateSetup/partition diagrams**: `findNoteGroups()` incorrectly detected participant boxes and partition labels (fill `#E2E2F0`) as note groups, causing a mismatch between SVG note groups and source note blocks. Hover rects and button click handlers were attached to the wrong elements, making note collapse/expand/cycle actions appear broken. Fixed by excluding the standard PlantUML participant/partition fill (`#E2E2F0`) from `hasNoteFill()` and adding a safety-net fill-frequency filter in `makeNotesCollapsible()` that reconciles group counts with note block counts.
- **WebApplicationFactory NRE on teardown when ComponentDiagramOptions is null**: The v2.22.6 fix for `ComponentDiagramOptions` null handling changed the semantic behavior — `null` options now defaulted to embedding the component diagram (via `new ComponentDiagramOptions()` where `EmbedInTestRunReport = true`). This caused unexpected component diagram embedding for consumers who never configured `ComponentDiagramOptions`, adding work during report generation that could trigger a `NullReferenceException` in `WebApplicationFactory<T>.DisposeAsync()`. Reverted to the v2.22.5 null-conditional behavior: `null` `ComponentDiagramOptions` now means "don't embed".

### Added
- **TTD version embedded in all reports**: The TestTrackingDiagrams version is now included in HTML reports (as a `<meta name="generator">` tag and in the Test Execution Summary table), JSON data (`ttdVersion` field), YAML data (`TtdVersion` field), XML data (`<TtdVersion>` element), and the JSON schema.

## [2.22.11] - 2026-04-23

### Added
- **R2 (FlattenedObject) parameter display rule**: When a parameterized test has a single complex object parameter with all scalar properties (≤ max columns), its properties are automatically flattened into individual columns in the parameter table. This provides a clear, readable view of complex test case objects instead of showing a single wall-of-text column. Only applies when structured extraction is available (xUnit3, TUnit, NUnit4).
- **R3 (SubTable) cell rendering**: Within R1/R2 parameter tables, individual cell values that are small complex objects (≤5 scalar properties, no nesting) are rendered as a mini sub-table inside the cell, with property names as row headers and values as row data.
- **R4 (ExpandableComplex) cell rendering**: Within R1/R2 parameter tables, individual cell values that are deeply complex (nested objects, arrays, or >5 properties) are rendered as a collapsible `<details>/<summary>` element with a type-name preview and syntax-highlighted JSON expansion body.
- **`ExampleRawValues` on Scenario**: New `Dictionary<string, object?>?` property that preserves the raw parameter objects (not just `ToString()` strings) for reflection-based R2/R3/R4 rendering.
- **`ParameterParser.ExtractStructuredParametersWithRaw()`**: New method that returns both string values and raw object references, used by xUnit3, TUnit, and NUnit4 adapters.
- **`ParameterValueRenderer` helper class**: Internal static class providing object introspection (`IsScalarType`, `IsSmallComplexObject`, `IsComplexValue`, `TryGetFlattenableProperties`), property flattening (`FlattenToStringValues`, `FlattenToRawValues`), and HTML rendering (`RenderSubTable`, `RenderExpandable`, `GenerateHighlightedJson`).
- **CSS for R3/R4**: `.cell-subtable` styles for sub-table cells and `details.param-expand` / `.expand-body` / `.prop-key` / `.prop-val` styles for expandable complex parameter cells.
- **JavaScript for R4 interaction**: Expanding a `<details class="param-expand">` element auto-selects the containing row (switching diagrams/detail panels); sub-table clicks don't bubble to row selection.
- **40 new unit tests** for `ParameterValueRenderer` covering type classification, property introspection, flattening, sub-table rendering, expandable rendering, JSON highlighting, and preview generation.
- **5 new R2 detection tests** in `ParameterGrouperTests` covering single complex param flattening, nested objects not flattening, no raw values fallback, multiple params not triggering R2, and max columns exceeded.
- **10+ new R2/R3/R4 rendering integration tests** in `ParameterizedGroupRenderTests` verifying flattened property columns, sub-table HTML, expandable details HTML, CSS/JS presence, and correct scalar fallback for single primitives.

### Changed
- **xUnit3, TUnit, NUnit4 adapters**: Updated to use `ExtractStructuredParametersWithRaw()` and populate `ExampleRawValues` alongside `ExampleValues`, enabling R2/R3/R4 cell rendering when structured extraction is available.
- **`ParameterGrouper.DetermineParamsAndRule()`**: Now detects R2 (FlattenedObject) when all scenarios have a single complex parameter with all-scalar properties ≤ max columns, flattening the property names into columns and updating `ExampleValues`/`ExampleRawValues` on each scenario.
- **`ReportGenerator.RenderParameterizedGroup()`**: Header and cell rendering now handles `FlattenedObject` rule identically to `ScalarColumns`, and applies per-cell R3/R4 rendering based on `ExampleRawValues` type inspection.

## [2.22.10] - 2026-04-23

### Added
- **Structured parameter extraction for TUnit, NUnit4, and MSTest adapters**: Parameterized test tables now use real parameter names instead of positional `arg0`, `arg1` keys when the framework provides access to method arguments and parameter metadata.
  - **TUnit**: Uses `TestDetails.TestMethodArguments` and `MethodMetadata.Parameters` to extract named parameter values directly, bypassing string parsing entirely.
  - **NUnit4**: Uses `TestContext.Test.Arguments` and `TestContext.Test.Method.GetParameters()` for the same structured extraction.
  - **MSTest**: Captures parameter names via `MethodInfo.GetParameters()` in `DiagrammedComponentTest.TestTrackingCleanup()` and rebinds positional keys from display-name parsing to real parameter names. Added `ParameterNames` property to `MSTestScenarioInfo`.
  - **xUnit3**: Refactored to use shared `ParameterParser.ExtractStructuredParameters()` method (no behavioral change — xUnit3 already had structured extraction).
- **`ParameterParser.ExtractStructuredParameters()` shared method**: New public method on `ParameterParser` that maps raw argument values to parameter names, used by all adapters that support structured extraction.

## [2.22.9] - 2026-04-23

### Changed
- **Steps section is now collapsible**: Test steps within each scenario are wrapped in a `<details>` element with a "Steps" summary heading, matching the pattern used by the Diagrams section. Steps are expanded by default but can be collapsed by clicking the heading to reduce visual noise, especially for scenarios with many or long parameterized steps.
- **Removed left border from top-level steps**: The 3px vertical border on the left of the top-level steps container has been removed for a cleaner look. Sub-step borders are preserved.

## [2.22.8] - 2026-04-23

### Fixed
- **Parameter parser now handles curly-brace nesting in C# record `ToString()` output**: `SplitParams()` and `FindColon()` previously only tracked parenthesis depth, so commas inside `TypeName { Prop = val, ... }` structures (produced by C# record auto-generated `ToString()`) were incorrectly treated as top-level parameter separators. This caused parameterized test tables to split a single complex parameter into multiple mangled columns. Both methods now track `braceDepth` alongside `parenDepth`.

## [2.22.7] - 2026-04-23

### Fixed
- **Removed redundant "Loading component diagram..." text**: The placeholder text was shown alongside the "Rendering Diagram..." indicator from the PlantUML renderer, creating duplicate loading messages.
- **Component Diagram and Scenario Timeline toggles now act as radio buttons**: Activating one automatically deactivates the other and removes its active styling, preventing both panels from being visible simultaneously.

## [2.22.6] - 2026-04-23

### Fixed
- **Component diagram not embedded in TestRunReport when ComponentDiagramOptions is null**: When users did not explicitly set `ComponentDiagramOptions` on `ReportConfigurationOptions` (the common case), the null-conditional `?.EmbedInTestRunReport == true` evaluated to `null == true` → `false`, silently suppressing the embedded component diagram despite `EmbedInTestRunReport` defaulting to `true`. Now falls back to `new ComponentDiagramOptions()` before checking the flag.

## [2.22.5] - 2026-04-22

### Fixed
- **Fixed flaky Selenium note toggle tests under concurrent load**: `WaitForReRender()` now waits for both the SVG re-render AND `makeNotesCollapsible()` to finish adding `.note-toggle-icon` elements. Previously it returned as soon as the SVG innerHTML changed, but under CPU contention from parallel Chrome instances, there was a timing gap before the JS callback created toggle icons — causing assertions on button counts/types to see stale or missing state. `SetScenarioState()` also now waits for toggle icons alongside hover rects.

## [2.22.4] - 2026-04-22

### Changed
- **Component diagram is now a toolbar toggle instead of a collapsible `<details>` section**: The embedded component diagram in the TestRunReport is hidden by default and revealed via a "Component Diagram" toggle button in the toolbar (matching the existing "Scenario Timeline" button style). This avoids the large diagram dominating the report on load. The PlantUML renderer is triggered on first show via `_renderDiagramsInContainer`, ensuring the diagram renders correctly despite being initially hidden from the IntersectionObserver.

## [2.22.3] - 2026-04-22

### Fixed
- **Restored activity diagram and flame chart span capture**: The v2.21.1 fix for Application Insights dependency tracking over-corrected by excluding *all* well-known auto-instrumentation `ActivitySource`s (`Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `Npgsql`, `StackExchange.Redis`, etc.) from the `InternalFlowActivityListener`. Since `FilterByAutoInstrumentation` requires at least one well-known source span to anchor traces, this caused activity diagrams and flame charts to be completely empty for projects not using `AddTestTrackingExporter()`. The listener now only excludes `System.Net.Http` — the sole source where `ActivitySource.HasListeners()` triggers a mutually exclusive code path in `DiagnosticsHandler` that breaks Application Insights dependency telemetry.

## [2.22.2] - 2026-04-22

### Fixed
- **Note collapse/expand 3-state cycle for long notes**: Fixed three bugs in the note truncation/collapse/expand system:
  1. The ▼ (expand) button and + button from collapsed state now correctly go to **truncated** (step 1) for long notes, instead of skipping directly to fully expanded (step 2).
  2. The double-click cycle from collapsed state now correctly goes to **truncated** for long notes, instead of fully expanded.
  3. `isLongNote()` checks now use the container's per-scenario `_truncateLines` value instead of falling back to the global `window._truncateLines`. This fixes the ▲ button not appearing after scenario-level truncation changes.
- **Tooltip truncation for collapsed notes**: Collapsed note tooltips now respect the container's per-scenario truncation level instead of always using the global default.

### Added
- **Comprehensive Selenium tests for note state transitions**: Added 16 new Selenium tests covering all note collapse/expand/truncate state transitions including:
  - Long note 3-state double-click cycle (expanded → truncated → collapsed → truncated)
  - ▼ button from collapsed → truncated (not expanded) for long notes
  - ▼ button from truncated → expanded
  - ▲ button visibility and click behavior
  - Short note 2-state cycle (expanded ↔ collapsed)
  - Truncation level changes affecting note "long" classification
  - Minus button state transitions

## [2.22.1] - 2026-04-22

### Fixed
- **Deferred `InternalFlowActivityListener` startup**: The `TestTrackingMessageHandler` constructor no longer calls `InternalFlowActivityListener.EnsureStarted()`. The listener is now started lazily on the first `SendAsync()` call. Registering an `ActivityListener` during DI resolution could alter `ActivitySource.HasListeners()` state before Application Insights' `DependencyTrackingTelemetryModule` and the host had fully initialised, preventing `DiagnosticsHandler` from being added to the HTTP pipeline and silently breaking HTTP dependency telemetry.
- **Traceparent injection limited to TestServer scenarios**: `TestTrackingMessageHandler.SendAsync()` now only injects the `traceparent` header when `Activity.Current` is null (i.e. in-process TestServer calls). When an ambient Activity already exists, framework handlers (e.g. `DiagnosticsHandler` inside `SocketsHttpHandler`) create proper child Activities and inject `traceparent` themselves — pre-empting this by injecting the parent’s span ID broke Application Insights dependency correlation.
- **Fixed flaky `No_tracking_component_section_when_none_registered` test**: The diagnostic report test now clears `TrackingComponentRegistry` before asserting, preventing pollution from parallel test runs that create `TestTrackingMessageHandler` instances.

## [2.22.0] - 2026-04-22

### Added
- **Dependency-Type Colored Arrows & Typed Shapes**: Sequence and component diagrams now color-code arrows and use typed shapes based on the target service's dependency type (Issue #21).
  - New `DependencyCategory` parameter on `RequestResponseLog` — set automatically by all extension packages (CosmosDB, Redis, EF Core, ServiceBus, BigQuery, BlobStorage).
  - New `DependencyType` enum: `HttpApi`, `Database`, `Cache`, `MessageQueue`, `Storage`, `Unknown`.
  - New `DependencyPalette` static class with default vivid color palette and category-to-type resolution.
  - **Sequence diagrams**: Services render with typed PlantUML shapes (`database` for DB/Storage, `collections` for Cache, `queue` for MessageQueue, `entity` for HTTP APIs). Request/response arrows are colored by target service type.
  - **Component diagrams**: C4 mode uses `SystemDb()`/`SystemQueue()` for appropriate types. Plain PlantUML mode uses `database`/`collections`/`queue` shapes with matching skinparams. Arrows colored by dependency type (default) or P95 latency (opt-in via `ArrowColorMode.Performance`).
  - New `ArrowColorMode` enum to select between `DependencyType` (default) and `Performance` arrow coloring.
  - New config options on `ReportConfigurationOptions`: `SequenceDiagramArrowColors`, `SequenceDiagramParticipantColors`, `DependencyColors`, `ServiceTypeOverrides`.
  - New `DependencyColors` property on `ComponentDiagramOptions` for per-diagram color overrides.
- **Embedded Component Diagram in TestRunReport**: When `ComponentDiagramOptions.EmbedInTestRunReport` is `true` (default), the component diagram is rendered inline in the TestRunReport as a collapsible section before the scenario list, using the same BrowserJs PlantUML renderer. The standalone `ComponentDiagram.html` file continues to be generated as well.

### Changed
- Default arrow style in both sequence and component diagrams is now dependency-type colored. Previous behavior (plain arrows or P95-based coloring) is available via `SequenceDiagramArrowColors = false` or `ArrowColorMode.Performance`.
- `GetProtocol` in `ComponentDiagramGenerator` now prefers `DependencyCategory` over HTTP method when available, producing labels like "CosmosDB: Query" instead of "HTTP: POST".

## [2.21.2] - 2026-04-22

### Fixed
- **`TestTrackingMessageHandler.SendAsync` no longer sets `Activity.Current`**: The handler was creating a `new Activity("TestTrackingDiagrams.Request").Start()` when no ambient Activity existed, which set `Activity.Current` and interfered with Application Insights' telemetry correlation — `DependencyTelemetry` items received the wrong `Context.Operation.Name` (or none at all) instead of the server request's operation name (e.g. `"GET /health"`). The handler now generates trace/span IDs directly via `ActivityTraceId.CreateRandom()` / `ActivitySpanId.CreateRandom()` and injects the `traceparent` header without creating an Activity. This preserves W3C trace context propagation for InternalFlow span correlation while leaving `Activity.Current` untouched.

## [2.21.1] - 2026-04-22

### Fixed
- **`InternalFlowActivityListener` no longer breaks Application Insights HTTP dependency tracking**: The listener was subscribing to ALL `ActivitySource`s (`ShouldListenTo = _ => true`), which caused .NET's `System.Net.Http.DiagnosticsHandler` to take the `ActivitySource`-based code path instead of the `DiagnosticListener`-based path. Application Insights SDK 2.x only creates `DependencyTelemetry` from the latter, so HTTP dependency tracking was silently broken. The listener now excludes well-known auto-instrumentation sources (e.g. `System.Net.Http`, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`) via `InternalFlowSpanCollector.WellKnownAutoInstrumentationSources`. Custom application `ActivitySource` spans are still captured for internal flow diagrams.

## [2.21.0] - 2026-04-22

### Added
- **`MessageTracker` upgraded to first-class tracking component**: The core `MessageTracker` class (used for tracking custom messaging abstractions) now implements `ITrackingComponent` with auto-registration in `TrackingComponentRegistry`, enabling unused-component diagnostic warnings.
  - New `MessageTrackerOptions` record with `ServiceName`, `CallingServiceName`, `Verbosity`, `CurrentTestInfoFetcher`, and `SerializerOptions` — aligns `MessageTracker` with the same options pattern used by all extension packages.
  - New `MessageTracker(MessageTrackerOptions)` constructor — recommended for new code. The legacy `IHttpContextAccessor`-based constructor is preserved for backward compatibility.
  - New `MessageTrackerVerbosity` enum (Raw, Detailed, Summarised) — `Summarised` omits message payloads from diagrams.
  - New `TrackSendEvent()` one-shot method — logs a complete fire-and-forget request/response pair in a single call, reducing boilerplate for event-driven patterns.
  - New `TrackMessagesForDiagrams(MessageTrackerOptions)` DI overload.

### Fixed
- Fixed `ReportDiagnosticsTests.Unused_component_warning_lists_count` flaky test — now clears `TrackingComponentRegistry` before asserting component counts, preventing pollution from parallel test runs.

## [2.20.0] - 2026-04-22

### Added
- **New `TestTrackingDiagrams.Extensions.EventBridge` package**: Track Amazon EventBridge operations in test diagrams via the AWS SDK HTTP pipeline. Intercepts `X-Amz-Target` JSON-RPC calls using the same `DelegatingHandler` pattern as the S3, DynamoDB, SQS, and SNS extensions. Includes:
  - `EventBridgeTrackingMessageHandler` — DelegatingHandler implementing `ITrackingComponent` with auto-registration. Classifies and logs PutEvents, rule management, target management, event bus lifecycle, archive, replay, and tagging operations.
  - `EventBridgeOperationClassifier` — Dictionary-based classifier mapping 28 `X-Amz-Target` headers to `EventBridgeOperation` enum values, with JSON body parsing for PutEvents (DetailType, Source, EntryCount, EventBusName) and rule operations (Name, EventBusName).
  - `AmazonEventBridgeConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonEventBridgeConfig` that injects the tracking handler via `HttpClientFactory`.
  - URI scheme: `eventbridge://{busName}/` (Detailed/Summarised), original AWS URL (Raw).
  - Three verbosity levels: Raw (full HTTP details), Detailed (classified labels with context like `PutEvents [OrderCreated] x5`), Summarised (grouped labels like `ManageRule`, `ManageTargets`, `ManageBus`).
  - Default excluded operations: TagResource, UntagResource, ListTagsForResource, ListEventBuses.

## [2.19.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Dapper` package**: Track Dapper and ADO.NET SQL operations in test diagrams. Wraps `DbConnection` to intercept all query execution with zero Dapper-specific dependencies — works with any ADO.NET provider. Includes:
  - `TrackingDbConnection` — Decorator wrapping `DbConnection` that implements `ITrackingComponent` with auto-registration. Creates `TrackingDbCommand` instances that intercept `ExecuteReader`, `ExecuteNonQuery`, `ExecuteScalar` (sync + async).
  - `TrackingDbCommand` — Intercepts all execution methods, classifies the SQL, and logs request/response pairs to `RequestResponseLogger`.
  - `TrackingDbTransaction` — Transparent wrapper that logs `BEGIN`, `COMMIT`, and `ROLLBACK` operations.
  - `DapperOperationClassifier` — Regex-based classifier recognising 15 SQL operation types (Query, Insert, Update, Delete, Merge, StoredProcedure, CreateTable, AlterTable, DropTable, CreateIndex, Truncate, BeginTransaction, Commit, Rollback, Other) with table name extraction.
  - `DbConnectionExtensions.WithTestTracking()` — Fluent extension on any `DbConnection` that wraps it in a `TrackingDbConnection`.
  - URI scheme: `sql://dataSource/database/table` (Detailed), `sql://dataSource/database` (Raw), `sql:///database/table` (Summarised).
  - Three verbosity levels with configurable SQL text logging, parameter logging, and operation exclusions.

## [2.18.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Elasticsearch` package**: Track Elasticsearch operations in test diagrams via the Elastic .NET client's `OnRequestCompleted` callback. Intercepts and classifies REST API operations across indices. Includes:
  - `ElasticsearchTrackingCallbackHandler` — Callback handler implementing `ITrackingComponent`. Classifies and logs index, search, document, bulk, and cluster operations.
  - `ElasticsearchOperationClassifier` — Classifies 24 Elasticsearch REST API operations (IndexDocument, GetDocument, Search, Bulk, CreateIndex, DeleteIndex, etc.) from URL path patterns and HTTP methods.
  - `ElasticsearchClientSettingsExtensions.WithTestTracking()` — Fluent extension on `ElasticsearchClientSettings` that enables `DisableDirectStreaming` and registers the tracking callback.
  - URI scheme: `elasticsearch:///indexName` (Detailed), full request URI (Raw), `elasticsearch:///` (Summarised).
  - Configurable operation exclusions (ClusterHealth and CatApis excluded by default), request/response body capture, and three verbosity levels.

## [2.17.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Kafka` package**: Track Apache Kafka produce and consume operations in test diagrams using wrapper classes around Confluent.Kafka's `IProducer<TKey,TValue>` and `IConsumer<TKey,TValue>`. Includes:
  - `KafkaTracker` — Central logging component implementing `ITrackingComponent`. Logs produce, consume, subscribe, and commit operations with Event MetaType. Consume operations swap caller/service names to reflect incoming message direction.
  - `TrackingKafkaProducer<TKey,TValue>` — Wrapper implementing `IProducer<TKey,TValue>` that intercepts `Produce` and `ProduceAsync` calls with topic, partition, and offset tracking.
  - `TrackingKafkaConsumer<TKey,TValue>` — Wrapper implementing `IConsumer<TKey,TValue>` that intercepts `Consume` and `Subscribe` calls, skipping EOF/null results.
  - `KafkaOperationClassifier` — Classifies operations (Produce, ProduceAsync, Consume, Subscribe, Unsubscribe, Commit, Flush) with topic, partition, and offset details.
  - URI scheme: `kafka:///topic` (Detailed), `kafka:///topic/partition@offset` (Raw), `kafka:///` (Summarised).
  - Supports configurable produce/consume/subscribe/commit tracking, message key/value logging, and three verbosity levels.

## [2.16.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.MassTransit` package**: Track MassTransit message operations (RabbitMQ, Azure Service Bus, Amazon SQS, and other transports) in test diagrams using MassTransit observer interfaces. Includes:
  - `MassTransitTracker` — Central logging component implementing `ITrackingComponent`. Logs send, publish, consume, and fault operations with Event MetaType.
  - `TrackingSendObserver`, `TrackingPublishObserver`, `TrackingConsumeObserver` — MassTransit observer implementations that delegate to the tracker.
  - `MassTransitOperationClassifier` — Classifies operations (Send, Publish, Consume, SendFault, PublishFault, ConsumeFault) with message type and URI extraction.
  - `BusConfigurationExtensions.WithTestTracking()` — Fluent extension on `IBusFactoryConfigurator`.
  - URI scheme: `masstransit:///queue-name` (Detailed) or transport URI (Raw).
  - Supports configurable send/publish/consume tracking, message body logging, and fault logging.

## [2.15.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.StorageQueues` package**: Track Azure Storage Queue operations in test diagrams using the Azure.Core Transport pattern (same as BlobStorage). Includes:
  - `StorageQueueOperationClassifier` — Classifies Storage Queue REST API operations (SendMessage, ReceiveMessages, PeekMessages, DeleteMessage, UpdateMessage, ClearMessages, CreateQueue, DeleteQueue, GetProperties, SetMetadata, ListQueues) from URL path patterns and query parameters.
  - `StorageQueueTrackingMessageHandler` — `DelegatingHandler` + `ITrackingComponent` that intercepts HTTP requests, classifies operations, and logs request/response pairs.
  - `QueueClientOptionsExtensions.WithTestTracking()` — Fluent extension on `QueueClientOptions` that sets `Transport` to `HttpClientTransport` with tracking handler.
  - URI scheme: `storagequeue:///queueName`.
  - Supports three verbosity levels: Raw, Detailed (with queue name in labels), and Summarised.

## [2.14.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.EventHubs` package**: Track Azure Event Hubs operations in test diagrams using the wrapper/decorator pattern around `EventHubProducerClient` and `EventHubConsumerClient`. Includes:
  - `EventHubsOperationClassifier` — Classifies Event Hubs operations (Send, SendBatch, CreateBatch, ReadEvents, ReadEventsFromPartition, GetPartitionIds, GetEventHubProperties, GetPartitionProperties, StartProcessing, StopProcessing, ProcessEvent) by method name with event count awareness.
  - `EventHubsTracker` — Central logging helper implementing `ITrackingComponent`. Logs request/response pairs with Event MetaType. Supports three verbosity levels with partition ID in URI.
  - `TrackingEventHubProducerClient` — Wrapper around `EventHubProducerClient` tracking single and batch send operations with event body serialization.
  - `TrackingEventHubConsumerClient` — Wrapper around `EventHubConsumerClient` tracking `ReadEventsAsync` and `ReadEventsFromPartitionAsync` via `IAsyncEnumerable`.
  - URI scheme: `eventhubs:///hub-name[/partition-id]`.

## [2.13.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.CloudStorage` package**: Track Google Cloud Storage operations in test diagrams using the `DelegatingHandler` pattern via Google APIs `HttpClientFactory`. Includes:
  - `CloudStorageOperationClassifier` — Classifies GCS REST API operations (Upload, Download, Delete, ListObjects, GetMetadata, UpdateMetadata, Copy, Compose, CreateBucket, DeleteBucket, GetBucket, ListBuckets) from URL path patterns. Distinguishes Download vs GetMetadata via `alt=media` query parameter.
  - `CloudStorageTrackingMessageHandler` — `DelegatingHandler` + `ITrackingComponent` that intercepts HTTP requests, classifies operations, and logs request/response pairs.
  - `TrackingCloudStorageHttpClientFactory` — Google APIs `HttpClientFactory` wrapper that injects the tracking handler.
  - `StorageClientBuilderExtensions.WithTestTracking()` — Fluent extension on `StorageClientBuilder`.
  - URI scheme: `gcs:///bucket/object` (Detailed) or original Google API URL (Raw).
  - Handles URL-encoded object names, Copy/Compose paths, and bucket-level operations.

## [2.12.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.SNS` package**: Track Amazon SNS operations in test diagrams using the `DelegatingHandler` pattern via `AmazonSimpleNotificationServiceConfig.HttpClientFactory`. Includes:
  - `SnsOperationClassifier` — Classifies SNS operations (Publish, PublishBatch, Subscribe, Unsubscribe, CreateTopic, DeleteTopic, ListTopics, ListSubscriptions, ListSubscriptionsByTopic, GetTopicAttributes, SetTopicAttributes, ConfirmSubscription) from `X-Amz-Target: AmazonSimpleNotificationService.{Op}` header or legacy `Action` query/form parameter. Extracts topic name from `TopicArn`/`TargetArn` ARN fields.
  - `SnsTrackingMessageHandler` — `DelegatingHandler` + `ITrackingComponent` that intercepts HTTP requests, classifies operations, and logs request/response pairs with configurable verbosity. Reconstructs request body after classification for downstream handlers.
  - `AmazonSNSConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonSimpleNotificationServiceConfig` that installs the tracking handler via `HttpClientFactory`.
  - URI scheme: `sns:///topic-name` (Detailed/Summarised) or original AWS URL (Raw).
  - Supports FIFO topics (`.fifo` suffix preserved), direct publish via `TargetArn`, and full ARN extraction.

## [2.11.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.PubSub` package**: Track Google Cloud Pub/Sub operations in test diagrams using wrapper/decorator pattern around `PublisherClient` and `SubscriberClient`. Includes:
  - `PubSubOperationClassifier` — Classifies Pub/Sub operations (Publish, PublishBatch, Pull, Acknowledge, ModifyAckDeadline, Receive, StartSubscriber, StopSubscriber) with short name extraction from full GCP resource paths (`projects/p/topics/t` → `t`).
  - `PubSubTracker` — Central logging helper implementing `ITrackingComponent`. Logs request/response pairs with Event MetaType for publish/receive operations. Supports three verbosity levels.
  - `TrackingPublisherClient` — Wrapper around `PublisherClient` tracking single and batch publish operations with message content at Raw/Detailed verbosity.
  - `TrackingSubscriberClient` — Wrapper around `SubscriberClient` that wraps the message handler callback to track received messages and Ack/Nack replies.
  - URI scheme: `pubsub:///topic-name` (Detailed) or `pubsub:///projects/p/topics/t` (Raw).

## [2.10.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Grpc` package**: Track gRPC client calls in test diagrams using the `Grpc.Core.Interceptors.Interceptor` API. Includes:
  - `GrpcOperationClassifier` — Classifies gRPC calls by method type (Unary, ServerStreaming, ClientStreaming, DuplexStreaming) with service name, method name, and full method path extraction from `ClientInterceptorContext`.
  - `GrpcTrackingInterceptor` — Client-side interceptor that intercepts all gRPC call types (AsyncUnaryCall, BlockingUnaryCall, AsyncServerStreamingCall, AsyncClientStreamingCall, AsyncDuplexStreamingCall). Logs request/response pairs with protobuf message serialization. Implements `ITrackingComponent` with auto-registration. Maps gRPC `StatusCode` to HTTP status codes for consistent error logging.
  - `GrpcChannelExtensions.WithTestTracking()` — Fluent extension on `GrpcChannel` that wraps the channel's `CallInvoker` with the tracking interceptor.
  - Three verbosity levels: Raw (full method path + call type annotation + headers), Detailed (method name with streaming annotations + grpc URI + message content), Summarised (method name only, no content).
  - URI scheme: `grpc:///ServiceName/MethodName` (path-based to preserve casing).
  - Optional `UseProtoServiceNameInDiagram` to use the proto service name instead of the configured `ServiceName`.
  - Streaming calls tracked at initiation level (not per-message) for clean diagrams.

## [2.9.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.SQS` package**: Track Amazon SQS operations in test diagrams. Includes:
  - `SqsOperationClassifier` — Classifies SQS operations from both the JSON protocol (`X-Amz-Target: AmazonSQS.{Op}`) and legacy query protocol (`Action=` parameter in query string or form body). Extracts queue names from URL path (`/account-id/queue-name`), body `QueueUrl` field, or body `QueueName` field. Supports 14 operations: messaging (SendMessage, SendMessageBatch, ReceiveMessage, DeleteMessage, DeleteMessageBatch), visibility (ChangeMessageVisibility, ChangeMessageVisibilityBatch), queue management (CreateQueue, DeleteQueue, GetQueueUrl, GetQueueAttributes, SetQueueAttributes, PurgeQueue, ListQueues).
  - `SqsTrackingMessageHandler` — `DelegatingHandler` that intercepts all SQS HTTP traffic, reads and reconstructs request bodies for classification, and logs request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `AmazonSQSConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonSQSConfig` that injects a tracking `HttpClientFactory` into the AWS SDK pipeline. Zero production code changes required.
  - Three verbosity levels: Raw (HTTP method + full URI), Detailed (classified operation name + `sqs:///QueueName` URI + request/response bodies), Summarised (operation name only, `sqs:///QueueName` URI, no content/headers, skips unrecognised operations).
  - URI scheme: `sqs:///QueueName` (path-based to preserve casing, supports FIFO queue `.fifo` suffix).
  - Default excluded headers: `Authorization`, `x-amz-date`, `x-amz-security-token`, `x-amz-content-sha256`, `User-Agent`, `amz-sdk-invocation-id`, `amz-sdk-request`.

## [2.8.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.MongoDB` package**: Track MongoDB operations in test diagrams using the driver's built-in command monitoring events. Includes:
  - `MongoDbOperationClassifier` — Classifies MongoDB commands by name (find, insert, update, delete, aggregate, count, findAndModify, distinct, bulkWrite, createIndexes, dropIndexes, create, drop, listCollections, listDatabases, getMore), extracts collection name from the command BsonDocument, and optionally extracts filter text.
  - `MongoDbTrackingSubscriber` — Event-driven subscriber that hooks into `CommandStartedEvent`, `CommandSucceededEvent`, and `CommandFailedEvent` via `ClusterBuilder.Subscribe<T>()`. Uses `ConcurrentDictionary<int, PendingOperation>` to correlate request/response pairs by `RequestId`. Implements `ITrackingComponent` with auto-registration.
  - `MongoClientSettingsExtensions.WithTestTracking()` — Fluent extension on `MongoClientSettings` that chains a tracking subscriber into `ClusterConfigurator` without replacing existing configurators.
  - Three verbosity levels: Raw (full BSON command/reply + database.collection + filter), Detailed (operation → collection label + filter as request content), Summarised (operation name only, no content).
  - Default ignored commands: `isMaster`, `hello`, `saslStart`, `saslContinue`, `ping`, `buildInfo`, `getLastError`, `killCursors`. Optional `getMore` tracking (disabled by default).
  - URI scheme: `mongodb:///database/collection` (path-based to preserve casing).

## [2.7.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.DynamoDB` package**: Track Amazon DynamoDB operations in test diagrams. Includes:
  - `DynamoDbOperationClassifier` — Classifies DynamoDB operations from the `X-Amz-Target` header, extracting table names from JSON request bodies (including batch operations via `RequestItems` keys) and PartiQL statements. Supports 19 distinct operations: CRUD (PutItem, GetItem, UpdateItem, DeleteItem), queries (Query, Scan), batch (BatchWriteItem, BatchGetItem), transactions (TransactWriteItems, TransactGetItems), table management (CreateTable, DeleteTable, DescribeTable, ListTables, UpdateTable), and PartiQL (ExecuteStatement, BatchExecuteStatement, ExecuteTransaction).
  - `DynamoDbTrackingMessageHandler` — `DelegatingHandler` that intercepts all DynamoDB HTTP traffic, reads and reconstructs request bodies for classification, and logs request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `AmazonDynamoDBConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonDynamoDBConfig` that injects a tracking `HttpClientFactory` into the AWS SDK pipeline. Zero production code changes required.
  - Three verbosity levels: Raw (HTTP method + full URI), Detailed (classified operation name + `dynamodb:///TableName` URI + request/response bodies), Summarised (operation name only, `dynamodb:///TableName` URI, no content/headers, skips unrecognised operations).
  - Default excluded headers: `Authorization`, `x-amz-date`, `x-amz-security-token`, `x-amz-content-sha256`, `User-Agent`, `amz-sdk-invocation-id`, `amz-sdk-request`.

### Fixed
- **Flaky `TrackingComponentRegistryTests` when run in parallel**: Tests now use `Assert.Contains` instead of exact count assertions, preventing false failures when handler auto-registration from parallel test projects pollutes the static registry.

## [2.6.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.S3` package**: Track Amazon S3 operations in test diagrams. Includes:
  - `S3OperationClassifier` — Regex-based classifier that identifies S3 operations from HTTP requests, supporting both **path-style** (`s3.region.amazonaws.com/bucket/key`) and **virtual-hosted-style** (`bucket.s3.region.amazonaws.com/key`) URL formats. Classifies 20 distinct operations including PutObject, GetObject, DeleteObject, CopyObject, multipart uploads, tagging, and bucket management.
  - `S3TrackingMessageHandler` — `DelegatingHandler` that intercepts all S3 HTTP traffic, classifies operations, and logs request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `AmazonS3ConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonS3Config` that injects a tracking `HttpClientFactory` into the AWS SDK pipeline. Zero production code changes required.
  - Three verbosity levels: Raw (HTTP method + full URI), Detailed (classified operation name + `s3://bucket/key` URI), Summarised (operation name only, `s3://bucket/` URI, no content/headers, skips unrecognised operations).
  - Default excluded headers: `Authorization`, `x-amz-date`, `x-amz-security-token`, `x-amz-content-sha256`, `User-Agent`, `amz-sdk-invocation-id`, `amz-sdk-request`.

## [2.5.2] - 2026-04-21

### Changed
- **Increased PlantUML browser-rendering size limit by 50%**: The hosted `plantuml.js` CDN URL now points to a new patched build (`plantuml-js-plantuml_limit_size_98304`) that raises the maximum diagram pixel dimensions from 65,536px to 98,304px.

## [2.5.1] - 2026-04-21

### Fixed
- **EF Core extension now uses TFM-conditional package references**: `Microsoft.EntityFrameworkCore.Relational` is now referenced as `8.*` for net8.0, `9.*` for net9.0, and `10.*` for net10.0. Previously it unconditionally referenced `9.*`, which forced .NET 8 consumers to pull in EF Core 9 — a major version upgrade just to use the tracking package.

## [2.5.0] - 2026-04-21

### Added
- **HttpContext-based test identity for SQL tracking**: `SqlTrackingInterceptor` now accepts an optional `IHttpContextAccessor` and reads TTD's test identity headers (`test-tracking-current-test-name`, `test-tracking-current-test-id`) from the current `HttpContext` before falling back to `CurrentTestInfoFetcher`. This enables SQL tracking inside server-side HTTP request pipelines in `WebApplicationFactory`-based tests without custom fetcher wrappers.
- **Automatic `IHttpContextAccessor` resolution**: `AddSqlTestTracking(options)` now uses factory-based DI registration that auto-resolves `IHttpContextAccessor` when available. No code changes needed for existing consumers — SQL tracking in server-side pipelines works out of the box.

### Fixed
- **Exception safety in `SqlTrackingInterceptor`**: `CurrentTestInfoFetcher?.Invoke()` is now wrapped in a try-catch. An exception from a diagnostic fetcher delegate (e.g. `ScenarioExecutionContext.CurrentScenario` called outside a LightBDD scenario context) will never propagate into the EF Core command pipeline. If the fetcher throws and no `HttpContext` headers are available, the SQL command executes normally and is simply not logged.

## [2.4.1] - 2026-04-21

### Fixed
- **Corrected PostConfigure guidance for Duende IdentityServer**: The diagnostic report hint and wiki troubleshooting section previously recommended using `PostConfigure<ConfigurationStoreOptions>` to wire the SQL tracking interceptor into Duende's EF Core pipeline. This does not work because Duende registers its store options as a direct singleton (not via `IOptions<T>`) and captures the `ResolveDbContextOptions` delegate by value at service-registration time. The diagnostic hint now correctly advises adding `WithSqlTestTracking(sp)` inside the `ResolveDbContextOptions` implementation that runs at resolution time, and explicitly warns that `PostConfigure` does not work with Duende IdentityServer.
- **Fixed flaky ServiceBus test**: `Constructor_RegistersTrackerWithRegistry` failed intermittently due to xUnit parallel execution racing on the static `TrackingComponentRegistry`. Added `[Collection("TrackingComponentRegistry")]` to all test classes that share this static state.

## [2.4.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.ServiceBus` package**: Track Azure Service Bus messaging operations in test diagrams. Includes:
  - `ServiceBusTracker` — Central logging helper that logs send/receive/management operations as request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `TrackingServiceBusClient` — Wrapper around `ServiceBusClient` that creates tracked senders and receivers.
  - `TrackingServiceBusSender` — Wrapper around `ServiceBusSender` that intercepts `SendMessageAsync`, `SendMessagesAsync`, `ScheduleMessageAsync`, and `CancelScheduledMessageAsync`.
  - `TrackingServiceBusReceiver` — Wrapper around `ServiceBusReceiver` that intercepts `ReceiveMessageAsync`, `ReceiveMessagesAsync`, `PeekMessageAsync`, `CompleteMessageAsync`, `AbandonMessageAsync`, `DeadLetterMessageAsync`, `DeferMessageAsync`, and `RenewMessageLockAsync`.
  - `ServiceBusOperationClassifier` — Classifies Service Bus method calls into operations (Send, SendBatch, Receive, Complete, Abandon, DeadLetter, Defer, Schedule, etc.) with diagram labels.
  - `ServiceBusServiceCollectionExtensions.AddServiceBusTestTracking()` — DI extension that wraps existing `ServiceBusClient` registrations with tracking.
  - Three verbosity levels: Raw (enum names), Detailed (operation with queue/topic arrows), Summarised (simple operation names, no content).
  - Messaging operations (Send, Receive, Schedule, Peek) use `MetaType.Event` for blue async-messaging rendering in PlantUML diagrams.

## [2.3.2] - 2026-04-21

### Fixed
- **Failure cluster links now navigate correctly**: Clicking a scenario link in the failure clusters section of the HTML report now scrolls to the correct scenario. Previously, anchor IDs were generated from the test runtime ID instead of the scenario display name, causing a mismatch with the target element's ID.

## [2.3.1] - 2026-04-21

### Changed
- **TrackingComponentRegistry no longer throws**: Removed `ValidateAllComponentsWereInvoked()` and `ValidateComponentsWereInvoked<T>()` methods. Unused component detection is now fully passive — warnings appear in console output automatically and in the HTML diagnostic report when `DiagnosticMode=true`. TTD should never cause test failures in its default configuration.

## [2.3.0] - 2026-04-21

### Added
- **`ITrackingComponent` interface**: All tracking handlers and interceptors now implement `ITrackingComponent`, providing `ComponentName`, `WasInvoked`, and `InvocationCount` properties.
- **`TrackingComponentRegistry`**: Central static registry that auto-registers all tracking components on construction.
  - `GetUnusedComponents()` / `GetRegisteredComponents()` — Programmatic inspection of component state.
  - `Clear()` — Reset alongside `RequestResponseLogger.Clear()` in test setup.
- **Unused component warnings in diagnostic report**: `ReportDiagnostics.Analyse()` now warns when registered tracking components were never invoked — a strong indicator of misconfiguration (e.g. EF Core `DbContextOptions<T>` type mismatch). Warnings appear in console output automatically and in the HTML diagnostic report when `DiagnosticMode=true`. This never throws or fails tests.
- **Diagnostic report "Tracking Components" section**: When `DiagnosticMode=true`, the HTML diagnostic report now includes a table of all registered tracking components with their invocation counts and active/unused status, plus troubleshooting hints for common causes.
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
