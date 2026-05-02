# Agents

## Playwright E2E Test Rules

When writing or modifying Playwright end-to-end tests in `tests/TestTrackingDiagrams.Tests.EndToEnd/`:

- **No `{ force: true }` click bypass** — never use `ClickAsync(new() { Force = true })` or similar. If a click doesn't work, diagnose and fix the root cause (e.g., use JS `dispatchEvent` for SVG elements that intercept pointer events).
- **No network mocking** — do not mock network requests. Tests must use real page rendering with local HTML files.
- **Use `PollingInterval = 200`** on all `WaitForFunctionAsync` calls — the default `requestAnimationFrame`-based polling fails under parallel test execution load. Always specify `PollingInterval = 200` in the options.
- **SVG interactions** — use JS `dispatchEvent` for:
  - Context menu: `dispatchEvent(new MouseEvent('contextmenu', ...))` via `DispatchContextMenu()` helper
  - Note hover: `dispatchEvent(new MouseEvent('mouseenter', ...))` (not `mouseover`)
  - Note double-click: `dispatchEvent(new MouseEvent('dblclick', ...))` to avoid `<text>` element pointer interception
- **Strict mode** — always use `.First` or `.Nth(n)` when selectors may match multiple elements (per-diagram buttons, nested summaries, report+scenario level controls).
- **Search bar** — use `FillSearchBar()` helper which dispatches `keyup` event after `FillAsync` (required by `onkeyup="search_scenarios()"`).
