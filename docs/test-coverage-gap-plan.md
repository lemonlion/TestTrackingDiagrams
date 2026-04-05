# Test Coverage Gap Plan

Created: 2026-04-05 after deep analysis of all source projects vs test projects.

## Completed

- [x] **InternalFlowSegmentBuilder** (Medium-High) — 14 tests: time windows, trace ID filtering, boundary conditions, multi-test grouping, span ordering
- [x] **YamlExtensions** (Medium) — 13 tests + fix: sanitization for `#`, `&`, `*`, `{`, `}`, `!`, `%`, `@`, `` ` ``, `|` in both core and LightBDD.Contrib copies
- [x] **InternalFlowRenderer** — 14 tests: BuildSpanTree, activity diagram, call tree, flame chart, duplicate SpanId
- [x] **Selenium UI tests** — 15 tests: popup open/close, toggle switching, flame chart bars, call tree, no-data messages

## Priority 1: Medium risk — silent failures

- [ ] **InternalFlowSpanCollector** — Characterization test for reflection-based `Type.GetType` + `GetSpans()` call. Test 3 granularity filters (Auto/Manual/Full). Test graceful empty result when OTel assembly absent.
- [ ] **PlantUmlTextEncoder** — Known encoding test vectors. Round-trip verification. Edge cases: empty, long, Unicode.
- [ ] **TestTrackingSpanStore** — Add/GetSpans/Clear lifecycle. Concurrent writes. Document unbounded growth.

## Priority 2: Low risk — behavior documentation

- [ ] **InternalFlowHtmlGenerator** — Valid `<script>` output, `data-view` attributes, FlameChart position variants.
- [ ] **StringExtensions** — `ChunksUpTo`, `StringJoin`, `TrimEnd(string)` edge cases.
- [ ] **HttpRequestExtensions.GetUri()** — Missing scheme/host, comma-separated hosts, PathBase.
- [ ] **DefaultTrackingDiagramOverride** — `StartOverride`/`EndOverride` flags, `InsertPlantUml` content, `InsertTestDelimiter` marker.

## Priority 3: Framework integration projects (structural debt)

| Project | Current Coverage | Gap |
|---|---|---|
| NUnit4 | 0% (no test project) | 11 untested classes |
| BDDfy.xUnit3 | 0% (no test project) | 15 untested classes |
| ReqNRoll.xUnit2 | 0% (no test project) | 15 untested classes |
| ReqNRoll.xUnit3 | 0% (no test project) | 15 untested classes |
| xUnit2 | 6% | 15 untested classes (only DisplayNameFormatter tested) |
| xUnit3 | 8% | 11 untested classes (only DisplayNameFormatter tested) |
| LightBDD.xUnit2 | 0% | 6 untested classes (xUnit3 version is 83%) |
| LightBDD.Contrib | 0% (no test project) | ~20 untested classes |

## Priority 4: Trivial / self-revealing

- [ ] ServiceCollectionHelper, TestTrackingHttpClientFactory, OpenTelemetryTrackingExtensions, Stylesheets
