# Advanced Search Bar — Design Plan

## 1. Problem Statement

The current search bar supports:
- **Text search**: space-separated words (implicit AND), quoted phrases (`"exact phrase"`)
- **Tag expressions**: `@tag1 and @tag2 or not @tag3` with `(` `)` grouping

These two modes are **separate** — `@tag` tokens are stripped out and evaluated independently from text tokens. There is **no way to combine text search terms with boolean logic**. Users cannot express queries like:

- "Show me scenarios that mention `chocolate` OR `vanilla`"
- "Show me scenarios about `payment` but NOT `refund`"
- "Show me scenarios matching `(login OR signup) AND error`"
- "Show me scenarios matching text `timeout` OR tagged `@flaky`"

## 2. Goals

- Allow `(`, `)`, `&&`, `||`, `!!` (or chosen syntax) to combine text search terms with boolean logic.
- Allow mixing text search and `@tag` expressions in a single unified query.
- Preserve current behaviour: undecorated space-separated words still use implicit AND.
- Choose syntax that is **highly unlikely to conflict** with natural text that appears in test names, step definitions, or PlantUML code.
- Extensive unit test coverage of the parsing/evaluation algorithm, **testing the real JavaScript** via Jint rather than a C# mirror.

## 3. Syntax Design Decisions

### 3.1 Operator Syntax Choice

| Concern | Option A: Keywords | Option B: Symbolic | Option C: Prefixed Symbolic (Chosen) |
|---|---|---|---|
| AND | `AND` | `&&` | `&&` |
| OR | `OR` | `||` | `||` |
| NOT | `NOT` | `!` | `!!` |
| Grouping | `(` `)` | `(` `)` | `(` `)` |
| Conflict risk | HIGH — `and`, `or`, `not` appear in English prose and step definitions constantly | MEDIUM — `&&` and `||` could appear in code snippets in PlantUML | **LOW** — see rationale below |

**Decision: Option C — Prefixed Symbolic**

Rationale:
- `&&` and `||` are **extremely rare** in test scenario names, Gherkin steps, or PlantUML sequence diagrams (these are natural-language-heavy contexts, not code).
- `!!` (double-bang for NOT) is essentially never used in natural language or PlantUML, and distinguishes from a single `!` which might appear in exclamatory text.
- `(` `)` for grouping are acceptable — while parentheses appear in prose, they would only be ambiguous if adjacent to `&&`/`||`/`!!`, which is vanishingly unlikely in test content.
- Keywords like `and`/`or`/`not` are FAR too common in test names (`"User can log in and see dashboard"`, `"Order is not placed when stock is empty"`).
- The `@tag` system already uses keywords (`and`, `or`, `not`) but this works because `@`-prefixed tokens are unambiguous. For free-text search, keywords would be disastrous.

### 3.2 Implicit AND (Backward Compatibility)

**Decision**: Space-separated bare words without any operators continue to use implicit AND, exactly as today.

| Input | Interpretation |
|---|---|
| `chocolate cake` | `chocolate AND cake` (unchanged from today) |
| `chocolate && cake` | `chocolate AND cake` (explicit) |
| `chocolate \|\| cake` | `chocolate OR cake` (new) |
| `!!chocolate` | `NOT chocolate` (new) |

**Rule**: If the input contains **no** `&&`, `||`, `!!`, or `(` `)` tokens, the entire input is parsed using the legacy tokeniser (space-split + quoted phrases, implicit AND). This guarantees 100% backward compatibility.

### 3.3 Quoted Phrases in Boolean Expressions

**Decision**: Quoted phrases work as operands within boolean expressions.

| Input | Interpretation |
|---|---|
| `"chocolate cake" \|\| vanilla` | phrase "chocolate cake" OR word "vanilla" |
| `"error message" && !!timeout` | phrase "error message" AND NOT "timeout" |

### 3.4 Unified Tag + Text Expressions

**Decision**: `@tag` references become first-class operands in the boolean expression grammar, alongside text terms.

| Input | Interpretation |
|---|---|
| `@smoke && timeout` | tagged "smoke" AND text matches "timeout" |
| `@smoke \|\| @regression` | tagged "smoke" OR tagged "regression" |
| `(@smoke \|\| @regression) && !!@slow` | (tagged "smoke" OR tagged "regression") AND NOT tagged "slow" |
| `error && @critical` | text contains "error" AND tagged "critical" |

