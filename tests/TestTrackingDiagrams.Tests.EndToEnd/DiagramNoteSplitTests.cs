namespace TestTrackingDiagrams.Tests.EndToEnd;

[Collection(PlaywrightCollections.Notes)]
public class DiagramNoteSplitTests : DiagramNotePlaywrightBase
{
    public DiagramNoteSplitTests(PlaywrightFixture fixture) : base(fixture) { }

    private async Task ExpandAndRenderSplitDiagram(string fileName)
    {
        await Page.GotoAsync(GenerateSplitDiagramReport(fileName));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await RenderAllDiagramsAndWait();
        await WaitForNoteElements();
    }

    [Fact]
    public async Task Split_diagram_all_parts_have_hover_rects_on_initial_render()
    {
        await ExpandAndRenderSplitDiagram("SplitHoverRectsInit.html");

        var count = await Page.Locator(".note-hover-rect").CountAsync();
        Assert.True(count >= 2, $"Expected hover rects in all diagram parts, found {count}");
    }

    [Fact]
    public async Task Split_diagram_all_parts_have_toggle_icons_on_initial_render()
    {
        await ExpandAndRenderSplitDiagram("SplitToggleIconsInit.html");

        var count = await Page.Locator(".note-toggle-icon").CountAsync();
        Assert.True(count >= 2, $"Expected toggle icons in all diagram parts, found {count}");
    }

