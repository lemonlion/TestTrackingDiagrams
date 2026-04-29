using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DiagramNoteLongNoteTests : DiagramNoteTestBase
{
    public DiagramNoteLongNoteTests(ChromeFixture chrome) : base(chrome, "ttd-notes-long-") { }

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
}
