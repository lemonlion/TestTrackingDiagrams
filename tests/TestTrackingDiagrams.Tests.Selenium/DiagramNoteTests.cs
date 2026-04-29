using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DiagramNoteTests : IClassFixture<ChromeFixture>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(DiagramNoteTests).Assembly.Location)!,
        "SeleniumOutput");

    public DiagramNoteTests(ChromeFixture chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-notes-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateReport(string fileName) =>
        ReportTestHelper.GenerateReport(_tempDir, OutputDir, fileName);

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private IWebElement WaitForDiagramSvg(int timeoutSeconds = 20)
    {
        // Force rendering — IntersectionObserver doesn't fire reliably in headless Chrome
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(document.body);");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                return svg.Displayed ? svg : null;
            }
            catch (NoSuchElementException) { return null; }
        }) ?? throw new TimeoutException("Diagram SVG did not render");
    }

    private void ExpandFirstScenarioWithDiagram()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    // ── Note toggle buttons appear after SVG renders ──

    [Fact]
    public void Note_toggle_icons_appear_after_render()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteToggleIcons.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // Wait for notes to be processed (makeNotesCollapsible runs after render)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var toggleIcons = wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Count > 0 ? icons : null;
        });

        Assert.NotNull(toggleIcons);
        Assert.True(toggleIcons.Count > 0, "Note toggle icons should exist after diagram renders");
    }

    [Fact]
    public void Note_hover_rects_exist_after_render()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteHoverRects.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var hoverRects = wait.Until(d =>
        {
            var rects = d.FindElements(By.CssSelector(".note-hover-rect"));
            return rects.Count > 0 ? rects : null;
        });

        Assert.NotNull(hoverRects);
    }

    // ── Per-scenario radio buttons ──

    [Fact]
    public void Scenario_truncated_is_active_by_default()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteScenarioTruncDefault.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // Scenario-level truncation controls (inside .diagram-toggle)
        var truncBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='truncated']"));
        Assert.Contains("details-active", truncBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Clicking_scenario_expanded_activates_it()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteScenarioExpanded.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var expandBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='expanded']"));
        expandBtn.Click();
        Assert.Contains("details-active", expandBtn.GetAttribute("class")!);

        var truncBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='truncated']"));
        Assert.DoesNotContain("details-active", truncBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Clicking_scenario_collapsed_activates_it()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteScenarioCollapsed.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var collapseBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='collapsed']"));
        collapseBtn.Click();
        Assert.Contains("details-active", collapseBtn.GetAttribute("class")!);
    }

    // ── Scenario-level line count dropdown ──

    [Fact]
    public void Scenario_line_count_dropdown_exists()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteLineDropdown.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var select = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .truncate-lines-select"));
        Assert.NotNull(select);
        var selectedOption = new SelectElement(select).SelectedOption;
        Assert.Equal("40", selectedOption.GetAttribute("value"));
    }

    [Fact]
    public void Changing_scenario_line_count_updates_selected_value()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteLineChange.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var select = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .truncate-lines-select"));
        var selectEl = new SelectElement(select);

        Assert.Equal("40", selectEl.SelectedOption.GetAttribute("value"));

        // Change line count to 3
        selectEl.SelectByValue("3");

        Assert.Equal("3", selectEl.SelectedOption.GetAttribute("value"));
    }

    // ── Scenario-level headers radio ──

    [Fact]
    public void Scenario_headers_shown_is_active_by_default()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteHeadersDefault.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var shownBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .headers-radio-btn[data-hstate='shown']"));
        Assert.Contains("details-active", shownBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Clicking_scenario_headers_hidden_activates_it()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteHeadersHidden.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var hiddenBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .headers-radio-btn[data-hstate='hidden']"));
        var shownBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .headers-radio-btn[data-hstate='shown']"));

        hiddenBtn.Click();
        Assert.Contains("details-active", hiddenBtn.GetAttribute("class")!);
        Assert.DoesNotContain("details-active", shownBtn.GetAttribute("class")!);
    }

    // ── Double-click on notes ──

    [Fact]
    public void Double_click_on_note_hover_rect_cycles_state()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteDblClick.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // Wait for note buttons to be set up
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);

        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));

        // Capture SVG before double-click
        var svg1 = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
        var html1 = svg1.GetAttribute("outerHTML");

        // Double-click to cycle note state
        new Actions(_driver).DoubleClick(hoverRect).Perform();

        // SVG should re-render with different note state
        wait.Until(d =>
        {
            try
            {
                var svg2 = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                return svg2.GetAttribute("outerHTML") != html1;
            }
            catch { return false; }
        });
    }

    // ── Plus / minus toggle button on collapsed notes ──

    [Fact]
    public void Collapsed_note_shows_plus_button_in_top_right()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NotePlusCollapsed.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // Scope to the first scenario — other scenarios remain in truncated state
        var scenario = _driver.FindElement(By.CssSelector("details.scenario"));

        // Click scenario-level "Collapsed" radio button
        var collapseBtn = scenario.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='collapsed']"));
        collapseBtn.Click();

        // Wait for re-render — plus button should appear
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => scenario.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > 0);

        var plusBtns = scenario.FindElements(By.CssSelector("[data-note-btn='plus']"));
        var minusBtns = scenario.FindElements(By.CssSelector("[data-note-btn='minus']"));
        Assert.True(plusBtns.Count > 0, "Plus button should appear when note is collapsed");
        Assert.Empty(minusBtns);
    }

    [Fact]
    public void Truncated_note_shows_minus_button_not_plus()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteMinusTruncated.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // Default state is truncated — minus button should be present
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='minus']")).Count > 0);

        var minusBtns = _driver.FindElements(By.CssSelector("[data-note-btn='minus']"));
        var plusBtns = _driver.FindElements(By.CssSelector("[data-note-btn='plus']"));
        Assert.True(minusBtns.Count > 0, "Minus button should appear when note is truncated");
        Assert.Empty(plusBtns);
    }

    [Fact]
    public void Clicking_plus_button_expands_note_and_shows_minus()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NotePlusClick.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // First collapse notes
        var collapseBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='collapsed']"));
        collapseBtn.Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > 0);

        var plusCountBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;
        var minusCountBefore = _driver.FindElements(By.CssSelector("[data-note-btn='minus']")).Count;

        // Hover over note to make buttons visible, then click plus
        var noteHover = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        new Actions(_driver).MoveToElement(noteHover).Perform();

        // Wait for opacity to become visible
        wait.Until(d =>
        {
            var plus = d.FindElement(By.CssSelector("[data-note-btn='plus']"));
            var opacity = plus.GetCssValue("opacity");
            return opacity != "0";
        });

        // Click the plus button's background rect
        var plusBtn = _driver.FindElement(By.CssSelector("[data-note-btn='plus'] rect"));
        plusBtn.Click();

        // After expanding, minus count should increase and plus count should decrease
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='minus']")).Count > minusCountBefore);

        var plusCountAfter = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;
        var minusCountAfter = _driver.FindElements(By.CssSelector("[data-note-btn='minus']")).Count;
        Assert.True(minusCountAfter > minusCountBefore, "Minus button count should increase after expanding");
        Assert.True(plusCountAfter < plusCountBefore, "Plus button count should decrease after expanding");
    }

    // ── Report-level truncation applies to all diagrams ──

    [Fact]
    public void Report_level_expanded_activates_for_all_scenarios()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteReportExpanded.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // Click report-level Expanded button
        var reportExpanded = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']"));
        reportExpanded.Click();

        // Scenario-level buttons should also sync to 'expanded'
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(_ =>
        {
            var scenarioExpandedBtns = _driver.FindElements(By.CssSelector(
                ".diagram-toggle .details-radio-btn[data-state='expanded']"));
            return scenarioExpandedBtns.Count > 0 &&
                   scenarioExpandedBtns.All(b => b.GetAttribute("class")!.Contains("details-active"));
        });
    }

    // ══════════════════════════════════════════════════════════
    // Long note 3-state cycle tests
    // ══════════════════════════════════════════════════════════

    private string GenerateLongNoteReport(string fileName) =>
        ReportTestHelper.GenerateReportWithLongNotes(_tempDir, OutputDir, fileName);

    private void ExpandAndRenderLongNoteDiagram(string fileName)
    {
        _driver.Navigate().GoToUrl(GenerateLongNoteReport(fileName));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);
    }

    /// <summary>
    /// Hover over a note hover rect by index, retrying if the element becomes stale
    /// (can happen when the SVG is still being replaced after a state change).
    /// </summary>
    private void HoverNoteRect(int index, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d =>
        {
            try
            {
                var rects = d.FindElements(By.CssSelector(".note-hover-rect"));
                if (rects.Count <= index) return false;
                new Actions(_driver).MoveToElement(rects[index]).Perform();
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
    }

    private IWebElement WaitForReRender(string previousSvgHtml, int timeoutSeconds = 15)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        // Wait for the SVG to change AND for makeNotesCollapsible() to finish adding
        // toggle icons. Under CPU load, there can be a gap between plantuml.js rendering
        // the new SVG and the JS callback that creates toggle icons on it.
        return wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                if (svg.GetAttribute("outerHTML") == previousSvgHtml) return null;
                // Ensure makeNotesCollapsible has run — it always creates at least one
                // .note-toggle-icon per visible note (plus, minus, or arrow buttons).
                var icons = svg.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Count > 0 ? svg : null;
            }
            catch { return null; }
        }) ?? throw new TimeoutException("SVG did not re-render with note toggle icons");
    }

    private string GetSvgHtml()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        return wait.Until(d =>
        {
            try
            {
                return d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"))
                        .GetAttribute("outerHTML") ?? "";
            }
            catch (StaleElementReferenceException) { return null; }
        }) ?? "";
    }

    private void SetScenarioState(string state)
    {
        var btn = _driver.FindElement(By.CssSelector(
            $".diagram-toggle .details-radio-btn[data-state='{state}']"));
        btn.Click();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        // Wait for both hover rects (note backgrounds) and toggle icons
        // (created by makeNotesCollapsible after re-render) to be present.
        wait.Until(d =>
            d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0 &&
            d.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0);
    }

    private void DoubleClickFirstNoteAndWait()
    {
        var htmlBefore = GetSvgHtml();
        // Retry hoverRect lookup — SVG may still be replacing after a state change.
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var hoverRect = d.FindElement(By.CssSelector(".note-hover-rect"));
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));",
                    hoverRect);
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
        WaitForReRender(htmlBefore);
    }

    private void ClickNoteButton(string cssSelector)
    {
        var htmlBefore = GetSvgHtml();
        HoverNoteRect(0);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var btn = d.FindElement(By.CssSelector(cssSelector));
                return btn.GetCssValue("opacity") != "0";
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Use JS-dispatched click — native .Click() can be intercepted by SVG path elements.
        wait.Until(d =>
        {
            try
            {
                var btn = d.FindElement(By.CssSelector(cssSelector + " rect"));
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));",
                    btn);
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
        WaitForReRender(htmlBefore);
    }

    private void ClickDownArrowAndWait()
    {
        var htmlBefore = GetSvgHtml();
        HoverNoteRect(0);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Any(i =>
                {
                    var texts = i.FindElements(By.TagName("text"));
                    return texts.Any(t => t.Text.Contains("\u25BC")) && i.GetCssValue("opacity") != "0";
                });
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Use JS-dispatched click — native .Click() can be intercepted by SVG path elements.
        wait.Until(d =>
        {
            try
            {
                var downArrowGroup = d.FindElements(By.CssSelector(".note-toggle-icon"))
                    .First(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25BC")));
                var rect = downArrowGroup.FindElement(By.TagName("rect"));
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));",
                    rect);
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
        WaitForReRender(htmlBefore);
    }

    // ── Long note double-click cycle ──

    [Fact]
    public void Long_note_dblclick_from_expanded_goes_to_truncated()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteDblClickExpToTrunc.html");
        SetScenarioState("expanded");

        // Both notes expanded → both have minus. SVG should change on double-click.
        DoubleClickFirstNoteAndWait();

        // First note went expanded → truncated: still shows minus (truncated has minus)
        var minusBtns = _driver.FindElements(By.CssSelector("[data-note-btn='minus']"));
        Assert.True(minusBtns.Count > 0, "Minus button should be present in truncated state");
    }

    [Fact]
    public void Long_note_dblclick_from_truncated_goes_to_collapsed()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteDblClickTruncToColl.html");
        SetScenarioState("truncated");

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        // Double-click first note: truncated → collapsed
        DoubleClickFirstNoteAndWait();

        // Plus count should increase (new plus for collapsed first note)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }

    [Fact]
    public void Long_note_dblclick_from_collapsed_goes_to_truncated_not_expanded()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteDblClickCollToTrunc.html");
        SetScenarioState("collapsed");

        // Wait for both notes to show plus buttons (collapsed state) —
        // SetScenarioState only waits for count > 0 which may be too early.
        var waitForPlus = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        waitForPlus.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count >= 2);

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;
        Assert.True(plusBefore >= 2, "Both notes collapsed should have plus buttons");

        // Double-click first note: collapsed → truncated (NOT expanded, because note is long)
        DoubleClickFirstNoteAndWait();

        // Plus should decrease — first note went from plus (collapsed) to minus (truncated)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count < plusBefore);

        // Verify no ▲ buttons (would only appear if note went to expanded, not truncated)
        var upArrows = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .Where(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")))
            .ToList();
        Assert.Empty(upArrows);
    }

    [Fact]
    public void Long_note_full_3_state_cycle_via_dblclick()
    {
        ExpandAndRenderLongNoteDiagram("LongNote3StateCycle.html");
        SetScenarioState("expanded");

        // expanded → truncated
        DoubleClickFirstNoteAndWait();

        // truncated → collapsed
        DoubleClickFirstNoteAndWait();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > 0);

        // collapsed → truncated (NOT expanded)
        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;
        DoubleClickFirstNoteAndWait();
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count < plusBefore);
    }

    // ── ▼ button from collapsed / truncated ──

    [Fact]
    public void Long_note_down_arrow_from_collapsed_goes_to_truncated_not_expanded()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteDownArrowCollToTrunc.html");
        SetScenarioState("collapsed");

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        // Click ▼ on first note: collapsed → truncated (long note)
        ClickDownArrowAndWait();

        // Plus should decrease
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count < plusBefore);

        // No ▲ should appear (would indicate expanded state, not truncated)
        var upArrows = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .Where(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")))
            .ToList();
        Assert.Empty(upArrows);
    }

    [Fact]
    public void Long_note_down_arrow_from_truncated_goes_to_expanded()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteDownArrowTruncToExp.html");
        SetScenarioState("truncated");

        ClickDownArrowAndWait();

        // ▲ should now appear (expanded long note has ▲)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i =>
                i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")));
        });
    }

    [Fact]
    public void Long_note_plus_button_from_collapsed_goes_to_truncated()
    {
        ExpandAndRenderLongNoteDiagram("LongNotePlusCollToTrunc.html");
        SetScenarioState("collapsed");

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        ClickNoteButton("[data-note-btn='plus']");

        // Plus should decrease (collapsed→truncated for long note)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count < plusBefore);

        // No ▲ — would mean expanded, not truncated
        var upArrows = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .Where(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")))
            .ToList();
        Assert.Empty(upArrows);
    }

    // ── ▲ button tests ──

    [Fact]
    public void Long_note_up_arrow_visible_when_expanded()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteUpArrowVisible.html");
        SetScenarioState("expanded");

        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i =>
                i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2"))
                && i.GetCssValue("opacity") != "0");
        });
    }

    [Fact]
    public void Long_note_up_arrow_not_visible_when_truncated()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteUpArrowNotTrunc.html");
        SetScenarioState("truncated");

        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i => i.GetCssValue("opacity") != "0");
        });

        var upArrows = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .Where(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")))
            .ToList();
        Assert.Empty(upArrows);
    }

    [Fact]
    public void Long_note_up_arrow_click_goes_to_truncated()
    {
        ExpandAndRenderLongNoteDiagram("LongNoteUpArrowToTrunc.html");
        SetScenarioState("expanded");

        var htmlBefore = GetSvgHtml();
        HoverNoteRect(0);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d =>
        {
            try
            {
                var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Any(i =>
                    i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2"))
                    && i.GetCssValue("opacity") != "0");
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Use JS-dispatched click — native .Click() can be intercepted by SVG path elements.
        wait.Until(d =>
        {
            try
            {
                var upArrowGroup = d.FindElements(By.CssSelector(".note-toggle-icon"))
                    .First(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")));
                var rect = upArrowGroup.FindElement(By.TagName("rect"));
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));",
                    rect);
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
        WaitForReRender(htmlBefore);

        // After clicking ▲, note went to truncated — ▲ should disappear
        var upArrowsAfter = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .Where(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")))
            .ToList();
        Assert.Empty(upArrowsAfter);
    }

    // ── Short note 2-state cycle ──

    [Fact]
    public void Short_note_dblclick_from_expanded_goes_to_collapsed()
    {
        ExpandAndRenderLongNoteDiagram("ShortNoteDblClickExpToColl.html");
        SetScenarioState("expanded");

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(hoverRects.Count >= 2, "Should have at least 2 notes");

        var htmlBefore = GetSvgHtml();
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));",
            hoverRects[1]);
        WaitForReRender(htmlBefore);

        // Second (short) note: expanded → collapsed = plus count increases
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }

    [Fact]
    public void Short_note_dblclick_from_collapsed_goes_to_expanded()
    {
        ExpandAndRenderLongNoteDiagram("ShortNoteDblClickCollToExp.html");
        SetScenarioState("collapsed");

        var minusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='minus']")).Count;

        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(hoverRects.Count >= 2);

        var htmlBefore = GetSvgHtml();
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));",
            hoverRects[1]);
        WaitForReRender(htmlBefore);

        // Second (short) note: collapsed → expanded = minus count increases
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='minus']")).Count > minusBefore);
    }

    [Fact]
    public void Short_note_no_up_arrow_when_expanded()
    {
        ExpandAndRenderLongNoteDiagram("ShortNoteNoUpArrow.html");
        SetScenarioState("expanded");

        // Hover second note (short) — uses retry to avoid stale element after re-render
        HoverNoteRect(1);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i => i.GetCssValue("opacity") != "0");
        });

        var visibleUpArrows = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .Where(i => i.GetCssValue("opacity") != "0")
            .Where(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")))
            .ToList();
        Assert.Empty(visibleUpArrows);
    }

    // ── Truncation level change tests ──

    [Fact]
    public void Reducing_truncation_makes_short_note_become_long()
    {
        // Second note has 4 lines. At default=40, it's short. At truncation=3, 4>3 = long.
        ExpandAndRenderLongNoteDiagram("NoteTruncReduceBecomesLong.html");

        var select = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .truncate-lines-select"));
        new SelectElement(select).SelectByValue("3");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);

        // Both notes now "long" at truncation=3. Set expanded to verify ▲ on second note.
        SetScenarioState("expanded");

        // Hover second note — uses retry to avoid stale element after re-render
        HoverNoteRect(1);

        // ▲ should now appear for second note (it's "long" at truncation=3)
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i =>
                i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2"))
                && i.GetCssValue("opacity") != "0");
        });
    }

    [Fact]
    public void Scenario_truncation_change_respected_by_note_buttons()
    {
        ExpandAndRenderLongNoteDiagram("ScenarioTruncChangeButtons.html");

        var select = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .truncate-lines-select"));
        new SelectElement(select).SelectByValue("3");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);

        SetScenarioState("expanded");

        // Hover first note — uses retry to avoid stale element after re-render
        HoverNoteRect(0);

        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i =>
                i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2"))
                && i.GetCssValue("opacity") != "0");
        });
    }

    // ── Minus button tests ──

    [Fact]
    public void Minus_button_from_expanded_goes_to_collapsed()
    {
        ExpandAndRenderLongNoteDiagram("MinusExpToColl.html");
        SetScenarioState("expanded");

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        ClickNoteButton("[data-note-btn='minus']");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }

    [Fact]
    public void Minus_button_from_truncated_goes_to_collapsed()
    {
        ExpandAndRenderLongNoteDiagram("MinusTruncToColl.html");
        SetScenarioState("truncated");

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        ClickNoteButton("[data-note-btn='minus']");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }

    // ── Partition (SeparateSetup) note buttons ──

    private string GeneratePartitionReport(string fileName) =>
        ReportTestHelper.GenerateReportWithPartitionDiagram(_tempDir, OutputDir, fileName);

    [Fact]
    public void Note_hover_rects_found_inside_partition_groups()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("PartitionNoteHoverRects.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var hoverRects = wait.Until(d =>
        {
            var rects = d.FindElements(By.CssSelector(".note-hover-rect"));
            return rects.Count >= 3 ? rects : null; // 3 notes: 1 in partition, 2 outside
        });

        Assert.NotNull(hoverRects);
        Assert.True(hoverRects!.Count >= 3,
            $"Expected at least 3 note hover rects (1 inside partition + 2 outside), got {hoverRects.Count}");
    }

    [Fact]
    public void Note_toggle_icons_found_inside_partition_groups()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("PartitionNoteToggleIcons.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var toggleIcons = wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Count >= 3 ? icons : null; // 3 notes total
        });

        Assert.NotNull(toggleIcons);
        Assert.True(toggleIcons!.Count >= 3,
            $"Expected at least 3 note toggle icons (1 inside partition + 2 outside), got {toggleIcons.Count}");
    }

    [Fact]
    public void Partition_note_buttons_respond_to_hover()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("PartitionNoteHover.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 3);

        // Hover over the first note hover rect to trigger button visibility
        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        var actions = new Actions(_driver);
        actions.MoveToElement(hoverRects[0]).Perform();

        // Buttons should become visible (opacity > 0)
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i =>
            {
                var opacity = i.GetCssValue("opacity");
                return opacity != "0";
            });
        });
    }

    [Fact]
    public void Partition_note_double_click_cycles_state()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("PartitionNoteDblClick.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 3);

        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        var svg1 = GetSvgHtml();

        new Actions(_driver).DoubleClick(hoverRect).Perform();

        WaitForReRender(svg1);
    }

    [Fact]
    public void Partition_note_scenario_collapse_shows_plus_buttons()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("PartitionNoteCollapse.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 3);

        // Click scenario-level "Collapsed" radio button
        var collapseBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='collapsed']"));
        collapseBtn.Click();

        // After collapsing, plus buttons should appear for all 3 notes
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count >= 3);

        var plusBtns = _driver.FindElements(By.CssSelector("[data-note-btn='plus']"));
        Assert.True(plusBtns.Count >= 3,
            $"Expected plus buttons for all 3 notes (1 in partition + 2 outside), got {plusBtns.Count}");
    }

    [Fact]
    public void Partition_svg_structure_has_expected_note_groups()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("PartitionSvgStructure.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);

        // Use JS to dump SVG structure for diagnosis
        var js = (IJavaScriptExecutor)_driver;
        var noteGroupCount = js.ExecuteScript(@"
            var svg = document.querySelector('[data-diagram-type=""plantuml""] svg');
            return window._findNoteGroups(svg).length;
        ");
        Assert.Equal(3L, (long)noteGroupCount!);

        // Check what parseNoteBlocks finds
        var container = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
        var noteBlockCount = js.ExecuteScript(@"
            var c = arguments[0];
            var source = c._noteOriginalSource || c.getAttribute('data-plantuml');
            var blocks = (function() {
                var lines = source.split('\n');
                var notes = [];
                for (var i = 0; i < lines.length; i++) {
                    var trimmed = lines[i].trim();
                    if (/^note(?:<<\w+>>)?\s+(left|right)/.test(trimmed)) {
                        var start = i; i++;
                        var cl = [];
                        while (i < lines.length && lines[i].trim() !== 'end note') { cl.push(lines[i]); i++; }
                        notes.push({ start: start, end: i, contentLines: cl });
                    }
                }
                return notes;
            })();
            return blocks.length;
        ", container);
        Assert.Equal(3L, (long)noteBlockCount!);

        // Dump mainG children structure
        var childrenInfo = (string)js.ExecuteScript(@"
            var svg = document.querySelector('[data-diagram-type=""plantuml""] svg');
            var mainG = null;
            for (var i = 0; i < svg.children.length; i++) {
                if (svg.children[i].tagName === 'g') { mainG = svg.children[i]; break; }
            }
            if (!mainG) return 'No mainG';
            var result = 'mainG children count: ' + mainG.children.length + '\n';
            for (var i = 0; i < mainG.children.length; i++) {
                var child = mainG.children[i];
                var fill = child.getAttribute('fill') || '';
                var cls = child.getAttribute('class') || '';
                var tag = child.tagName;
                if (tag === 'g') {
                    result += i + ': <g> (children: ' + child.children.length + ')\n';
                } else {
                    result += i + ': <' + tag + '> fill=' + fill + ' class=' + cls + '\n';
                }
            }
            return result;
        ")!;

        // Output structure for diagnostic purposes
        Assert.False(string.IsNullOrEmpty(childrenInfo), "SVG structure info should be available");
    }

    // ── Partition with LONG notes (triggering truncation) ──

    private string GeneratePartitionLongNoteReport(string fileName) =>
        ReportTestHelper.GenerateReportWithPartitionLongNotes(_tempDir, OutputDir, fileName);

    [Fact]
    public void Partition_long_notes_have_hover_rects()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionLongNoteReport("PartitionLongNoteHover.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        var hoverRects = wait.Until(d =>
        {
            var rects = d.FindElements(By.CssSelector(".note-hover-rect"));
            return rects.Count >= 3 ? rects : null;
        });

        Assert.NotNull(hoverRects);
        Assert.True(hoverRects!.Count >= 3,
            $"Expected at least 3 note hover rects for partition with long notes, got {hoverRects.Count}");
    }

    [Fact]
    public void Partition_long_note_double_click_cycles_state()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionLongNoteReport("PartitionLongNoteDblClick.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 3);

        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        var svg1 = GetSvgHtml();

        new Actions(_driver).DoubleClick(hoverRect).Perform();

        WaitForReRender(svg1);
    }

    [Fact]
    public void Partition_long_note_expand_click_works()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionLongNoteReport("PartitionLongNoteExpand.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 3);

        // Default state is truncated; expand ▼ buttons should exist
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='minus']")).Count >= 3);

        // Hover and click the ▼ expand button on first note
        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        // Wait for expansion buttons to become visible
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i => i.GetCssValue("opacity") != "0");
        });

        var svg1 = GetSvgHtml();

        // Click the minus button to collapse the note — use JS to avoid
        // click interception by the note fold path that overlaps the button
        var minusBtn = _driver.FindElement(By.CssSelector("[data-note-btn='minus'] rect"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].dispatchEvent(new MouseEvent('click', {bubbles:true}));", minusBtn);

        // SVG should re-render after clicking
        WaitForReRender(svg1);
    }

    // ── findNoteGroups must exclude participant/partition fills ──

    [Fact]
    public void FindNoteGroups_excludes_participant_fill_E2E2F0()
    {
        // Load a page with the JS functions available
        _driver.Navigate().GoToUrl(GeneratePartitionReport("FindNoteGroupsExclude.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);

        var js = (IJavaScriptExecutor)_driver;

        // Inject participant-fill (#E2E2F0) paths + text into the SVG's mainG before the real notes
        // Then re-run findNoteGroups and verify the injected elements are NOT counted as notes
        var result = (IDictionary<string, object?>)js.ExecuteScript(@"
            var svg = document.querySelector('[data-diagram-type=""plantuml""] svg');
            var mainG = null;
            for (var i = 0; i < svg.children.length; i++) {
                if (svg.children[i].tagName === 'g') { mainG = svg.children[i]; break; }
            }

            var SVGNS = 'http://www.w3.org/2000/svg';

            // Count original note groups
            var originalCount = window._findNoteGroups(svg).length;

            // Inject a participant-fill path + text at the start of mainG (before first child)
            var fakePath = document.createElementNS(SVGNS, 'path');
            fakePath.setAttribute('fill', '#E2E2F0');
            fakePath.setAttribute('d', 'M10,10 L200,10 L200,50 L10,50 Z');
            var fakeText = document.createElementNS(SVGNS, 'text');
            fakeText.textContent = 'FakeParticipant';
            fakeText.setAttribute('x', '50');
            fakeText.setAttribute('y', '30');

            // Also inject a partition-label-fill path + text
            var fakePath2 = document.createElementNS(SVGNS, 'path');
            fakePath2.setAttribute('fill', '#e2e2f0');
            fakePath2.setAttribute('d', 'M10,60 L200,60 L200,100 L10,100 Z');
            var fakeText2 = document.createElementNS(SVGNS, 'text');
            fakeText2.textContent = 'Setup';
            fakeText2.setAttribute('x', '50');
            fakeText2.setAttribute('y', '80');

            // Insert at the start of mainG (before any existing children)
            var firstChild = mainG.firstChild;
            mainG.insertBefore(fakeText2, firstChild);
            mainG.insertBefore(fakePath2, fakeText2);
            mainG.insertBefore(fakeText, fakePath2);
            mainG.insertBefore(fakePath, fakeText);

            // Re-count note groups after injection
            var afterCount = window._findNoteGroups(svg).length;

            return { originalCount: originalCount, afterCount: afterCount };
        ")!;

        var originalCount = Convert.ToInt64(result["originalCount"]);
        var afterCount = Convert.ToInt64(result["afterCount"]);

        // After injecting 2 participant-fill elements, findNoteGroups should NOT count them as notes
        Assert.Equal(originalCount, afterCount);
    }

    [Fact]
    public void FindNoteGroups_still_detects_note_fill_FEFFDD()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("FindNoteGroupsDetect.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);

        var js = (IJavaScriptExecutor)_driver;

        // Inject a note-fill (#FEFFDD) path + text at the start of mainG
        var result = (IDictionary<string, object?>)js.ExecuteScript(@"
            var svg = document.querySelector('[data-diagram-type=""plantuml""] svg');
            var mainG = null;
            for (var i = 0; i < svg.children.length; i++) {
                if (svg.children[i].tagName === 'g') { mainG = svg.children[i]; break; }
            }

            var SVGNS = 'http://www.w3.org/2000/svg';
            var originalCount = window._findNoteGroups(svg).length;

            // Inject a note-fill path + text
            var fakePath = document.createElementNS(SVGNS, 'path');
            fakePath.setAttribute('fill', '#FEFFDD');
            fakePath.setAttribute('d', 'M10,10 L200,10 L200,50 L10,50 Z');
            var fakeText = document.createElementNS(SVGNS, 'text');
            fakeText.textContent = 'Fake note content';
            fakeText.setAttribute('x', '50');
            fakeText.setAttribute('y', '30');

            var firstChild = mainG.firstChild;
            mainG.insertBefore(fakeText, firstChild);
            mainG.insertBefore(fakePath, fakeText);

            var afterCount = window._findNoteGroups(svg).length;

            return { originalCount: originalCount, afterCount: afterCount };
        ")!;

        var originalCount = Convert.ToInt64(result["originalCount"]);
        var afterCount = Convert.ToInt64(result["afterCount"]);

        // After injecting 1 note-fill element, findNoteGroups SHOULD count it
        Assert.Equal(originalCount + 1, afterCount);
    }

    [Fact]
    public void MakeNotesCollapsible_matches_groups_to_blocks_correctly_when_extra_groups_exist()
    {
        _driver.Navigate().GoToUrl(GeneratePartitionReport("NoteGroupBlockMatch.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);

        var js = (IJavaScriptExecutor)_driver;

        // Get the container and verify note blocks match hover rects
        var container = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
        var noteBlockCount = (long)js.ExecuteScript(@"
            var c = arguments[0];
            var source = c._noteOriginalSource || c.getAttribute('data-plantuml');
            return window._parseNoteBlocks(source).length;
        ", container)!;

        var hoverRectCount = _driver.FindElements(By.CssSelector(".note-hover-rect")).Count;

        // The hover rect count should match the note block count (not more)
        Assert.Equal(noteBlockCount, (long)hoverRectCount);
    }

    // ── Multi-diagram truncation hover button regression ──

    private string GenerateTwoLongNoteReport(string fileName) =>
        ReportTestHelper.GenerateReportWithTwoLongNoteDiagrams(_tempDir, OutputDir, fileName);

    private string GenerateSplitDiagramReport(string fileName) =>
        ReportTestHelper.GenerateReportWithSplitDiagramLongNotes(_tempDir, OutputDir, fileName);

    private string GenerateThreeDiagramSplitReport(string fileName) =>
        ReportTestHelper.GenerateReportWithThreeDiagramSplit(_tempDir, OutputDir, fileName);

    // ── Split-diagram initial render regression tests ──
    // These verify that hover buttons appear on ALL diagrams within a single scenario
    // on initial page load (no truncation change needed). This caught a regression where
    // only the first diagram got hover buttons.

    [Fact]
    public void Split_diagram_all_parts_have_hover_rects_on_initial_render()
    {
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagInitialHoverRects.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;

        // Force-render all diagrams (some may be outside viewport)
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        // Wait for both diagrams to render SVGs
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);

        // Wait for note processing — each diagram should have hover rects
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 2);

        // Verify EACH diagram has hover rects and toggle icons on initial render
        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        Assert.True(allContainers.Count >= 2, $"Expected at least 2 diagram containers, found {allContainers.Count}");

        for (var i = 0; i < allContainers.Count; i++)
        {
            var hoverRects = allContainers[i].FindElements(By.CssSelector(".note-hover-rect"));
            Assert.True(hoverRects.Count > 0,
                $"Diagram {i + 1} should have hover rects on initial render, but has {hoverRects.Count}");
        }
    }

    [Fact]
    public void Split_diagram_all_parts_have_toggle_icons_on_initial_render()
    {
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagInitialToggleIcons.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);
        wait.Until(d => d.FindElements(By.CssSelector(".note-toggle-icon")).Count >= 2);

        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        Assert.True(allContainers.Count >= 2, $"Expected at least 2 containers, found {allContainers.Count}");

        for (var i = 0; i < allContainers.Count; i++)
        {
            var icons = allContainers[i].FindElements(By.CssSelector(".note-toggle-icon"));
            Assert.True(icons.Count > 0,
                $"Diagram {i + 1} should have toggle icons on initial render, but has {icons.Count}");
        }
    }

    [Fact]
    public void Split_diagram_second_part_hover_shows_buttons_on_initial_render()
    {
        // Reproduce the regression: hover buttons visible on first diagram but not second
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagSecondHoverInitial.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 2);

        // Get the SECOND diagram container
        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        Assert.True(allContainers.Count >= 2, $"Expected at least 2 containers, found {allContainers.Count}");
        var secondContainer = allContainers[1];

        // Hover over the second diagram's hover rect
        var hoverRect = secondContainer.FindElement(By.CssSelector(".note-hover-rect"));
        js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", hoverRect);
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        // Verify buttons become visible
        wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var icons = secondContainer.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i => i.GetCssValue("opacity") != "0");
        });
    }

    [Fact]
    public void Split_diagram_first_part_hover_shows_buttons_on_initial_render()
    {
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagFirstHoverInitial.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 2);

        // Get the FIRST diagram container
        var firstContainer = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']")).First();
        var hoverRect = firstContainer.FindElement(By.CssSelector(".note-hover-rect"));
        js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", hoverRect);
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var icons = firstContainer.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i => i.GetCssValue("opacity") != "0");
        });
    }

    [Fact]
    public void Split_diagram_dblclick_on_second_diagram_note_cycles_state()
    {
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagDblClickSecond.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 2);

        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        var secondContainer = allContainers[1];
        var hoverRect = secondContainer.FindElement(By.CssSelector(".note-hover-rect"));
        js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", hoverRect);

        // Capture SVG before double-click
        var svgBefore = secondContainer.FindElement(By.CssSelector("svg")).GetAttribute("outerHTML");

        // Double-click to cycle note state
        js.ExecuteScript(
            "arguments[0].dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));",
            hoverRect);

        // SVG should re-render after double-click
        wait.Until(d =>
        {
            try
            {
                var svg = secondContainer.FindElement(By.CssSelector("svg"));
                return svg.GetAttribute("outerHTML") != svgBefore;
            }
            catch { return false; }
        });

        // After re-render, second diagram should still have hover rects and toggle icons
        wait.Until(d =>
        {
            var rects = secondContainer.FindElements(By.CssSelector(".note-hover-rect"));
            var icons = secondContainer.FindElements(By.CssSelector(".note-toggle-icon"));
            return rects.Count > 0 && icons.Count > 0;
        });
    }

    [Fact]
    public void Split_diagram_scenario_state_change_preserves_second_diagram_buttons()
    {
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagScenarioState.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);
        // Wait for BOTH diagram containers to individually have note-hover-rect
        wait.Until(d =>
        {
            try
            {
                var containers = d.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
                if (containers.Count < 2) return false;
                return containers[0].FindElements(By.CssSelector(".note-hover-rect")).Count > 0
                    && containers[1].FindElements(By.CssSelector(".note-hover-rect")).Count > 0;
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Use JS to diagnose the second container before clicking Expanded
        var beforeState = (string)js.ExecuteScript("""
            var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
            var results = [];
            containers.forEach(function(c, idx) {
                results.push({
                    idx: idx,
                    id: c.id,
                    hoverRects: c.querySelectorAll('.note-hover-rect').length,
                    toggleIcons: c.querySelectorAll('.note-toggle-icon').length,
                    noteSteps: JSON.stringify(c._noteSteps || {}),
                    hasOrigSource: !!c._noteOriginalSource,
                    noteRendering: !!c._noteRendering,
                    plantumlRendering: !!window._plantumlRendering
                });
            });
            return JSON.stringify(results, null, 2);
        """)!;

        // Click scenario-level "Expanded" radio
        var expandBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='expanded']"));
        expandBtn.Click();

        // Use JS-based wait — poll from JavaScript to avoid stale element issues
        wait.Until(_ =>
        {
            try
            {
                var done = (bool)js.ExecuteScript("""
                    var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
                    if (containers.length < 2) return false;
                    for (var i = 0; i < containers.length; i++) {
                        if (containers[i].querySelectorAll('.note-hover-rect').length === 0) return false;
                    }
                    return true;
                """)!;
                return done;
            }
            catch { return false; }
        });

        // Diagnostics after expanded
        var afterState = (string)js.ExecuteScript("""
            var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
            var results = [];
            containers.forEach(function(c, idx) {
                results.push({
                    idx: idx,
                    id: c.id,
                    hoverRects: c.querySelectorAll('.note-hover-rect').length,
                    toggleIcons: c.querySelectorAll('.note-toggle-icon').length,
                    noteSteps: JSON.stringify(c._noteSteps || {}),
                    plantumlRendering: !!window._plantumlRendering
                });
            });
            return JSON.stringify(results, null, 2);
        """)!;

        // Verify second diagram has buttons after state change
        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        for (var i = 0; i < allContainers.Count; i++)
        {
            var hoverRects = allContainers[i].FindElements(By.CssSelector(".note-hover-rect"));
            var icons = allContainers[i].FindElements(By.CssSelector(".note-toggle-icon"));
            Assert.True(hoverRects.Count > 0,
                $"Diagram {i + 1} should have hover rects after 'Expanded' (before: {beforeState}, after: {afterState})");
            Assert.True(icons.Count > 0,
                $"Diagram {i + 1} should have toggle icons after 'Expanded' (before: {beforeState}, after: {afterState})");
        }
    }

    [Fact]
    public void After_report_truncation_change_all_diagrams_have_hover_buttons()
    {
        _driver.Navigate().GoToUrl(GenerateTwoLongNoteReport("TwoLongNoteTruncHover.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        // Force-render all diagrams (some may be outside viewport / IntersectionObserver range)
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        // Wait for BOTH diagrams to render
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);

        // Wait for initial note processing on both diagrams
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 4);

        // Change report-level truncation to 5
        var select = _driver.FindElement(By.CssSelector(".toolbar-row .truncate-lines-select"));
        new SelectElement(select).SelectByValue("5");

        // Wait for all diagrams to re-render with note toggle icons
        wait.Until(d =>
        {
            var containers = d.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
            return containers.Count >= 2 && containers.All(c =>
                c.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0 &&
                c.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);
        });

        // Verify each diagram container independently has hover rects
        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        for (var i = 0; i < allContainers.Count; i++)
        {
            var hoverRects = allContainers[i].FindElements(By.CssSelector(".note-hover-rect"));
            Assert.True(hoverRects.Count > 0,
                $"Diagram {i + 1} should have hover rects after truncation change, but has {hoverRects.Count}");

            var toggleIcons = allContainers[i].FindElements(By.CssSelector(".note-toggle-icon"));
            Assert.True(toggleIcons.Count > 0,
                $"Diagram {i + 1} should have toggle icons after truncation change, but has {toggleIcons.Count}");
        }
    }

    [Fact]
    public void After_scenario_truncation_change_all_diagrams_have_hover_buttons()
    {
        _driver.Navigate().GoToUrl(GenerateTwoLongNoteReport("TwoLongNoteScenTruncHover.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        // Force-render all diagrams
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        // Wait for BOTH diagrams to render
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 4);

        // Change scenario-level truncation to 5 for the first scenario
        var select = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .truncate-lines-select"));
        new SelectElement(select).SelectByValue("5");

        // Wait for the first scenario's diagram to re-render with toggle icons
        wait.Until(d =>
        {
            var firstContainer = d.FindElements(By.CssSelector("[data-diagram-type='plantuml']")).FirstOrDefault();
            return firstContainer != null &&
                   firstContainer.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0 &&
                   firstContainer.FindElements(By.CssSelector(".note-hover-rect")).Count > 0;
        });

        // Verify the first scenario's diagram has hover rects
        var firstDiagram = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']")).First();
        var hoverRects = firstDiagram.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(hoverRects.Count > 0,
            $"First diagram should have hover rects after scenario truncation change, has {hoverRects.Count}");
    }

    [Fact]
    public void Split_diagram_all_parts_have_hover_buttons_after_truncation_change()
    {
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagTruncHover.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;

        // Diagnostic: check how many containers exist and their state
        var containerCount = (long)js.ExecuteScript(
            "return document.querySelectorAll('[data-diagram-type=\"plantuml\"]').length;")!;
        Assert.True(containerCount >= 2,
            $"Expected at least 2 diagram containers, found {containerCount}");

        // Force-render all diagrams in the scenario
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        // Wait for both diagram parts to render
        wait.Until(d =>
        {
            var svgCount = d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count;
            return svgCount >= 2;
        });
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 2);

        // Change scenario-level truncation to 5
        var select = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .truncate-lines-select"));
        new SelectElement(select).SelectByValue("5");

        // Wait for re-render
        wait.Until(d =>
        {
            var containers = d.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
            return containers.Count >= 2 && containers.All(c =>
                c.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0 &&
                c.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);
        });

        // Verify EACH diagram part has hover rects and toggle icons
        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        for (var i = 0; i < allContainers.Count; i++)
        {
            var hoverRects = allContainers[i].FindElements(By.CssSelector(".note-hover-rect"));
            Assert.True(hoverRects.Count > 0,
                $"Split diagram part {i + 1} should have hover rects, but has {hoverRects.Count}");

            var toggleIcons = allContainers[i].FindElements(By.CssSelector(".note-toggle-icon"));
            Assert.True(toggleIcons.Count > 0,
                $"Split diagram part {i + 1} should have toggle icons, but has {toggleIcons.Count}");
        }
    }

    [Fact]
    public void Split_diagram_hover_buttons_visible_on_second_diagram_after_truncation()
    {
        // Reproduces the bug: one scenario with TWO diagram parts (split diagram),
        // each with long notes. After changing truncation to 5, hovering over the
        // second diagram's note should make toggle buttons visible.
        _driver.Navigate().GoToUrl(GenerateSplitDiagramReport("SplitDiagHoverVisible.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(d => d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg")).Count >= 2);
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 2);

        // Change scenario-level truncation to 5
        var select = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .truncate-lines-select"));
        new SelectElement(select).SelectByValue("5");

        // Wait for re-render on all diagrams
        wait.Until(d =>
        {
            var containers = d.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
            return containers.Count >= 2 && containers.All(c =>
                c.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0 &&
                c.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);
        });

        // Hover over the SECOND diagram's hover rect and verify buttons become visible
        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        var secondContainer = allContainers[1];
        var hoverRect = secondContainer.FindElement(By.CssSelector(".note-hover-rect"));

        js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", hoverRect);
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var icons = secondContainer.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i => i.GetCssValue("opacity") != "0");
        });
    }

    // ── 3-diagram split with Creole separators (..Continued..) regression tests ──
    // PlantUML's Creole "..text.." syntax inside notes creates <line> elements
    // in the SVG that break findNoteGroups() — it expects path → text sequences
    // but gets path → line → text instead.

    private void RenderAllThreeDiagramsAndWait()
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(120));
        // Use JS-based wait to avoid stale elements during re-render
        wait.Until(_ =>
        {
            try
            {
                return (bool)js.ExecuteScript("""
                    var svgs = document.querySelectorAll('[data-diagram-type="plantuml"] svg');
                    return svgs.length >= 3;
                """)!;
            }
            catch { return false; }
        });

        // Wait for all note processing to finish
        wait.Until(_ =>
        {
            try
            {
                return (bool)js.ExecuteScript("""
                    var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
                    for (var i = 0; i < containers.length; i++) {
                        if (containers[i]._noteRendering || window._plantumlRendering) return false;
                    }
                    return true;
                """)!;
            }
            catch { return false; }
        });
    }

    [Fact]
    public void Three_diagram_split_continuation_note_has_hover_rects()
    {
        _driver.Navigate().GoToUrl(GenerateThreeDiagramSplitReport("ThreeSplitContinuationHoverRects.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        RenderAllThreeDiagramsAndWait();

        // puml-2 (the continuation diagram with ..Continued From Previous Diagram..)
        // must have hover rects for its note
        var js = (IJavaScriptExecutor)_driver;
        var puml2HoverRects = (long)js.ExecuteScript("""
            var c = document.getElementById('puml-2');
            return c ? c.querySelectorAll('.note-hover-rect').length : -1;
        """)!;

        Assert.True(puml2HoverRects > 0,
            $"puml-2 (continuation diagram) should have hover rects, has {puml2HoverRects}");
    }

    [Fact]
    public void Three_diagram_split_continuation_note_has_toggle_icons()
    {
        _driver.Navigate().GoToUrl(GenerateThreeDiagramSplitReport("ThreeSplitContinuationToggleIcons.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        RenderAllThreeDiagramsAndWait();

        var js = (IJavaScriptExecutor)_driver;
        var puml2Icons = (long)js.ExecuteScript("""
            var c = document.getElementById('puml-2');
            return c ? c.querySelectorAll('.note-toggle-icon').length : -1;
        """)!;

        Assert.True(puml2Icons > 0,
            $"puml-2 (continuation diagram) should have toggle icons, has {puml2Icons}");
    }

    [Fact]
    public void Three_diagram_split_findNoteGroups_matches_noteBlocks_on_all_diagrams()
    {
        _driver.Navigate().GoToUrl(GenerateThreeDiagramSplitReport("ThreeSplitNoteGroupsMatch.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        RenderAllThreeDiagramsAndWait();

        var js = (IJavaScriptExecutor)_driver;
        var result = (string)js.ExecuteScript("""
            var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
            var results = [];
            containers.forEach(function(c) {
                var svg = c.querySelector('svg');
                var src = c._noteOriginalSource || c.getAttribute('data-plantuml');
                var noteBlocks = window._parseNoteBlocks ? window._parseNoteBlocks(src).length : -1;
                var noteGroups = (svg && window._findNoteGroups) ? window._findNoteGroups(svg).length : -1;
                results.push({ id: c.id, noteBlocks: noteBlocks, noteGroups: noteGroups });
            });
            return JSON.stringify(results);
        """)!;

        // Parse and verify noteGroups >= noteBlocks for each diagram with notes
        var diagrams = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(result)!;
        foreach (var diag in diagrams)
        {
            var id = diag.GetProperty("id").GetString()!;
            var blocks = diag.GetProperty("noteBlocks").GetInt32();
            var groups = diag.GetProperty("noteGroups").GetInt32();
            if (blocks > 0)
            {
                Assert.True(groups >= blocks,
                    $"{id}: noteGroups ({groups}) should be >= noteBlocks ({blocks}). Full: {result}");
            }
        }
    }

    [Fact]
    public void Three_diagram_split_hover_on_continuation_note_shows_buttons()
    {
        _driver.Navigate().GoToUrl(GenerateThreeDiagramSplitReport("ThreeSplitHoverContNote.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        RenderAllThreeDiagramsAndWait();

        var js = (IJavaScriptExecutor)_driver;

        // Scroll to puml-2 and hover over its note
        var hoverRect = (IWebElement)js.ExecuteScript("""
            var c = document.getElementById('puml-2');
            return c ? c.querySelector('.note-hover-rect') : null;
        """)!;
        Assert.NotNull(hoverRect);

        js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", hoverRect);
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        // Verify buttons become visible
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(_ =>
        {
            try
            {
                return (bool)js.ExecuteScript("""
                    var c = document.getElementById('puml-2');
                    var icons = c.querySelectorAll('.note-toggle-icon');
                    for (var i = 0; i < icons.length; i++) {
                        if (icons[i].style.opacity !== '0') return true;
                    }
                    return false;
                """)!;
            }
            catch { return false; }
        });
    }

    [Fact]
    public void Three_diagram_split_all_diagrams_with_notes_have_hover_rects()
    {
        _driver.Navigate().GoToUrl(GenerateThreeDiagramSplitReport("ThreeSplitAllHoverRects.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        RenderAllThreeDiagramsAndWait();

        var js = (IJavaScriptExecutor)_driver;

        // Check every diagram that has noteBlocks also has hoverRects
        var result = (string)js.ExecuteScript("""
            var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
            var results = [];
            containers.forEach(function(c) {
                var src = c._noteOriginalSource || c.getAttribute('data-plantuml');
                var noteBlocks = window._parseNoteBlocks ? window._parseNoteBlocks(src).length : 0;
                var hoverRects = c.querySelectorAll('.note-hover-rect').length;
                results.push({ id: c.id, noteBlocks: noteBlocks, hoverRects: hoverRects });
            });
            return JSON.stringify(results);
        """)!;

        var diagrams = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(result)!;
        foreach (var diag in diagrams)
        {
            var id = diag.GetProperty("id").GetString()!;
            var blocks = diag.GetProperty("noteBlocks").GetInt32();
            var rects = diag.GetProperty("hoverRects").GetInt32();
            if (blocks > 0)
            {
                Assert.True(rects > 0,
                    $"{id}: has {blocks} noteBlocks but {rects} hoverRects. Full: {result}");
            }
        }
    }

    [Fact]
    public void Three_diagram_split_dblclick_on_continuation_note_cycles_state()
    {
        _driver.Navigate().GoToUrl(GenerateThreeDiagramSplitReport("ThreeSplitDblClickCont.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        RenderAllThreeDiagramsAndWait();

        var js = (IJavaScriptExecutor)_driver;

        // Get initial SVG HTML for puml-2
        var svgBefore = (string)js.ExecuteScript("""
            var c = document.getElementById('puml-2');
            var svg = c ? c.querySelector('svg') : null;
            return svg ? svg.outerHTML : '';
        """)!;
        Assert.NotEmpty(svgBefore);

        // Double-click the note hover rect on puml-2
        js.ExecuteScript("""
            var c = document.getElementById('puml-2');
            var hr = c.querySelector('.note-hover-rect');
            if (hr) hr.dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));
        """);

        // Wait for SVG to change (re-render after state cycle)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(_ =>
        {
            try
            {
                var svgAfter = (string)js.ExecuteScript("""
                    var c = document.getElementById('puml-2');
                    var svg = c ? c.querySelector('svg') : null;
                    return svg ? svg.outerHTML : '';
                """)!;
                return svgAfter != svgBefore;
            }
            catch { return false; }
        });

        // After re-render, puml-2 should still have hover rects and toggle icons
        var hasButtons = (bool)js.ExecuteScript("""
            var c = document.getElementById('puml-2');
            return c.querySelectorAll('.note-hover-rect').length > 0
                && c.querySelectorAll('.note-toggle-icon').length > 0;
        """)!;
        Assert.True(hasButtons, "puml-2 should have hover rects and toggle icons after dblclick state cycle");
    }
}
