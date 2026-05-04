namespace TestTrackingDiagrams.Tests.EndToEnd;

/// <summary>
/// Tests that verify note CONTENT actually changes when buttons are clicked,
/// not just that the button icons change. Covers edge cases around
/// state consistency between _noteSteps and the rendered SVG.
/// </summary>
public class DiagramNoteContentTests : DiagramNotePlaywrightBase
{
    public DiagramNoteContentTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Minus_button_reduces_text_element_count()
    {
        await ExpandAndRenderLongNoteDiagram("MinusReducesText.html");
        await SetScenarioState("truncated");

        var textCountBefore = await GetSvgTextCount();

        await ClickNoteButton("[data-note-btn='minus']");

        var textCountAfter = await GetSvgTextCount();
        Assert.True(textCountAfter < textCountBefore,
            $"Text element count should decrease after collapse. Before: {textCountBefore}, After: {textCountAfter}");
    }

    [Fact]
    public async Task Minus_from_expanded_reduces_text_element_count()
    {
        await ExpandAndRenderLongNoteDiagram("MinusExpReducesText.html");
        await SetScenarioState("expanded");

        var textCountBefore = await GetSvgTextCount();

        await ClickNoteButton("[data-note-btn='minus']");

        var textCountAfter = await GetSvgTextCount();
        Assert.True(textCountAfter < textCountBefore,
            $"Text element count should decrease after collapse. Before: {textCountBefore}, After: {textCountAfter}");
    }

    [Fact]
    public async Task Minus_button_changes_data_plantuml_source()
    {
        await ExpandAndRenderLongNoteDiagram("MinusChangesSource.html");
        await SetScenarioState("truncated");

        var sourceBefore = await GetDataPlantuml();

        await ClickNoteButton("[data-note-btn='minus']");

        var sourceAfter = await GetDataPlantuml();
        Assert.NotEqual(sourceBefore, sourceAfter);
    }

    [Fact]
    public async Task Plus_button_increases_text_element_count()
    {
        await ExpandAndRenderLongNoteDiagram("PlusIncreasesText.html");
        await SetScenarioState("collapsed");

        await Page.Locator("[data-note-btn='plus']").First.WaitForAsync(new() { Timeout = 10000 });
        var textCountBefore = await GetSvgTextCount();

        await ClickNoteButton("[data-note-btn='plus']");

        var textCountAfter = await GetSvgTextCount();
        Assert.True(textCountAfter > textCountBefore,
            $"Text element count should increase after expand. Before: {textCountBefore}, After: {textCountAfter}");
    }