This **replaces** the current dual-path approach (strip `@tag` parts, evaluate separately). The tag expression parser and text search are unified into a single expression tree.

### 3.5 Status Filter Syntax

**Decision**: Add `$status` syntax for filtering by test result status within the search expression.

| Input | Interpretation |
|---|---|
| `$failed` | status is "Failed" |
| `$passed && timeout` | status is "Passed" AND text matches "timeout" |
| `$failed \|\| $skipped` | status is "Failed" OR "Skipped" |

The `$` prefix is extremely unlikely to conflict with test content and provides a natural extension point.

### 3.6 Operator Precedence

**Decision**: Standard boolean precedence: `!!` (NOT) binds tightest, then `&&` (AND), then `||` (OR). Parentheses override.

| Expression | Evaluated As |
|---|---|
| `a \|\| b && c` | `a \|\| (b && c)` |
| `!!a && b` | `(!!a) && b` |
| `(a \|\| b) && c` | `(a \|\| b) && c` |

### 3.7 Error Handling for Malformed Expressions

**Decision**: If the expression cannot be parsed (mismatched parens, dangling operators), **fall back to legacy behaviour** — treat the entire input as a plain text search with implicit AND. This ensures the search bar never "breaks" for the user.

Optionally, a subtle visual indicator (e.g., orange border on the search bar) could indicate "parsed as plain text" when operators are present but parsing failed.

## 4. Grammar Specification

```
Expression     → OrExpr
OrExpr         → AndExpr ( '||' AndExpr )*
AndExpr        → NotExpr ( '&&' NotExpr )*
NotExpr        → '!!' Primary | Primary
Primary        → '(' Expression ')'
               | '"' <phrase> '"'       -- quoted text phrase
               | '@' <identifier>       -- tag match
               | '$' <identifier>       -- status match
               | <word>                 -- bare text word (substring match)
```

Tokeniser rules:
- `&&`, `||`, `!!` are operator tokens (must be surrounded by whitespace or adjacent to `(` `)`)
- `(`, `)` are grouping tokens
- `"..."` is a quoted phrase token
- `@word` is a tag token
- `$word` is a status token
- Anything else is a bare word token

## 5. Architecture

### 5.1 JavaScript-First with Jint-Based Testing

**Key insight**: The previous plan used a C# mirror of the JS logic for unit testing. This is fragile — any subtle difference between the C# port and the real JS means tests pass but the report is broken. Instead, we test the **actual JavaScript** using [Jint](https://github.com/nicholasgasior/jint) (a .NET JavaScript interpreter).

The architecture has three layers:

1. **Pure JavaScript functions** (`advanced-search.js`) — Tokeniser, parser, and evaluator as pure functions that take simple inputs (strings, arrays, objects) and return results. **No DOM access.** This is the single source of truth.
2. **DOM glue in `ReportGenerator.cs`** — The `run_search_scenarios()` function calls the pure JS functions and wires results to DOM visibility. This thin layer is covered by Selenium tests.
3. **Unit tests via Jint** — A dedicated test project loads the real `.js` file into a Jint engine and calls the pure functions directly. No C# mirror. No risk of drift.

### 5.2 JavaScript Module Structure

The search engine JS is extracted from the inline C# string literals into a standalone file:

**`TestTrackingDiagrams/Reports/advanced-search.js`** (embedded resource):
```javascript
// --- Tokeniser ---
function advancedSearchTokenise(input) { ... }
// Returns: [{ type: 'text'|'phrase'|'tag'|'status'|'and'|'or'|'not'|'lparen'|'rparen', value: string }]

// --- Parser ---
function advancedSearchParse(tokens) { ... }
// Returns: AST node tree, e.g. { type: 'and', left: {...}, right: {...} }
// Returns null on parse error.

// --- Evaluator ---
function advancedSearchEvaluate(ast, searchText, tags, status) { ... }
// Returns: boolean

// --- Legacy detection ---
function isAdvancedSearch(input) { ... }
// Returns: boolean — true if input contains &&, ||, or !!

// --- Convenience entry point ---
function advancedSearchMatch(input, searchText, tags, status) { ... }
// Tokenise → parse → evaluate. Returns boolean.
// On parse error, returns null (caller falls back to legacy).
```

All functions are pure: no `document`, no `window`, no `console`. They operate only on their arguments.

### 5.3 Embedded Resource Strategy

