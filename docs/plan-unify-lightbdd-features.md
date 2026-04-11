# Plan: Unify LightBDD Report Features into Main TTD Reports

## Background

Currently there are **two separate report generation paths**:

1. **Main TTD path**: `ReportGenerator.CreateStandardReportsWithDiagrams(Feature[], ...)` — used by xUnit2, xUnit3, BDDfy, ReqNRoll, MSTest, NUnit4. Generates `FeaturesReport.html`.
2. **LightBDD path**: `CustomisableHtmlResultTextWriter` (in `LightBDD.Contrib.ReportingEnhancements`) — a fork/extension of LightBDD's own `HtmlResultTextWriter`. Generates its own HTML report via LightBDD's `IReportFormatter` pipeline.

The goal is to **eliminate path #2** and bring all of LightBDD's valuable report features into the main `ReportGenerator` so they're available to all frameworks, using generic naming/structure that doesn't leak LightBDD-specific concepts.

---

## Current State Analysis

### What LightBDD reports have that main TTD reports don't

| Feature | LightBDD | Main TTD | Notes |
|---------|----------|----------|-------|
| **BDD steps** (Given/When/Then) | ✅ Rendered per-scenario | ❌ Steps captured by BDDfy/ReqNRoll adapters but **discarded** during `ToFeatures()` | Core gap |
| **Composite step nesting** | ✅ Recursive substep rendering | ❌ | LightBDD composite steps |
| **Step-level status** | ✅ Per-step pass/fail/bypassed/ignored/notrun | ❌ Only scenario-level | |
| **Step-level duration** | ✅ Per-step timing | ❌ Only scenario-level | |
| **Parameter verification** | ✅ Inline params colored green/red | ❌ | LightBDD-specific (inline verification) |
| **Tabular data display** | ✅ Table rows with =, !, +, - indicators | ❌ | LightBDD + ReqNRoll (DataTable/Scenario Outline) |
| **Tree parameter display** | ✅ Hierarchical object trees | ❌ | LightBDD-specific |
| **Execution summary** | ✅ Overall status, start/end time, content stats | ✅ Has its own summary section | Different layout |
| **Feature summary table** | ✅ Sortable, 18 columns (scenarios/steps/duration breakdown) | ❌ No summary table | |
| **Category/label filtering** | ✅ Radio buttons per category | ❌ | |
| **Feature descriptions** | ✅ `[FeatureDescription("...")]` shown | ❌ Feature model has no description field | |
| **Scenario labels** | ✅ Rendered as badges | ❌ | |
| **Expand/collapse steps** | ✅ Checkbox-based toggle per scenario/step | ✅ Has expand/collapse all | Per-item toggle is different |
| **Comments** | ✅ Step comments shown | ❌ | Will be modeled (DD-15) |
| **File attachments** | ✅ Links to attached files | ❌ | Will be modeled (DD-15) |
| **Bypassed/Ignored statuses** | ✅ 5 statuses | ✅ 3 statuses (Passed/Failed/Skipped) | |

### What main TTD reports have that LightBDD reports don't

| Feature | Main TTD | LightBDD |
|---------|----------|----------|
| Dependency filter (by service) | ✅ | ❌ |
| Duration percentile filter | ✅ | ❌ |
| Activity diagrams + flame charts | ✅ | ✅ (via shared code) |
| Inline SVG rendering | ✅ | ✅ (via shared code) |
| Report size compression (gzip+base64) | ✅ | ✅ (via shared code) |
| Export HTML/CSV | ✅ | ❌ |
| Shareable URL hash state | ✅ | ✅ |
| Keyboard navigation (/, arrows) | ✅ | ❌ |
| Span count warning | ✅ | ❌ |

---

## Design Decisions

### DD-1: Extend the core `Scenario` and `Feature` models rather than creating parallel types

**Decision**: Add optional properties to the existing `Scenario` and `Feature` records.

**Rationale**: All framework adapters already convert to `Feature[]` → `Scenario[]`. Adding optional step/label/category data keeps the pipeline simple. Frameworks that don't have steps (xUnit, MSTest, NUnit) just leave them null/empty.

**Impact**: `TestTrackingDiagrams` core package changes. All adapter packages updated.