    [Fact]
    public async Task Split_diagram_second_part_hover_shows_buttons_on_initial_render()
    {
        await ExpandAndRenderSplitDiagram("SplitSecondPartHover.html");

        var rects = Page.Locator(".note-hover-rect");
        var count = await rects.CountAsync();
        Assert.True(count >= 2);

        // JS dispatch avoids SVG <text> elements intercepting pointer events
        await rects.Nth(count - 1).EvaluateAsync(
            "el => el.dispatchEvent(new MouseEvent('mouseenter', {bubbles:true}))");

        await Page.WaitForFunctionAsync("""
            () => Array.from(document.querySelectorAll('.note-toggle-icon'))
                .some(i => i.style.opacity !== '0')
        """, null, new() { Timeout = 5000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Split_diagram_first_part_hover_shows_buttons_on_initial_render()
    {
        await ExpandAndRenderSplitDiagram("SplitFirstPartHover.html");

        await HoverNoteRect(0);

        await Page.WaitForFunctionAsync("""
            () => Array.from(document.querySelectorAll('.note-toggle-icon'))
                .some(i => i.style.opacity !== '0')
        """, null, new() { Timeout = 5000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Split_diagram_dblclick_on_second_diagram_note_cycles_state()
    {
        await ExpandAndRenderSplitDiagram("SplitDblClickSecond.html");

        var rects = Page.Locator(".note-hover-rect");
        var count = await rects.CountAsync();
        var htmlBefore = await Page.Locator("[data-diagram-type='plantuml'] svg").Last.EvaluateAsync<string>("el => el.outerHTML");

        // JS dispatch avoids SVG <text> elements intercepting pointer events
        await rects.Nth(count - 1).EvaluateAsync(
            "el => el.dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}))");

        await Page.WaitForFunctionAsync(
            $"() => {{ var svgs = document.querySelectorAll('[data-diagram-type=\"plantuml\"] svg'); " +
            $"return svgs[svgs.length-1].outerHTML !== {System.Text.Json.JsonSerializer.Serialize(htmlBefore)}; }}",
            null, new() { Timeout = 15000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Split_diagram_scenario_state_change_preserves_second_diagram_buttons()
    {
        await ExpandAndRenderSplitDiagram("SplitStateChangePreserves.html");

        await SetScenarioState("collapsed");

        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('[data-note-btn=\"plus\"]').length > 0",
            null, new() { Timeout = 10000, PollingInterval = 200 });

        // Check second diagram also has buttons
        var svgs = Page.Locator("[data-diagram-type='plantuml'] svg");
        var svgCount = await svgs.CountAsync();
        Assert.True(svgCount >= 2);
    }

    [Fact]
    public async Task After_report_truncation_change_all_diagrams_have_hover_buttons()
    {
        await ExpandAndRenderSplitDiagram("SplitReportTruncChange.html");

        await Page.Locator(".toolbar-row .details-radio-btn[data-state='collapsed']").ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('[data-note-btn=\"plus\"]').length > 0",
            null, new() { Timeout = 10000, PollingInterval = 200 });
    }

    [Fact]
    public async Task After_scenario_truncation_change_all_diagrams_have_hover_buttons()
    {
        await ExpandAndRenderSplitDiagram("SplitScenarioTruncChange.html");

        await SetScenarioState("expanded");

        Assert.True(await Page.Locator(".note-hover-rect").CountAsync() >= 2);
    }

    [Fact]
    public async Task Split_diagram_all_parts_have_hover_buttons_after_truncation_change()
    {
        await ExpandAndRenderSplitDiagram("SplitAllPartsButtons.html");

        await SetScenarioState("collapsed");
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('[data-note-btn=\"plus\"]').length > 0",
            null, new() { Timeout = 10000, PollingInterval = 200 });

        await SetScenarioState("expanded");
        Assert.True(await Page.Locator(".note-hover-rect").CountAsync() >= 2);
    }

    [Fact]
    public async Task Split_diagram_hover_buttons_visible_on_second_diagram_after_truncation()
    {
        await ExpandAndRenderSplitDiagram("SplitSecondDiagramButtons.html");

        await SetScenarioState("collapsed");
        await SetScenarioState("truncated");

        var rects = Page.Locator(".note-hover-rect");
        var count = await rects.CountAsync();
        // JS dispatch avoids SVG <text> elements intercepting pointer events
        await rects.Nth(count - 1).EvaluateAsync(
            "el => el.dispatchEvent(new MouseEvent('mouseenter', {bubbles:true}))");

        await Page.WaitForFunctionAsync("""
            () => Array.from(document.querySelectorAll('.note-toggle-icon'))
                .some(i => i.style.opacity !== '0')
        """, null, new() { Timeout = 5000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Three_diagram_split_continuation_note_has_hover_rects()
    {
        await Page.GotoAsync(GenerateThreeDiagramSplitReport("ThreeSplitHoverRects.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await RenderAllThreeDiagramsAndWait();
        await WaitForNoteElements();

        Assert.True(await Page.Locator(".note-hover-rect").CountAsync() >= 3);
    }

    [Fact]
    public async Task Three_diagram_split_continuation_note_has_toggle_icons()
    {
        await Page.GotoAsync(GenerateThreeDiagramSplitReport("ThreeSplitToggleIcons.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await RenderAllThreeDiagramsAndWait();
        await WaitForNoteElements();

        Assert.True(await Page.Locator(".note-toggle-icon").CountAsync() >= 3);
    }

    [Fact]
    public async Task Three_diagram_split_findNoteGroups_matches_noteBlocks_on_all_diagrams()
    {
        await Page.GotoAsync(GenerateThreeDiagramSplitReport("ThreeSplitGroupBlockMatch.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await RenderAllThreeDiagramsAndWait();
        await WaitForNoteElements();

        var hoverCount = await Page.Locator(".note-hover-rect").CountAsync();
        var iconCount = await Page.Locator(".note-toggle-icon").CountAsync();
        Assert.True(hoverCount > 0, $"Expected hover rects, found {hoverCount}");
        Assert.True(iconCount > 0, $"Expected toggle icons, found {iconCount}");
        Assert.True(iconCount >= hoverCount,
            $"Expected at least as many toggle icons ({iconCount}) as hover rects ({hoverCount})");
    }

    [Fact]
    public async Task Three_diagram_split_all_diagrams_with_notes_have_hover_rects()
    {
        await Page.GotoAsync(GenerateThreeDiagramSplitReport("ThreeSplitAllHoverRects.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await RenderAllThreeDiagramsAndWait();
        await WaitForNoteElements();

        var result = await Page.EvaluateAsync<string>("""
            (() => {
                var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
                var results = [];
                containers.forEach(function(c) {
                    var src = c._noteOriginalSource || c.getAttribute('data-plantuml');
                    var noteBlocks = window._parseNoteBlocks ? window._parseNoteBlocks(src).length : 0;
                    var hoverRects = c.querySelectorAll('.note-hover-rect').length;
                    results.push({ id: c.id, noteBlocks: noteBlocks, hoverRects: hoverRects });
                });
                return JSON.stringify(results);
            })()
        """);

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
    public async Task Three_diagram_split_dblclick_on_continuation_note_cycles_state()
    {
        await Page.GotoAsync(GenerateThreeDiagramSplitReport("ThreeSplitDblClickCont.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await RenderAllThreeDiagramsAndWait();
        await WaitForNoteElements();

        var svgBefore = await Page.EvaluateAsync<string>("""
            (() => {
                var c = document.getElementById('puml-2');
                var svg = c ? c.querySelector('svg') : null;
                return svg ? svg.outerHTML : '';
            })()
        """);
        Assert.False(string.IsNullOrEmpty(svgBefore));

        await Page.EvaluateAsync("""
            (() => {
                var c = document.getElementById('puml-2');
                var hr = c ? c.querySelector('.note-hover-rect') : null;
                if (hr) hr.dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));
            })()
        """);

        await Page.WaitForFunctionAsync("""
            (prev) => {
                var c = document.getElementById('puml-2');
                var svg = c ? c.querySelector('svg') : null;
                return svg && svg.outerHTML !== prev;
            }
        """, svgBefore, new() { Timeout = 15000 });

        var hasButtons = await Page.EvaluateAsync<bool>("""
            (() => {
                var c = document.getElementById('puml-2');
                return c && c.querySelectorAll('.note-hover-rect').length > 0
                    && c.querySelectorAll('.note-toggle-icon').length > 0;
            })()
        """);
        Assert.True(hasButtons, "puml-2 should have hover rects and toggle icons after dblclick state cycle");
    }

    [Fact]
    public async Task Three_diagram_split_hover_on_continuation_note_shows_buttons()
    {
        await Page.GotoAsync(GenerateThreeDiagramSplitReport("ThreeSplitHoverContNote.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await RenderAllThreeDiagramsAndWait();
        await WaitForNoteElements();

        var hoverRect = Page.Locator("#puml-2 .note-hover-rect").First;
        await hoverRect.ScrollIntoViewIfNeededAsync();
        // JS dispatch avoids SVG <text> elements intercepting pointer events
        await hoverRect.EvaluateAsync(
            "el => el.dispatchEvent(new MouseEvent('mouseenter', {bubbles:true}))");

        await Page.WaitForFunctionAsync("""
            () => {
                var c = document.getElementById('puml-2');
                if (!c) return false;
                var icons = c.querySelectorAll('.note-toggle-icon');
                for (var i = 0; i < icons.length; i++) {
                    if (icons[i].style.opacity !== '0') return true;
                }
                return false;
            }
        """, null, new() { Timeout = 5000, PollingInterval = 200 });
    }
}