The project already embeds `plantuml-render.js` as an `<EmbeddedResource>`. We follow the same pattern:

```xml
<!-- TestTrackingDiagrams.csproj -->
<EmbeddedResource Include="Reports\advanced-search.js" />
```

**`ReportGenerator.cs`** loads the embedded JS at report generation time and injects it into the `<script>` block:
```csharp
var advancedSearchJs = LoadEmbeddedResource("advanced-search.js");
// Embed directly in <script> tag alongside existing search functions
```

**Test project** loads the same embedded JS via assembly resource stream → feeds it into Jint. Single source of truth, zero duplication.

### 5.4 Integration Point

In `run_search_scenarios()` (JavaScript, still in ReportGenerator.cs as DOM glue):

```
Current flow:
  1. Extract @tag parts → evaluate separately
  2. Parse remaining text → implicit AND token matching
  3. Combine: scenario visible if textMatch AND tagMatch

New flow:
  1. Check: isAdvancedSearch(input)?
  2. If YES → result = advancedSearchMatch(input, item.searchText, tagSet, item.status)
     - If result === null (parse error) → fall back to legacy
     - Else use result as match boolean
  3. If NO → use legacy path (100% backward compatible)
```

### 5.5 Test Project Structure

```
TestTrackingDiagrams.Tests.SearchEngine/
├── TestTrackingDiagrams.Tests.SearchEngine.csproj    ← references Jint + xunit
├── JintTestBase.cs                                    ← loads advanced-search.js into Jint engine
├── TokeniserTests.cs                                  ← calls advancedSearchTokenise() via Jint
├── ParserTests.cs                                     ← calls advancedSearchParse() via Jint
├── EvaluatorTests.cs                                  ← calls advancedSearchEvaluate() via Jint
├── IntegrationTests.cs                                ← calls advancedSearchMatch() end-to-end via Jint
├── BackwardCompatibilityTests.cs                      ← also tests legacy functions via Jint
└── EdgeCaseTests.cs                                   ← fuzz/boundary tests via Jint
```

**`TestTrackingDiagrams.Tests.SearchEngine.csproj`**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Jint" Version="4.*" />
    <PackageReference Include="xunit.v3" Version="3.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\TestTrackingDiagrams\TestTrackingDiagrams.csproj" />
  </ItemGroup>
</Project>
```

### 5.6 JintTestBase Pattern

```csharp
public abstract class JintTestBase : IDisposable
{
    protected Engine JsEngine { get; }

    protected JintTestBase()
    {
        JsEngine = new Engine();

        // Load the real advanced-search.js from the embedded resource
        var assembly = typeof(ReportGenerator).Assembly;
        using var stream = assembly.GetManifestResourceStream("...");
        using var reader = new StreamReader(stream!);
        var js = reader.ReadToEnd();
        JsEngine.Execute(js);
    }

    // Helper: call a JS function and get the result
    protected T CallJs<T>(string functionName, params object[] args) { ... }

    // Helper: parse the Jint return value (JS object/array) into C# types
    protected List<TokenResult> CallTokenise(string input) { ... }
    protected AstNode? CallParse(string input) { ... }
    protected bool CallEvaluate(string input, string searchText, string[] tags, string status) { ... }