### DD-2: Use generic naming for step concepts — no "Given/When/Then" baked into the model

**Decision**: Steps are modeled as:
```csharp
public record ScenarioStep
{
    public string? Keyword { get; set; }     // "Given", "When", "Then", "And", "But", "Setup", "Step", null
    public required string Text { get; set; }
    public ScenarioResult? Status { get; set; }
    public TimeSpan? Duration { get; set; }
    public ScenarioStep[]? SubSteps { get; set; }
    public StepParameter[]? Parameters { get; set; }
    public string[]? Comments { get; set; }
    public FileAttachment[]? Attachments { get; set; }
}
```

**Rationale**: The keyword is just a display string. BDDfy uses "Given"/"When"/"Then", ReqNRoll uses the same, LightBDD uses the same. Plain xUnit/MSTest tests could use "Arrange"/"Act"/"Assert" or nothing at all. The model shouldn't assume BDD.

### DD-3: Keep parameter verification as a stretch goal — implement steps first

**Decision**: Phase 1 delivers steps with keyword/text/status/duration/nesting. Phase 2 adds parameter verification display (inline params, tabular data, tree params).

**Rationale**: Parameter verification is complex but benefits multiple frameworks — not just LightBDD. ReqNRoll has DataTable/Scenario Outline table arguments accessible via `StepContext.StepInfo.Table` (the hooks already have access but don't capture it). Steps alone cover the biggest gap and benefit all BDD frameworks. Parameter/table data can follow in Phase 3 and will benefit both LightBDD and ReqNRoll users.

### DD-4: Extend `ScenarioResult` enum to support additional statuses

**Decision**: Add `Bypassed` and `Ignored` to the enum (currently: `Passed`, `Failed`, `Skipped`).

**Rationale**: LightBDD has 5 statuses: Passed, Failed, Bypassed, Ignored, NotRun. The current "Skipped" can map to both Ignored and NotRun. Bypassed is a distinct concept (test passed but with a caveat). Adding these gives richer status reporting in the main report.

**Alternative considered**: Map everything to 3 statuses. Rejected because it loses information.

### DD-5: Add Feature-level metadata (description, labels/tags, categories)

**Decision**: Extend `Feature` with:
```csharp
public record Feature
{
    // existing
    public required string DisplayName { get; set; }
    public string? Endpoint { get; set; }
    public Scenario[] Scenarios { get; set; } = [];
    
    // new
    public string? Description { get; set; }
    public string[]? Labels { get; set; }
}
```

And `Scenario` with:
```csharp
public record Scenario
{
    // existing ...
    
    // new
    public ScenarioStep[]? Steps { get; set; }
    public string[]? Labels { get; set; }
    public string[]? Categories { get; set; }
}
```

**Rationale**: Description comes from `[FeatureDescription]` in LightBDD, from `Feature:` in ReqNRoll .feature files, from `[Story]` in BDDfy. Labels/categories are cross-cutting concepts used for filtering. Generic naming avoids framework coupling.

### DD-6: Steps rendered conditionally — only shown when populated

**Decision**: The HTML report renders steps only when `scenario.Steps` is non-null and non-empty. For xUnit/MSTest/NUnit scenarios with no steps, the report looks exactly as it does today.

**Rationale**: Backward compatible. No visual change for frameworks that don't capture steps.

### DD-7: Feature summary table is opt-in via ReportConfigurationOptions

**Decision**: Add `bool IncludeFeatureSummaryTable { get; set; } = false` to `ReportConfigurationOptions`. When true, a sortable summary table is rendered above the feature details (similar to LightBDD's current one but adapted to TTD's column set).

**Rationale**: Not all consumers want a summary table. The current compact layout works for smaller test suites.

### DD-8: Category filtering becomes a new filter type alongside existing filters

**Decision**: When any scenario has categories, a new "Categories" filter section appears in the filter toolbar (alongside search, status, happy-path, dependency filters). Uses radio buttons (one category at a time) with "All" and "Uncategorized" options.

**Rationale**: Consistent with LightBDD's UX. Radio buttons prevent confusion about combinatorial filtering.

### DD-9: ~~Retire LightBDD.Contrib.ReportingEnhancements~~ ✅ DONE

**Status**: Completed. `LightBDD.Contrib.ReportingEnhancements` has been retired and deleted.

**What was done**:
1. `TestTrackingDiagrams.LightBDD.xUnit3` and `.xUnit2` now convert LightBDD's `IFeatureResult[]` → `Feature[]` via `UnifiedReportFormatter` and `UnifiedYamlFormatter`
2. `ReportWritersConfigurationExtensions.CreateStandardReportsWithDiagrams()` uses the main `ReportGenerator` pipeline exclusively
3. `HappyPathAttribute` moved into both LightBDD adapter packages
4. `LightBDD.Contrib.ReportingEnhancements` project removed from solutions and deleted

### DD-10: LightBDD → Feature[] conversion captures all rich data

**Decision**: Create a comprehensive `IFeatureResult.ToFeatures()` extension method in `TestTrackingDiagrams.LightBDD.xUnit3` that maps:

| LightBDD | → | TTD |
|----------|---|-----|
| `IFeatureResult.Info.Name` | → | `Feature.DisplayName` |
| `IFeatureResult.Info.Description` | → | `Feature.Description` |
| `IFeatureResult.Info.Labels` | → | `Feature.Labels` |
| `IScenarioResult.Info.Name` | → | `Scenario.DisplayName` |
| `IScenarioResult.Info.Labels` | → | `Scenario.Labels` |
| `IScenarioResult.Info.Categories` | → | `Scenario.Categories` |
| `IScenarioResult.Status` | → | `Scenario.Result` |
| `IScenarioResult.StatusDetails` | → | `Scenario.ErrorMessage` |
| `IScenarioResult.ExecutionTime` | → | `Scenario.Duration` |
| `IStepResult` tree | → | `ScenarioStep[]` tree |
| `IStepResult.Comments` | → | `ScenarioStep.Comments` |
| `IStepResult.FileAttachments` | → | `ScenarioStep.Attachments` |
| `IStepResult.Parameters` | → | `ScenarioStep.Parameters` (Phase 3) |

### DD-11: BDDfy and ReqNRoll adapters updated to preserve steps

**Decision**: Fix the current data loss where steps are captured but discarded in `ToFeatures()`.

- **BDDfy**: `BDDfyScenarioInfo.Steps` (List<BDDfyStepInfo>) → `Scenario.Steps` (ScenarioStep[])
- **ReqNRoll**: `ReqNRollScenarioInfo.Steps` (List<ReqNRollStepInfo>) → `Scenario.Steps` (ScenarioStep[])

Both already capture `(Keyword, Text)` pairs. They just need to flow through.

### DD-12: Step rendering HTML structure

**Decision**: Steps rendered inside the scenario `<details>` block, between the summary and the diagrams section:

```html
<details class="scenario" ...>
  <summary class="h3">Scenario Name ...</summary>
  
  <!-- NEW: Steps section (only if steps exist) -->
  <div class="scenario-steps">
    <div class="step">
      <span class="step-status passed">✓</span>
      <span class="step-keyword">Given</span>
      <span class="step-text">a valid request body</span>
      <span class="step-duration">(0.5s)</span>
      <!-- Substeps (nested) -->
      <div class="sub-steps">
        <div class="step">...</div>
      </div>
    </div>
    <div class="step">
      <span class="step-keyword">When</span>
      <span class="step-text">the request is sent</span>
    </div>
  </div>
  
  <!-- Existing: Error details -->
  <!-- Existing: Diagrams section -->
</details>
```

**Rationale**: Steps provide the narrative context for the diagrams below them. Reading order: scenario name → steps → diagram → error details.

### DD-13: Configurable report branding (logo, title, favicon)

**Decision**: Add to `ReportConfigurationOptions`:
```csharp
public string? CustomLogoBase64 { get; set; }
public string? CustomLogoMimeType { get; set; }
public string? CustomFaviconBase64 { get; set; }
public string? CustomFaviconMimeType { get; set; }
```

**Rationale**: LightBDD.Contrib.ReportingEnhancements currently embeds the LightBDD logo. When we retire that package, consumers may want their own branding. This also makes it useful beyond LightBDD.

### DD-15: Comments and file attachments are modeled on ScenarioStep

**Decision**: Add to `ScenarioStep`:
```csharp
public string[]? Comments { get; set; }
public FileAttachment[]? Attachments { get; set; }
```

With:
```csharp
public record FileAttachment(string Name, string RelativePath);
```

**Rationale**: LightBDD supports step-level comments (`AddComment()`) and file attachments. These provide useful context in reports — comments explain intent or caveats, attachments link to screenshots, logs, or other artifacts. Rendering: comments as an italic block below the step text; attachments as download links.

**Phase**: Included in Phase 1 model definition. HTML rendering in Phase 1 (simple display). LightBDD adapter maps `IStepResult.Comments` and `IStepResult.FileAttachments` in Phase 1.

### DD-14: YAML specifications output includes steps

**Decision**: The YAML report (ComponentSpecifications.yml) includes step data when available:
```yaml
Features:
  - Feature: /cake
    Description: "Cake management endpoints"
    Scenarios:
      - Scenario: Creating a cake successfully
        IsHappyPath: true
        Steps:
          - Given: a valid request body
          - When: the request is sent to the cake endpoint
          - Then: the response should be successful
```

**Rationale**: The YAML spec is meant to be a human-readable specification. Steps are the specification.

---

## Implementation Phases

### Phase 1: Core Model Extensions + Step Rendering
**Scope**: Get steps displaying in the main report for all BDD frameworks.

1. **Extend core models** (`TestTrackingDiagrams/Reports/`)
   - Add `ScenarioStep` record (with `Comments` and `Attachments` properties)
   - Add `FileAttachment` record
   - Add `Steps`, `Labels`, `Categories` to `Scenario`
   - Add `Description`, `Labels` to `Feature`
   - Extend `ScenarioResult` enum with `Bypassed`, `Ignored`

2. **Update ReportGenerator HTML** (`TestTrackingDiagrams/Reports/ReportGenerator.cs`)
   - Add step rendering block inside scenario `<details>`
   - Add CSS for steps (keyword styling, status icons, indentation for nesting)
   - Render step comments as italic blocks below step text
   - Render file attachments as download links below step text
   - Conditional rendering: only when steps are populated

3. **Update framework adapters to flow steps through**
   - `TestTrackingDiagrams.BDDfy.xUnit3`: Map `BDDfyStepInfo` → `ScenarioStep` in `ToFeatures()`
   - `TestTrackingDiagrams.ReqNRoll.xUnit2/xUnit3`: Map `ReqNRollStepInfo` → `ScenarioStep` in `ToFeatures()`
   - `TestTrackingDiagrams.LightBDD.xUnit2/xUnit3`: Create new `IFeatureResult[].ToFeatures()` that maps the full LightBDD result tree into `Feature[]` with steps, description, labels, categories

4. **Update YAML generator** to include steps

5. **Update LightBDD adapter report lifecycle**
   - `TestTrackingDiagrams.LightBDD.xUnit3`: Add alternative report generation path that converts `IFeatureResult[]` → `Feature[]` → `ReportGenerator.CreateStandardReportsWithDiagrams()`
   - Keep existing `CustomisableHtmlReportFormatter` path working in parallel during transition

### Phase 2: Feature Summary Table + Category Filtering
**Scope**: Port the sortable summary table and category-based filtering.

6. **Feature summary table** in ReportGenerator
   - Sortable columns: Feature name, scenario counts by status, duration (overall/aggregated/average)
   - Step counts by status (when steps available)
   - JavaScript sortTable() function

7. **Category filter** in the filter toolbar
   - Extract categories from all scenarios
   - Radio button UI: All | Category1 | Category2 | ... | Uncategorized
   - JavaScript filtering by `data-categories` attribute
   - Integration with existing filter state persistence (localStorage + URL hash)

8. **Label/badge rendering**
   - Feature labels and scenario labels rendered as styled badges
   - Happy-path label already exists; generalize the pattern

### Phase 3: Parameter Verification + Tabular Data (Stretch)
**Scope**: Port the rich parameter verification and tabular data features. Benefits LightBDD (inline params, tables, trees) and ReqNRoll (DataTable, Scenario Outline tables).

9. **Step parameter model**
   ```csharp
   public record StepParameter
   {
       public required string Name { get; set; }
       public StepParameterKind Kind { get; set; } // Inline, Tabular, Tree
       public InlineParameterValue? InlineValue { get; set; }
       public TabularParameterValue? TabularValue { get; set; }
       public TreeParameterValue? TreeValue { get; set; }
   }
   
   public enum StepParameterKind { Inline, Tabular, Tree }
   
   public record InlineParameterValue(string Value, string? Expectation, VerificationStatus Status);
   
   public record TabularParameterValue(TabularColumn[] Columns, TabularRow[] Rows);
   public record TabularColumn(string Name, bool IsKey);
   public record TabularRow(TableRowType Type, TabularCell[] Values);
   public record TabularCell(string Value, string? Expectation, VerificationStatus Status);
   
   public enum VerificationStatus { NotApplicable, Success, Failure, Exception, NotProvided }
   public enum TableRowType { Matching, Surplus, Missing }
   
   public record TreeParameterValue(TreeNode Root);
   public record TreeNode(string Path, string Node, string Value, string? Expectation, VerificationStatus Status, TreeNode[]? Children);
   ```

10. **HTML rendering for parameters**
    - Inline: colored background (green=success, red=failure, yellow=not provided)
    - Tabular: HTML table with row type indicators (=, !, +, -)
    - Tree: hierarchical display with per-node status

11. **LightBDD adapter maps full parameter data** from `IParameterResult` → `StepParameter`

12. **ReqNRoll adapter captures table arguments**
    - `[AfterStep]` hook reads `StepContext.StepInfo.Table` (already has access but doesn't capture it)
    - Map ReqNRoll `DataTable` → `StepParameter` with `Kind = Tabular`
    - ReqNRoll tables are input-only (no verification status), so `VerificationStatus = NotApplicable` for all cells
    - Update `ReqNRollStepInfo` to include optional `Table` property

### Phase 4: Retirement of LightBDD.Contrib.ReportingEnhancements ✅ DONE

12. ~~**Remove dual-path**: Delete `LightBDD.Contrib.ReportingEnhancements` project entirely~~ ✅
13. ~~**Update LightBDD examples**: Point to main report path only~~ ✅
14. **Branding**: Add logo/favicon customization to `ReportConfigurationOptions`
15. ~~**Documentation**: Update wiki and integration guides~~ ✅

---

## File Changes Summary

### Phase 1 files to modify/create:

| File | Action | What |
|------|--------|------|
| `TestTrackingDiagrams/Reports/Scenario.cs` | Modify | Add Steps, Labels, Categories properties |
| `TestTrackingDiagrams/Reports/Feature.cs` | Modify | Add Description, Labels properties |
| `TestTrackingDiagrams/Reports/ScenarioResult.cs` | Modify | Add Bypassed, Ignored enum values |
| `TestTrackingDiagrams/Reports/ScenarioStep.cs` | Create | New record for step data (incl. Comments, Attachments) |
| `TestTrackingDiagrams/Reports/FileAttachment.cs` | Create | New record for file attachment |
| `TestTrackingDiagrams/Reports/ReportGenerator.cs` | Modify | Add step rendering HTML + CSS |
| `TestTrackingDiagrams/Reports/YamlReportGenerator.cs` | Modify | Include steps in YAML output |
| `TestTrackingDiagrams.BDDfy.xUnit3/ScenarioInfoExtensions.cs` | Modify | Map BDDfyStepInfo → ScenarioStep |
| `TestTrackingDiagrams.ReqNRoll.xUnit2/ScenarioInfoEnumerableExtensions.cs` | Modify | Map ReqNRollStepInfo → ScenarioStep |
| `TestTrackingDiagrams.ReqNRoll.xUnit3/ScenarioInfoEnumerableExtensions.cs` | Modify | Map ReqNRollStepInfo → ScenarioStep |
| `TestTrackingDiagrams.LightBDD.xUnit3/FeatureResultExtensions.cs` | Create | IFeatureResult[] → Feature[] conversion |
| `TestTrackingDiagrams.LightBDD.xUnit3/LightBddReportGenerator.cs` | Create/Modify | Wire into main ReportGenerator |
| `TestTrackingDiagrams.LightBDD.xUnit2/` (same as xUnit3) | Modify | Same changes |

### Phase 2 files:

| File | Action | What |
|------|--------|------|
| `TestTrackingDiagrams/Reports/ReportGenerator.cs` | Modify | Summary table + category filter HTML/JS |

### Phase 3 files:

| File | Action | What |
|------|--------|------|
| `TestTrackingDiagrams/Reports/StepParameter.cs` | Create | Parameter models |
| `TestTrackingDiagrams/Reports/ReportGenerator.cs` | Modify | Parameter rendering HTML/CSS |
| `TestTrackingDiagrams.LightBDD.xUnit3/FeatureResultExtensions.cs` | Modify | Map IParameterResult → StepParameter |
| `TestTrackingDiagrams.ReqNRoll.xUnit3/ReqNRollStepInfo.cs` | Modify | Add optional Table property |
| `TestTrackingDiagrams.ReqNRoll.xUnit3/ReqNRollTrackingHooks.cs` | Modify | Capture StepContext.StepInfo.Table in AfterStep |
| `TestTrackingDiagrams.ReqNRoll.xUnit3/ScenarioInfoEnumerableExtensions.cs` | Modify | Map table data → StepParameter |
| `TestTrackingDiagrams.ReqNRoll.xUnit2/` (same) | Modify | Same changes as xUnit3 |

### Phase 4 files:

| File | Action | What |
|------|--------|------|
| `LightBDD.Contrib.ReportingEnhancements/` | Delete | Entire project |
| `TestTrackingDiagrams/Reports/ReportConfigurationOptions.cs` | Modify | Add branding options |

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| LightBDD report parity — users may notice missing features during transition | Consumer impact | Keep both paths working in parallel until Phase 4; feature-flag the new path |
| Step rendering increases report HTML size | Performance | Steps are inside collapsed `<details>` — DOM exists but hidden. Compress if needed (same gzip pattern) |
| Parameter verification model complexity | Dev effort | Phase 3 is explicitly deferred and scoped only to LightBDD adapter initially |
| ScenarioResult enum change is breaking | Binary compat | Existing Passed=0, Failed=1, Skipped=2 values stay the same; Bypassed=3, Ignored=4 are additive |
| LightBDD version coupling | Maintenance | The LightBDD adapter already depends on LightBDD packages; no new coupling introduced |
| BDDfy step data is limited (no status/duration per step) | Feature parity | BDDfy only provides keyword+text; status and duration will be null for BDDfy steps |
| ReqNRoll step data may lack per-step status | Feature parity | Check what ReqNRoll exposes; capture what's available |
| ReqNRoll DataTable is input-only (no verification) | Different semantics from LightBDD tables | Render as plain table with all cells NotApplicable; no =,!,+,- row indicators |

---

## Open Questions

1. **Should the feature summary table replace or supplement the existing execution summary section?** The current TTD summary section has different metrics (dependency-oriented). Recommendation: keep both; summary table is a new section between summary and features.

2. **Should steps be collapsed by default?** LightBDD has a `StepsHiddenInitially` option. Recommendation: steps collapsed by default, with expand/collapse all toggle.

3. **Should we support LightBDD's "Bypassed" status?** Bypassed means "test technically passed but with a note". Recommendation: yes, as it enables richer status reporting. Map it to a blue badge.

4. **How should the LightBDD adapter's report lifecycle work without `ReportWritersConfiguration`?** Currently LightBDD hooks report generation into its own lifecycle via `IReportFormatter`. Options:
   - a) Keep using LightBDD's lifecycle but point it at TTD's `ReportGenerator` instead of the custom formatter
   - b) Use TTD's disposal/assembly-unload lifecycle (like xUnit3 does via `DiagrammedTestRun`)
   
   Recommendation: (a) — less disruptive for LightBDD users, and it lets LightBDD pass us its `IFeatureResult[]` at the right time.

5. ~~Should comments and file attachments be modeled?~~ **Yes.** Comments and file attachments will be modeled on `ScenarioStep`. See DD-15.
