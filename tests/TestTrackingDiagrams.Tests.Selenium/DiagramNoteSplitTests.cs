using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DiagramNoteSplitTests : DiagramNoteTestBase
{
    public DiagramNoteSplitTests(ChromeFixture chrome) : base(chrome, "ttd-notes-split-") { }

    // ── Split-diagram initial render regression tests ──

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

        // Verify second diagram has buttons after state change
        var allContainers = _driver.FindElements(By.CssSelector("[data-diagram-type='plantuml']"));
        for (var i = 0; i < allContainers.Count; i++)
        {
            var hoverRects = allContainers[i].FindElements(By.CssSelector(".note-hover-rect"));
            var icons = allContainers[i].FindElements(By.CssSelector(".note-toggle-icon"));
            Assert.True(hoverRects.Count > 0,
                $"Diagram {i + 1} should have hover rects after 'Expanded'");
            Assert.True(icons.Count > 0,
                $"Diagram {i + 1} should have toggle icons after 'Expanded'");
        }
    }

    // ── Multi-diagram truncation hover button regression ──

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