    public void Dispose() => JsEngine.Dispose();
}
```

### 5.7 File Changes

| File | Change |
|---|---|
| `TestTrackingDiagrams/Reports/advanced-search.js` | **New** — Pure JS: tokeniser, parser, evaluator |
| `TestTrackingDiagrams/TestTrackingDiagrams.csproj` | **Modified** — Add `<EmbeddedResource>` for `advanced-search.js` |
| `TestTrackingDiagrams/Reports/ReportGenerator.cs` | **Modified** — Load embedded JS, update `run_search_scenarios()` DOM glue |
| `TestTrackingDiagrams.Tests.SearchEngine/` (new project) | **New** — Entire test project |
| `TestTrackingDiagrams.Tests.SearchEngine/JintTestBase.cs` | **New** — Base class loading JS into Jint |
| `TestTrackingDiagrams.Tests.SearchEngine/TokeniserTests.cs` | **New** — Tests `advancedSearchTokenise()` |
| `TestTrackingDiagrams.Tests.SearchEngine/ParserTests.cs` | **New** — Tests `advancedSearchParse()` |
| `TestTrackingDiagrams.Tests.SearchEngine/EvaluatorTests.cs` | **New** — Tests `advancedSearchEvaluate()` |
| `TestTrackingDiagrams.Tests.SearchEngine/IntegrationTests.cs` | **New** — End-to-end `advancedSearchMatch()` |
| `TestTrackingDiagrams.Tests.SearchEngine/BackwardCompatibilityTests.cs` | **New** — Legacy behaviour preserved |
| `TestTrackingDiagrams.Tests.SearchEngine/EdgeCaseTests.cs` | **New** — Boundary/fuzz tests |
| `TestTrackingDiagrams.sln` | **Modified** — Add new test project |

### 5.8 What About the Existing C# SearchFunction.cs?

The existing `SearchFunction.cs` and `SearchFunctionTests.cs` test the **legacy** search behaviour (implicit AND, quoted phrases, DOM manipulation via AngleSharp). These remain unchanged — they continue to test the legacy code path. The new Jint-tested JS is the advanced code path. Over time, the legacy path may be migrated into the same JS file and tested via Jint too, but that's out of scope for this change.

## 6. Unit Test Plan

All tests below call the **real JavaScript functions** via Jint. No C# reimplementation.

### 6.1 Tokeniser Tests (`TokeniserTests.cs`)

Each test calls `advancedSearchTokenise(input)` via Jint and asserts the returned token array.

#### Basic tokenisation
- Empty string → empty token list
- Single word → `[{ type: 'text', value: 'word' }]`
- Multiple words → `[text("a"), text("b"), text("c")]`
- Quoted phrase → `[{ type: 'phrase', value: 'exact phrase' }]`
- Mixed quotes and words → correct token sequence
- `@tag` → `[{ type: 'tag', value: 'tag' }]`
- `$failed` → `[{ type: 'status', value: 'failed' }]`
- `&&` → `[{ type: 'and' }]`
- `||` → `[{ type: 'or' }]`
- `!!` → `[{ type: 'not' }]`
- `(` → `[{ type: 'lparen' }]`, `)` → `[{ type: 'rparen' }]`

#### Compound tokenisation
- `a && b` → `[text("a"), and, text("b")]`
- `a || b` → `[text("a"), or, text("b")]`
- `!!a` → `[not, text("a")]`
- `(a || b) && c` → `[lparen, text("a"), or, text("b"), rparen, and, text("c")]`
- `"hello world" && @smoke` → `[phrase("hello world"), and, tag("smoke")]`
- `$failed || $skipped` → `[status("failed"), or, status("skipped")]`

#### Whitespace handling
- Leading/trailing whitespace ignored
- Multiple spaces between tokens collapsed
- Tabs and mixed whitespace handled

#### Edge cases
- `&&` at the start of input (dangling operator)
- `||` at the end of input (dangling operator)
- `!!` with nothing after it
- Empty quoted phrase `""`
- Unclosed quote `"hello`
- Nested quotes (not supported — treat as text)
- `@` alone (no tag name)
- `$` alone (no status name)
- `!!@tag` → `[not, tag("tag")]`
- `!!$failed` → `[not, status("failed")]`
- Single `!` (not an operator — treated as part of a word)
- Single `&` (not an operator — treated as part of a word)
- Single `|` (not an operator — treated as part of a word)

### 6.2 Parser Tests (`ParserTests.cs`)

Each test calls `advancedSearchParse(tokens)` via Jint and asserts the returned AST structure.

#### Simple expressions
- `word` → `{ type: 'text', value: 'word' }`
- `"phrase"` → `{ type: 'phrase', value: 'phrase' }`
- `@tag` → `{ type: 'tag', value: 'tag' }`
- `$failed` → `{ type: 'status', value: 'failed' }`

#### Binary operators
- `a && b` → `{ type: 'and', left: text("a"), right: text("b") }`
- `a || b` → `{ type: 'or', left: text("a"), right: text("b") }`

#### NOT operator
- `!!a` → `{ type: 'not', operand: text("a") }`
- `!!@tag` → `{ type: 'not', operand: tag("tag") }`
- `!!$failed` → `{ type: 'not', operand: status("failed") }`

#### Precedence
- `a || b && c` → `{ type: 'or', left: text("a"), right: { type: 'and', left: text("b"), right: text("c") } }`
- `a && b || c` → `{ type: 'or', left: { type: 'and', left: text("a"), right: text("b") }, right: text("c") }`
- `!!a && b` → `{ type: 'and', left: { type: 'not', operand: text("a") }, right: text("b") }`
- `!!a || b` → `{ type: 'or', left: { type: 'not', operand: text("a") }, right: text("b") }`