    [Fact]
    public async Task Collapsed_note_shows_preview_text()
    {
        await ExpandAndRenderLongNoteDiagram("CollapsedPreview.html");
        await SetScenarioState("collapsed");

        // After collapsing, the note should have a short preview text
        var previewVisible = await Page.EvaluateAsync<bool>("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                var source = container.getAttribute('data-plantuml');
                // Collapsed notes in PlantUML source should NOT contain all 45 lines
                var lineCount = (source.match(/^Line \d+/gm) || []).length;
                return lineCount < 5;
            }
        """);
        Assert.True(previewVisible, "Collapsed source should not contain all note content lines");
    }

    [Fact]
    public async Task NoteSteps_matches_visual_state_after_collapse()
    {
        await ExpandAndRenderLongNoteDiagram("NoteStepsMatch.html");
        await SetScenarioState("truncated");

        await ClickNoteButton("[data-note-btn='minus']");

        // Verify that _noteSteps[0] is 0 (collapsed) AND plus button exists.
        // The second note (short, index 1) may still show minus, so check
        // that the first note specifically is collapsed.
        var stateConsistent = await Page.EvaluateAsync<bool>("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                var step = container._noteSteps && container._noteSteps[0];
                var hasPlus = container.querySelector('[data-note-btn="plus"]') !== null;
                return step === 0 && hasPlus;
            }
        """);
        Assert.True(stateConsistent, "_noteSteps[0] should be 0 with plus button after collapse");
    }

    [Fact]
    public async Task NoteSteps_matches_visual_state_after_expand()
    {
        await ExpandAndRenderLongNoteDiagram("NoteStepsMatchExpand.html");
        await SetScenarioState("collapsed");

        await Page.Locator("[data-note-btn='plus']").First.WaitForAsync(new() { Timeout = 10000 });
        await ClickNoteButton("[data-note-btn='plus']");

        // For long notes, plus→expand goes to truncated (step 1)
        var stateConsistent = await Page.EvaluateAsync<bool>("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                var step = container._noteSteps && container._noteSteps[0];
                var hasMinus = container.querySelector('[data-note-btn="minus"]') !== null;
                return step === 1 && hasMinus;
            }
        """);
        Assert.True(stateConsistent, "_noteSteps should be 1 with minus button after expand from collapsed (long note)");
    }

    [Fact]
    public async Task Rendering_flags_are_clear_after_collapse()
    {
        await ExpandAndRenderLongNoteDiagram("FlagsClear.html");
        await SetScenarioState("truncated");

        await ClickNoteButton("[data-note-btn='minus']");

        var flagsClear = await Page.EvaluateAsync<bool>("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                return !container._noteRendering && !window._plantumlRendering;
            }
        """);
        Assert.True(flagsClear, "Rendering flags should be clear after collapse completes");
    }

    [Fact]
    public async Task Minus_twice_quickly_still_collapses()
    {
        await ExpandAndRenderLongNoteDiagram("MinusTwice.html");
        await SetScenarioState("truncated");

        var htmlBefore = await GetSvgHtml();
        await HoverNoteRect(0);
        await Page.Locator("[data-note-btn='minus']").First.WaitForAsync(
            new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        // Dispatch two rapid clicks
        await Page.EvaluateAsync("""
            () => {
                var btn = document.querySelector('[data-note-btn="minus"] rect');
                btn.dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));
                btn.dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));
            }
        """);

        await WaitForSvgReRender(htmlBefore);

        // Should end up collapsed with plus button
        await Page.WaitForFunctionAsync("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                return container._noteSteps[0] === 0
                    && container.querySelector('[data-note-btn="plus"]') !== null;
            }
        """, null, new() { Timeout = 10000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Collapse_and_expand_cycle_is_idempotent()
    {
        await ExpandAndRenderLongNoteDiagram("CollapseExpandCycle.html");
        await SetScenarioState("truncated");

        var textCountOriginal = await GetSvgTextCount();

        // Collapse
        await ClickNoteButton("[data-note-btn='minus']");
        var textCountCollapsed = await GetSvgTextCount();
        Assert.True(textCountCollapsed < textCountOriginal, "Should have fewer text elements when collapsed");

        // Expand (from collapsed, long note → truncated)
        await ClickNoteButton("[data-note-btn='plus']");
        var textCountExpanded = await GetSvgTextCount();
        Assert.True(textCountExpanded > textCountCollapsed, "Should have more text elements after expand");
    }

    [Fact]
    public async Task Collapse_expand_collapse_produces_consistent_state()
    {
        await ExpandAndRenderLongNoteDiagram("CollapseExpandCollapse.html");
        await SetScenarioState("truncated");

        // First collapse
        await ClickNoteButton("[data-note-btn='minus']");
        var source1 = await GetDataPlantuml();
        var textCount1 = await GetSvgTextCount();

        // Expand
        await ClickNoteButton("[data-note-btn='plus']");

        // Second collapse — should produce the same result as the first
        await ClickNoteButton("[data-note-btn='minus']");
        var source2 = await GetDataPlantuml();
        var textCount2 = await GetSvgTextCount();

        Assert.Equal(source1, source2);
        Assert.Equal(textCount1, textCount2);
    }

    private async Task<int> GetSvgTextCount()
    {
        return await Page.EvaluateAsync<int>("""
            () => {
                var svg = document.querySelector('[data-diagram-type="plantuml"] svg');
                return svg ? svg.querySelectorAll('text').length : 0;
            }
        """);
    }

    private async Task<string> GetDataPlantuml()
    {
        return await Page.EvaluateAsync<string>("""
            () => document.querySelector('[data-diagram-type="plantuml"]').getAttribute('data-plantuml') || ''
        """);
    }
}
