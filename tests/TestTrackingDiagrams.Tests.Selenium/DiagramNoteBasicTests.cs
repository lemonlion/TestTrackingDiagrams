using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DiagramNoteBasicTests : DiagramNoteTestBase
{
    public DiagramNoteBasicTests(ChromeFixture chrome) : base(chrome, "ttd-notes-basic-") { }

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
