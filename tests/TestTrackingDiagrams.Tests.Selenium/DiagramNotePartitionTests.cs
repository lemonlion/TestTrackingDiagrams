using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DiagramNotePartitionTests : DiagramNoteTestBase
{
    public DiagramNotePartitionTests(ChromeFixture chrome) : base(chrome, "ttd-notes-part-") { }

    // ── Partition (SeparateSetup) note buttons ──

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
}