#### Grouping
- `(a || b)` → `{ type: 'or', left: text("a"), right: text("b") }`
- `(a || b) && c` → `{ type: 'and', left: { type: 'or', ... }, right: text("c") }`
- `a && (b || c)` → `{ type: 'and', left: text("a"), right: { type: 'or', ... } }`
- `(a && b) || (c && d)` → `{ type: 'or', left: { type: 'and', ... }, right: { type: 'and', ... } }`

#### Nested grouping
- `((a || b))` → same as `(a || b)`
- `(a || (b && c))` → `{ type: 'or', left: text("a"), right: { type: 'and', ... } }`

#### Complex mixed expressions
- `@smoke && "error message" || timeout` → correct precedence tree
- `($failed || $skipped) && @regression` → correct grouping
- `!!@slow && (@smoke || @regression) && timeout` → three-way AND with NOT

#### Error recovery (parser returns null)
- Mismatched `(` → returns null
- Mismatched `)` → returns null
- Dangling `&&` at end → returns null
- Dangling `||` at start → returns null
- `&&` `||` adjacent with no operand between → returns null

### 6.3 Evaluator Tests (`EvaluatorTests.cs`)

Each test builds an AST (either manually or via `advancedSearchParse`), then calls `advancedSearchEvaluate(ast, searchText, tags, status)` via Jint.

#### Text matching
- Text "error" matches search text containing "error" → true
- Text "error" against text without "error" → false
- Case insensitive: "error" matches "ERROR in service" → true
- Phrase "error message" matches "the error message was" → true
- Phrase "error message" does NOT match "error and message" → false

#### Tag matching
- Tag "smoke" with tags `["smoke", "api"]` → true
- Tag "smoke" with tags `["regression"]` → false
- Tag "smoke" with empty tags → false
- Case insensitive tag matching

#### Status matching
- Status "failed" with status "Failed" → true (case insensitive)
- Status "failed" with status "Passed" → false
- Status "passed" with status "Passed" → true

#### AND expressions
- Both sides true → true
- Left false → false
- Right false → false
- Both false → false

#### OR expressions
- Both true → true
- Left true, right false → true
- Left false, right true → true
- Both false → false

#### NOT expressions
- NOT true → false
- NOT false → true

#### Complex evaluation
- `(text("error") || tag("smoke")) && !!status("passed")` — with matching text, no tag, failed status → true
- Same with passing status → false
- `text("login") && text("success") && tag("happy-path")` — all must match

### 6.4 Integration Tests (`IntegrationTests.cs`)

End-to-end: call `advancedSearchMatch(input, searchText, tags, status)` via Jint. One JS call per assertion.

#### Scenarios dataset for integration tests
Define a set of test scenarios with known attributes:

| Scenario | Search Text | Tags | Status |
|---|---|---|---|
| S1 | "user login success" | smoke, happy-path | Passed |
| S2 | "user login failure invalid password" | smoke, regression | Failed |
| S3 | "payment timeout error" | critical, regression | Failed |
| S4 | "order placed successfully" | happy-path | Passed |
| S5 | "admin dashboard loads" | smoke | Passed |
| S6 | "refund processed" | critical | Skipped |

Tests:
- `login` → matches S1, S2
- `login && success` → matches S1 only
- `login || payment` → matches S1, S2, S3
- `!!login` → matches S3, S4, S5, S6
- `login && !!success` → matches S2 only
- `@smoke` → matches S1, S2, S5
- `@smoke && $failed` → matches S2 only
- `@smoke || @critical` → matches S1, S2, S3, S5, S6
- `(@smoke || @critical) && $failed` → matches S2, S3
- `"invalid password"` → matches S2 only
- `"invalid password" || timeout` → matches S2, S3
- `!!@smoke && $passed` → matches S4
- `(login || payment) && $failed` → matches S2, S3
- `error && @critical && $failed` → matches S3
- `@happy-path && !!$passed` → matches nothing (S4 is passed, S1 is passed)
- Empty string → matches all (returns null → caller treats as "no filter")
- Only whitespace → matches all

### 6.5 Backward Compatibility Tests (`BackwardCompatibilityTests.cs`)

