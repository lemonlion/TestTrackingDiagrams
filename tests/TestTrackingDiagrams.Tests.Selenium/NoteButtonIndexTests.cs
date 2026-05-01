using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Tests that note hover buttons affect the correct note when some notes become empty
/// (e.g., header-only notes with "Headers: Hidden"). Verifies that the index mapping
/// between SVG note groups and PlantUML source note blocks remains aligned.
/// </summary>
public class NoteButtonIndexTests : IClassFixture<ChromeFixture>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(NoteButtonIndexTests).Assembly.Location)!,
        "SeleniumOutput");

    public NoteButtonIndexTests(ChromeFixture chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-noteidx-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private void ExpandAndRender(string url)
    {
        _driver.Navigate().GoToUrl(url);
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d => d.FindElement(By.CssSelector("details.feature")));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();

        // Force rendering
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(document.body);");

        // Wait for SVG + note toggle icons
        var renderWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        renderWait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                if (!svg.Displayed) return false;
                var icons = svg.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Count > 0;
            }
            catch (NoSuchElementException) { return false; }
        });
    }

    private void HideHeaders()
    {
        // Click the "Hidden" headers radio button
        var btn = _driver.FindElement(By.CssSelector(".headers-radio-btn[data-hstate='hidden']"));
        btn.Click();

        // Wait for re-render
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(d =>
        {
            try
            {
                var icons = d.FindElements(By.CssSelector("[data-diagram-type='plantuml'] svg .note-toggle-icon"));
                return icons.Count > 0;
            }
            catch { return false; }
        });
    }

    private int GetNoteGroupCount()
    {
        var result = ((IJavaScriptExecutor)_driver).ExecuteScript("""
            var container = document.querySelector('[data-plantuml]');
            var svg = container.querySelector('svg');
            return window._findNoteGroups(svg).length;
            """);
        return Convert.ToInt32(result);
    }

    private int GetNoteBlockCount()
    {
        var result = ((IJavaScriptExecutor)_driver).ExecuteScript("""
            var container = document.querySelector('[data-plantuml]');
            return window._parseNoteBlocks(container._noteOriginalSource).length;
            """);
        return Convert.ToInt32(result);
    }

    private void ClickMinusButtonOnNote(int noteHoverRectIndex)
    {
        // Hover to show buttons
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var rects = d.FindElements(By.CssSelector(".note-hover-rect"));
                if (rects.Count <= noteHoverRectIndex) return false;
                new Actions(_driver).MoveToElement(rects[noteHoverRectIndex]).Perform();
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Wait for buttons to appear
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
            return icons.Any(i =>
            {
                try { return i.GetCssValue("opacity") == "1"; }
                catch { return false; }
            });
        });

        // Click the minus button using JS (to avoid interception by other SVG elements)
        ((IJavaScriptExecutor)_driver).ExecuteScript("""
            var icons = document.querySelectorAll('.note-toggle-icon[data-note-btn="minus"]');
            var visible = Array.from(icons).filter(i => i.style.opacity === '1');
            if (visible.length > 0) {
                var btn = visible[0].querySelector('rect');
                btn.dispatchEvent(new MouseEvent('click', {bubbles: true}));
            }
            """);
    }

    private void ClickPlusButtonOnNote(int noteHoverRectIndex)
    {
        // Hover to show buttons
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var rects = d.FindElements(By.CssSelector(".note-hover-rect"));
                if (rects.Count <= noteHoverRectIndex) return false;
                new Actions(_driver).MoveToElement(rects[noteHoverRectIndex]).Perform();
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Wait for plus button to appear
        wait.Until(d =>
        {
            var icons = d.FindElements(By.CssSelector(".note-toggle-icon[data-note-btn='plus']"));
            return icons.Any(i =>
            {
                try { return i.GetCssValue("opacity") == "1"; }
                catch { return false; }
            });
        });

        // Click the plus button using JS
        ((IJavaScriptExecutor)_driver).ExecuteScript("""
            var icons = document.querySelectorAll('.note-toggle-icon[data-note-btn="plus"]');
            var visible = Array.from(icons).filter(i => i.style.opacity === '1');
            if (visible.length > 0) {
                var btn = visible[0].querySelector('rect');
                btn.dispatchEvent(new MouseEvent('click', {bubbles: true}));
            }
            """);
    }

    private int GetNoteStepForSourceIndex(int sourceIndex)
    {
        var result = ((IJavaScriptExecutor)_driver).ExecuteScript($"""
            var container = document.querySelector('[data-plantuml]');
            return (container._noteSteps && container._noteSteps[{sourceIndex}]) || 0;
            """);
        return Convert.ToInt32(result);
    }

    private void WaitForReRender()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                var icons = svg.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Count > 0;
            }
            catch { return false; }
        });
    }

    // ═══════════════════════════════════════════════════════════
    // Index alignment: header-only note becomes empty when headers hidden
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Note_groups_and_blocks_match_with_headers_visible()
    {
        var url = ReportTestHelper.GenerateReportWithHeaderOnlyNotes(_tempDir, OutputDir, "NoteIdxVisible.html");
        ExpandAndRender(url);

        var groups = GetNoteGroupCount();
        var blocks = GetNoteBlockCount();

        // With headers visible, all 3 notes should be detected in SVG
        Assert.Equal(blocks, groups);
    }

    [Fact]
    public void Note_groups_and_blocks_match_after_hiding_headers()
    {
        var url = ReportTestHelper.GenerateReportWithHeaderOnlyNotes(_tempDir, OutputDir, "NoteIdxHidden.html");
        ExpandAndRender(url);
        HideHeaders();

        var groups = GetNoteGroupCount();
        var blocks = GetNoteBlockCount();

        // After hiding headers, note 1 (all-gray) should still be detected as a group
        // (even though its visual content is minimal) to maintain index alignment
        Assert.Equal(blocks, groups);
    }

    [Fact]
    public void Collapse_button_affects_correct_note_after_hiding_headers()
    {
        var url = ReportTestHelper.GenerateReportWithHeaderOnlyNotes(_tempDir, OutputDir, "NoteIdxCollapse.html");
        ExpandAndRender(url);

        // First set all notes to expanded
        var expandBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='expanded']"));
        expandBtn.Click();
        WaitForReRender();

        // Hide headers — note 1 (all-gray) gets a placeholder but remains as a hover rect
        HideHeaders();

        // All 3 hover rects still exist (fix ensures empty notes keep a placeholder)
        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(hoverRects.Count >= 3, $"Expected at least 3 hover rects, got {hoverRects.Count}");

        // Click minus on hover rect index 2 (note 3 = response note)
        ClickMinusButtonOnNote(2);
        WaitForReRender();

        // Verify: note 3 (source index 2) should be collapsed, note 2 (source index 1) should NOT be
        var note2Step = GetNoteStepForSourceIndex(1); // SQL note
        var note3Step = GetNoteStepForSourceIndex(2); // response note

        Assert.NotEqual(0, note2Step); // note 2 should still be expanded (step 2)
        Assert.Equal(0, note3Step);    // note 3 should be collapsed (step 0)
    }

    [Fact]
    public void Multiple_header_only_notes_maintain_correct_index_mapping()
    {
        var url = ReportTestHelper.GenerateReportWithMultipleHeaderOnlyNotes(
            _tempDir, OutputDir, "NoteIdxMultiEmpty.html");
        ExpandAndRender(url);

        // Set to expanded first
        var expandBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='expanded']"));
        expandBtn.Click();
        WaitForReRender();

        // Hide headers — notes 1 and 3 (all-gray) become empty
        HideHeaders();

        var groups = GetNoteGroupCount();
        var blocks = GetNoteBlockCount();

        // All 4 notes should still be tracked (even if some are empty)
        Assert.Equal(blocks, groups);
    }

    [Fact]
    public void Collapse_last_content_note_with_multiple_empty_notes_before_it()
    {
        var url = ReportTestHelper.GenerateReportWithMultipleHeaderOnlyNotes(
            _tempDir, OutputDir, "NoteIdxMultiCollapse.html");
        ExpandAndRender(url);

        // Set to expanded first
        var expandBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='expanded']"));
        expandBtn.Click();
        WaitForReRender();

        // Hide headers
        HideHeaders();

        // The last note (source index 3) has body content.
        // Get hover rects and click minus on the last one.
        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        var lastIdx = hoverRects.Count - 1;
        Assert.True(lastIdx >= 0, "Expected at least 1 hover rect");

        ClickMinusButtonOnNote(lastIdx);
        WaitForReRender();

        // Verify: the LAST source note (index 3) should be collapsed
        var lastNoteStep = GetNoteStepForSourceIndex(3);
        Assert.Equal(0, lastNoteStep);

        // Other notes should NOT be collapsed by this action
        var note2Step = GetNoteStepForSourceIndex(1); // has body content
        Assert.NotEqual(0, note2Step);
    }

    [Fact]
    public void Collapsed_header_only_note_still_maintains_index_alignment()
    {
        var url = ReportTestHelper.GenerateReportWithHeaderOnlyNotes(_tempDir, OutputDir, "NoteIdxCollapsedGray.html");
        ExpandAndRender(url);

        // Collapse all notes
        var collapseBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='collapsed']"));
        collapseBtn.Click();
        WaitForReRender();

        // Note 1 (all-gray) when collapsed has no preview text (getNotePreview returns empty)
        // So it becomes empty in the rendered PlantUML
        var groups = GetNoteGroupCount();
        var blocks = GetNoteBlockCount();

        Assert.Equal(blocks, groups);
    }

    [Fact]
    public void Expand_specific_note_after_collapse_with_header_only_notes()
    {
        var url = ReportTestHelper.GenerateReportWithHeaderOnlyNotes(_tempDir, OutputDir, "NoteIdxExpandAfter.html");
        ExpandAndRender(url);

        // Collapse all
        var collapseBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='collapsed']"));
        collapseBtn.Click();
        WaitForReRender();

        // Now click plus on the first visible hover rect (should be note 2 if index is correct)
        var hoverRects = _driver.FindElements(By.CssSelector(".note-hover-rect"));
        Assert.True(hoverRects.Count >= 2, $"Expected at least 2 hover rects, got {hoverRects.Count}");

        ClickPlusButtonOnNote(0);
        WaitForReRender();

        // If index mapping is correct, the first hover rect corresponds to source note index 0
        // (the header-only note). Expanding it should change step for index 0.
        // However if alignment is wrong, it might change index 1 or 2.
        var note0Step = GetNoteStepForSourceIndex(0);
        var note1Step = GetNoteStepForSourceIndex(1);

        // note 0 should be expanded (step 2) since we clicked plus on first hover rect
        Assert.Equal(2, note0Step);
        // note 1 should still be collapsed (step 0) — not affected
        Assert.Equal(0, note1Step);
    }

    [Fact]
    public void Double_click_note_cycles_correct_note_after_headers_hidden()
    {
        var url = ReportTestHelper.GenerateReportWithHeaderOnlyNotes(_tempDir, OutputDir, "NoteIdxDblClick.html");
        ExpandAndRender(url);

        // Set to expanded
        var expandBtn = _driver.FindElement(By.CssSelector(
            ".diagram-toggle .details-radio-btn[data-state='expanded']"));
        expandBtn.Click();
        WaitForReRender();

        // Hide headers
        HideHeaders();

        // Wait for 3 hover rects to exist
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count >= 3);

        // Double-click the third hover rect (note 3 = response note, index 2) via JS dispatch
        ((IJavaScriptExecutor)_driver).ExecuteScript("""
            var rects = document.querySelectorAll('.note-hover-rect');
            if (rects.length >= 3) {
                rects[2].dispatchEvent(new MouseEvent('dblclick', {bubbles: true}));
            }
            """);
        WaitForReRender();

        // The double-click should cycle note at source index 2 (the third note — response)
        // It should NOT cycle the wrong note due to index misalignment
        var note1Step = GetNoteStepForSourceIndex(1); // SQL note — should be unchanged
        var note2Step = GetNoteStepForSourceIndex(2); // response note — should be cycled

        // After double-click from expanded (step 2), short note cycles to collapsed (step 0)
        Assert.Equal(2, note1Step); // unchanged
        Assert.Equal(0, note2Step); // cycled
    }
}
