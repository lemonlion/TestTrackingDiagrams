using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DiagramNoteTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(DiagramNoteTests).Assembly.Location)!,
        "SeleniumOutput");

    public DiagramNoteTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-notes-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
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

        // Click scenario-level "Collapsed" radio button
        var collapseBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='collapsed']"));
        collapseBtn.Click();

        // Wait for re-render — plus button should appear
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > 0);

        var plusBtns = _driver.FindElements(By.CssSelector("[data-note-btn='plus']"));
        var minusBtns = _driver.FindElements(By.CssSelector("[data-note-btn='minus']"));
        Assert.True(plusBtns.Count > 0, "Plus button should appear when note is collapsed");
        Assert.Equal(0, minusBtns.Count);
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
        Assert.Equal(0, plusBtns.Count);
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

    private IWebElement WaitForReRender(string previousSvgHtml, int timeoutSeconds = 15)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                return svg.GetAttribute("outerHTML") != previousSvgHtml ? svg : null;
            }
            catch { return null; }
        }) ?? throw new TimeoutException("SVG did not re-render");
    }

    private string GetSvgHtml() =>
        _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"))
               .GetAttribute("outerHTML");

    private void SetScenarioState(string state)
    {
        var btn = _driver.FindElement(By.CssSelector(
            $".diagram-toggle .details-radio-btn[data-state='{state}']"));
        btn.Click();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);
    }

    private void DoubleClickFirstNoteAndWait()
    {
        var htmlBefore = GetSvgHtml();
        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        // Use JS to dispatch dblclick directly on hover rect — Selenium's Actions.DoubleClick
        // can miss the hover rect when SVG text elements rendered on top intercept the event.
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));",
            hoverRect);
        WaitForReRender(htmlBefore);
    }

    private void ClickNoteButton(string cssSelector)
    {
        var htmlBefore = GetSvgHtml();
        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var btn = d.FindElement(By.CssSelector(cssSelector));
            return btn.GetCssValue("opacity") != "0";
        });

        var btn = _driver.FindElement(By.CssSelector(cssSelector + " rect"));
        btn.Click();
        WaitForReRender(htmlBefore);
    }

    private void ClickDownArrowAndWait()
    {
        var htmlBefore = GetSvgHtml();
        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
        new Actions(_driver).MoveToElement(hoverRect).Perform();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i =>
            {
                var texts = i.FindElements(By.TagName("text"));
                return texts.Any(t => t.Text.Contains("\u25BC")) && i.GetCssValue("opacity") != "0";
            });
        });

        var downArrowGroup = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .First(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25BC")));
        downArrowGroup.FindElement(By.TagName("rect")).Click();
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

        var upArrowGroup = _driver.FindElements(By.CssSelector(".note-toggle-icon"))
            .First(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25B2")));
        upArrowGroup.FindElement(By.TagName("rect")).Click();
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

        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(hoverRects.Count >= 2);

        // Hover second note (short)
        new Actions(_driver).MoveToElement(hoverRects[1]).Perform();

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

        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(hoverRects.Count >= 2);
        new Actions(_driver).MoveToElement(hoverRects[1]).Perform();

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

        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        new Actions(_driver).MoveToElement(hoverRects[0]).Perform();

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
}