These tests load **both** the legacy JS functions (existing `parseSearchTokensIncludingQuotes`, `evaluateTagExpression`) **and** the new `isAdvancedSearch` + `advancedSearchMatch` into the same Jint engine, then verify the dispatch logic works:

- `chocolate` → `isAdvancedSearch` returns false → legacy path used
- `chocolate cake` → legacy (implicit AND of two words)
- `"chocolate cake"` → legacy (quoted phrase)
- `"chocolate cake" vanilla` → legacy (phrase + word, implicit AND)
- `@smoke` → legacy (tag match via evaluateTagExpression)
- `@smoke and @regression` → legacy (tag AND using keywords)
- `@smoke or @regression` → legacy (tag OR using keywords)
- `not @slow` → legacy (tag NOT using keyword)
- `(@smoke or @regression) and not @slow` → legacy
- Mixed: `@smoke error` → legacy (tag filter + text search)

And verify that when `&&`, `||`, or `!!` are present, `isAdvancedSearch` returns true:
- `a && b` → advanced
- `a || b` → advanced
- `!!a` → advanced
- `@smoke && @regression` → advanced (uses `&&` instead of keyword `and`)

**Decision point**: the existing keyword-based `and`/`or`/`not` syntax for `@tag` expressions should continue to work. The new `&&`/`||`/`!!` syntax is additive. If the input contains no `&&`/`||`/`!!` tokens, legacy parsing is used verbatim.

### 6.6 Edge Case / Fuzz Tests (`EdgeCaseTests.cs`)

- Very long input (1000+ characters)
- Deeply nested parentheses `((((a))))`
- Many OR branches `a || b || c || d || e || f || g || h`
- Many AND terms `a && b && c && d && e`
- Mixed implicit AND and explicit operators — In advanced mode, adjacent bare words (not separated by an operator) are **implicitly ANDed**. So `login success && @smoke` = `(login && success) && @smoke`.
- Operator-like text in quotes: `"a && b"` → single phrase, not boolean
- Tag names with hyphens and dots: `@happy-path`, `@api.v2`
- Status names case variations: `$Failed`, `$FAILED`, `$failed` → all match "Failed"
- Unicode characters in search terms
- Search term that is a substring of an operator: `&` alone, `|` alone, `!` alone
- `!!` immediately followed by `(` : `!!(a || b)` → NOT of grouped expression
- Empty parentheses `()` → parse error → null
- Jint faithfulness: verify JS `Set` behaviour matches browser (tags stored in Set)

## 7. JavaScript Implementation Notes

### 7.1 Pure Functions (in `advanced-search.js`)

All logic lives here. No DOM. Testable via Jint.

```javascript
function isAdvancedSearch(input) {
    return /&&|\|\||!!/.test(input);
}

function advancedSearchTokenise(input) {
    // Returns array of { type, value } tokens
    // Handles: &&, ||, !!, (, ), "...", @word, $word, bare words
    // Adjacent operands without operators get implicit AND inserted
}

function advancedSearchParse(tokens) {
    // Recursive descent: OrExpr → AndExpr → NotExpr → Primary
    // Returns AST node or null on error
}

function advancedSearchEvaluate(ast, searchText, tags, status) {
    // Walks AST, returns boolean
    // searchText: lowercased string (pre-computed, from data-search attr)
    // tags: Set of lowercased strings (categories + labels)
    // status: string like "Passed", "Failed", etc.
}

function advancedSearchMatch(input, searchText, tags, status) {
    // Convenience: tokenise → parse → evaluate
    // Returns boolean, or null on parse error
}
```

### 7.2 DOM Glue (remains in `ReportGenerator.cs` as inline JS)

The `run_search_scenarios()` function remains as inline JS in ReportGenerator.cs. It's a thin wrapper:

```javascript
function run_search_scenarios() {
    var c = fc();
    let input = document.getElementById('searchbar').value.toLowerCase().trim();

    if (isAdvancedSearch(input)) {
        // Advanced path
        for (let i = 0; i < c.items.length; i++) {
            let item = c.items[i];
            let tags = new Set();
            let cats = (item.el.getAttribute('data-categories') || '').toLowerCase();
            let labels = (item.el.getAttribute('data-labels') || '').toLowerCase();
            if (cats) cats.split(',').forEach(t => tags.add(t.trim()));
            if (labels) labels.split(',').forEach(t => tags.add(t.trim()));

            let result = advancedSearchMatch(input, item.searchText, tags, item.status);
            if (result === null) {
                // Parse error — fall through to legacy below
                break;
            }
            item.sr = !result;
        }
        // ... applyVisibility, single-match expand, etc.
    } else {
        // Legacy path — existing code, unchanged
    }
}
```

