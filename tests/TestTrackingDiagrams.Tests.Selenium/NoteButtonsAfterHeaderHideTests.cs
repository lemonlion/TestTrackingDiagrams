using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Selenium tests verifying that note hover button click behavior (plus/minus/dblclick
/// 3-state cycle) works correctly AFTER toggling headers to "hidden".
/// This interaction has regressed multiple times because header hiding re-renders the SVG
/// (removing gray lines changes note length), which can break the note state machine
/// and button handlers.
/// </summary>
public class NoteButtonsAfterHeaderHideTests : IClassFixture<ChromeFixture>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(NoteButtonsAfterHeaderHideTests).Assembly.Location)!,
        "SeleniumOutput");

    public NoteButtonsAfterHeaderHideTests(ChromeFixture chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-hdr-notes-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private string GenerateReport(string fileName) =>
        ReportTestHelper.GenerateReportWithLongNotesAndHeaders(_tempDir, OutputDir, fileName);

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

    private void SetScenarioState(string state)
    {
        var btn = _driver.FindElement(By.CssSelector(
            $".diagram-toggle .details-radio-btn[data-state='{state}']"));
        btn.Click();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d =>
            d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0 &&
            d.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0);
    }

    private void ToggleHeadersHidden()
    {
        var scenario = _driver.FindElement(By.CssSelector("details.scenario"));
        var hiddenBtn = scenario.FindElement(By.CssSelector(
            ".headers-radio-btn[data-hstate='hidden']"));
        hiddenBtn.Click();

        // Wait for SVG re-render with toggle icons (headers removal triggers PlantUML re-render)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(d =>
            d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0 &&
            d.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0);
    }

    private void ToggleHeadersShown()
    {
        var scenario = _driver.FindElement(By.CssSelector("details.scenario"));
        var shownBtn = scenario.FindElement(By.CssSelector(
            ".headers-radio-btn[data-hstate='shown']"));
        shownBtn.Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(d =>
            d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0 &&
            d.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0);
    }

    /// <summary>
    /// Hover over a note hover rect by index, retrying if the element becomes stale.
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
        return wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                if (svg.GetAttribute("outerHTML") == previousSvgHtml) return null;
                var icons = svg.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Count > 0 ? svg : null;
            }
            catch { return null; }
        }) ?? throw new TimeoutException("SVG did not re-render with note toggle icons");
    }

    private string GetSvgHtml() =>
        _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"))
               .GetAttribute("outerHTML") ?? "";

    private void DoubleClickFirstNoteAndWait()
    {
        var htmlBefore = GetSvgHtml();
        var hoverRect = _driver.FindElement(By.CssSelector(".note-hover-rect"));
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

    private void NavigateAndSetup(string fileName)
    {
        _driver.Navigate().GoToUrl(GenerateReport(fileName));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);
    }

    // ═══════════════════════════════════════════════════════════
    // Baseline: buttons still exist after hiding headers
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void After_hiding_headers_note_hover_rects_still_exist()
    {
        NavigateAndSetup("HdrHide_HoverRectsExist.html");
        ToggleHeadersHidden();

        var rects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(rects.Count > 0, "Note hover rects should exist after hiding headers");
    }

    [Fact]
    public void After_hiding_headers_note_toggle_icons_still_exist()
    {
        NavigateAndSetup("HdrHide_ToggleIconsExist.html");
        ToggleHeadersHidden();

        var icons = _driver.FindElements(By.CssSelector(".note-toggle-icon"));
        Assert.True(icons.Count > 0, "Note toggle icons should exist after hiding headers");
    }

    // ═══════════════════════════════════════════════════════════
    // 3-state dblclick cycle with headers hidden (long note)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Headers_hidden_dblclick_from_expanded_goes_to_truncated()
    {
        NavigateAndSetup("HdrHide_DblClickExpToTrunc.html");
        SetScenarioState("expanded");
        ToggleHeadersHidden();

        // Double-click first note: expanded → truncated
        DoubleClickFirstNoteAndWait();

        // Truncated state should have minus buttons (not plus)
        var minusBtns = _driver.FindElements(By.CssSelector("[data-note-btn='minus']"));
        Assert.True(minusBtns.Count > 0, "Minus button should be present in truncated state after dblclick with headers hidden");
    }

    [Fact]
    public void Headers_hidden_dblclick_from_truncated_goes_to_collapsed()
    {
        NavigateAndSetup("HdrHide_DblClickTruncToColl.html");
        SetScenarioState("truncated");
        ToggleHeadersHidden();

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        // Double-click first note: truncated → collapsed
        DoubleClickFirstNoteAndWait();

        // Plus count should increase (new plus for collapsed first note)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }

    [Fact]
    public void Headers_hidden_dblclick_from_collapsed_goes_to_truncated()
    {
        NavigateAndSetup("HdrHide_DblClickCollToTrunc.html");
        SetScenarioState("collapsed");
        ToggleHeadersHidden();

        // Wait for both notes to show plus buttons
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
    public void Headers_hidden_full_3_state_cycle()
    {
        NavigateAndSetup("HdrHide_Full3StateCycle.html");
        SetScenarioState("expanded");
        ToggleHeadersHidden();

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

    // ═══════════════════════════════════════════════════════════
    // Button clicks with headers hidden
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Headers_hidden_plus_button_from_collapsed_goes_to_truncated()
    {
        NavigateAndSetup("HdrHide_PlusCollToTrunc.html");
        SetScenarioState("collapsed");
        ToggleHeadersHidden();

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

    [Fact]
    public void Headers_hidden_minus_button_from_expanded_goes_to_collapsed()
    {
        NavigateAndSetup("HdrHide_MinusExpToColl.html");
        SetScenarioState("expanded");
        ToggleHeadersHidden();

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        ClickNoteButton("[data-note-btn='minus']");

        // Plus count should increase (expanded→collapsed)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }

    [Fact]
    public void Headers_hidden_minus_button_from_truncated_goes_to_collapsed()
    {
        NavigateAndSetup("HdrHide_MinusTruncToColl.html");
        SetScenarioState("truncated");
        ToggleHeadersHidden();

        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;

        ClickNoteButton("[data-note-btn='minus']");

        // Plus count should increase (truncated→collapsed)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }

    // ═══════════════════════════════════════════════════════════
    // Up arrow with headers hidden
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Headers_hidden_up_arrow_visible_when_expanded()
    {
        NavigateAndSetup("HdrHide_UpArrowVisible.html");
        SetScenarioState("expanded");
        ToggleHeadersHidden();

        HoverNoteRect(0);

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
    public void Headers_hidden_up_arrow_click_goes_to_truncated()
    {
        NavigateAndSetup("HdrHide_UpArrowToTrunc.html");
        SetScenarioState("expanded");
        ToggleHeadersHidden();

        var htmlBefore = GetSvgHtml();
        HoverNoteRect(0);

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

    // ═══════════════════════════════════════════════════════════
    // Headers toggle mid-interaction
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Hiding_headers_after_manual_note_state_change_preserves_state()
    {
        NavigateAndSetup("HdrHide_PreservesManualState.html");
        SetScenarioState("expanded");

        // Manually collapse first note via dblclick (expanded → truncated)
        DoubleClickFirstNoteAndWait();

        var minusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='minus']")).Count;

        // Now hide headers — first note should STAY truncated (minus visible)
        ToggleHeadersHidden();

        var minusAfter = _driver.FindElements(By.CssSelector("[data-note-btn='minus']")).Count;
        Assert.True(minusAfter > 0,
            "Minus button should still be present after hiding headers (note stayed truncated)");
    }

    [Fact]
    public void Showing_headers_after_hidden_preserves_note_button_behavior()
    {
        NavigateAndSetup("HdrHide_ShowAfterHideWorks.html");
        SetScenarioState("expanded");
        ToggleHeadersHidden();

        // Dblclick first note with headers hidden: expanded → truncated
        DoubleClickFirstNoteAndWait();

        // Now show headers again
        ToggleHeadersShown();

        // Note should still be truncated — verify buttons still work by dblclicking again
        // truncated → collapsed
        var plusBefore = _driver.FindElements(By.CssSelector("[data-note-btn='plus']")).Count;
        DoubleClickFirstNoteAndWait();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector("[data-note-btn='plus']")).Count > plusBefore);
    }
}