This DOM glue is **not unit-tested via Jint** (no DOM). It's covered by Selenium tests.

## 8. UX / Placeholder Text

Update the search bar placeholder to hint at the new syntax:

**Current**: `Search scenarios... (use @tag for labels, e.g. @smoke and @api)`

**Proposed**: `Search... (@tag, $status, &&, ||, !!, parentheses)`

Consider adding a small `?` help icon next to the search bar that shows a tooltip/popup with syntax examples:
```
Search syntax:
  word            - text contains "word"
  "exact phrase"  - text contains exact phrase
  @tagname        - has category/label
  $status         - test result (passed, failed, skipped)
  a && b          - both must match
  a || b          - either must match
  !!a             - must NOT match
  (a || b) && c   - grouping with parentheses
```

## 9. Implementation Order (TDD)

1. **Create test project** — `TestTrackingDiagrams.Tests.SearchEngine` with Jint + xUnit, add to solution
2. **Create `JintTestBase.cs`** — Base class that loads JS from embedded resource into Jint engine
3. **Create `advanced-search.js`** — Stub file with empty functions, add as `<EmbeddedResource>`
4. **Tokeniser (TDD)** — Write failing `TokeniserTests.cs` → implement `advancedSearchTokenise()` in JS → green → refactor
5. **Parser (TDD)** — Write failing `ParserTests.cs` → implement `advancedSearchParse()` in JS → green → refactor
6. **Evaluator (TDD)** — Write failing `EvaluatorTests.cs` → implement `advancedSearchEvaluate()` in JS → green → refactor
7. **Integration tests (TDD)** — Write failing `IntegrationTests.cs` using `advancedSearchMatch()` → green
8. **Backward compat tests** — Load legacy + new JS into Jint, write `BackwardCompatibilityTests.cs` → green
9. **Edge case tests** — Write `EdgeCaseTests.cs` → green
10. **Wire into ReportGenerator** — Load embedded JS, update `run_search_scenarios()` DOM glue
11. **HTML structure tests** — Verify generated report HTML contains the new JS functions
12. **Selenium tests** — End-to-end browser tests for the advanced search UX
13. **Update placeholder text and help tooltip**
14. **Update wiki documentation**

## 10. Open Questions / Decisions for Review

1. **`!!` vs `!` for NOT**: `!!` is safer against conflicts but unusual. Alternative: require `NOT` as keyword only when followed by `@`/`$` operand? Or use `~` for NOT? → **Current decision: `!!`** for maximum safety.

2. **`$status` syntax**: Is this useful enough to include in v1, or should we defer? It adds complexity but is a natural extension. → **Current decision: include.**

3. **Implicit AND fallback**: When `&&`/`||`/`!!` operators are present, should bare-word adjacency still imply AND? E.g., `login success && @smoke` — is `login success` two implicit-AND words or a parse error?  
   → **Current decision**: In advanced mode, adjacent bare words (not separated by an operator) are **implicitly ANDed**. So `login success && @smoke` = `(login && success) && @smoke`. This is achieved by inserting implicit AND tokens between adjacent operands during tokenisation.

4. **Help tooltip**: Should this be a hoverable `?` icon, a collapsible section, or just placeholder text? → **Defer to implementation, start with placeholder text.**

5. **Visual feedback on parse errors**: Should the search bar change colour/border when an expression fails to parse and falls back to legacy mode? → **Nice to have, defer to v2.**

6. **Tag keyword backward compat**: The current `@tag and @tag2 or not @tag3` uses English keywords. With unified parsing, should `and`/`or`/`not` keywords continue to work only within `@tag`-only expressions, or everywhere? → **Current decision: only in legacy mode (no `&&`/`||`/`!!` present). When advanced operators are detected, only `&&`/`||`/`!!` are recognized as operators.**

7. **Existing `evaluateTagExpression()` JS**: Should we also test this legacy function via Jint (extract it to the `.js` file too)? → **Current decision: out of scope for this change, but a natural follow-up.** The backward compat tests will call `isAdvancedSearch()` to verify it correctly identifies legacy inputs, but won't re-test the legacy tag parser itself.
