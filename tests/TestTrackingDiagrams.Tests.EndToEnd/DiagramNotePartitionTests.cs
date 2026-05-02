namespace TestTrackingDiagrams.Tests.EndToEnd;

public class DiagramNotePartitionTests : DiagramNotePlaywrightBase
{
    public DiagramNotePartitionTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Note_hover_rects_found_inside_partition_groups()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionNoteHoverRects.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.note-hover-rect').length >= 3",
            null, new() { Timeout = 10000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Note_toggle_icons_found_inside_partition_groups()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionNoteToggleIcons.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.note-toggle-icon').length >= 3",
            null, new() { Timeout = 10000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Partition_note_buttons_respond_to_hover()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionNoteHover.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        await HoverNoteRect(0);

        await Page.WaitForFunctionAsync("""
            () => Array.from(document.querySelectorAll('.note-toggle-icon'))
                .some(i => i.style.opacity !== '0')
        """, null, new() { Timeout = 5000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Partition_note_double_click_cycles_state()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionNoteDblClick.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        await DoubleClickFirstNoteAndWait();
    }

    [Fact]
    public async Task Partition_note_scenario_collapse_shows_plus_buttons()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionNoteCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        await SetScenarioState("collapsed");

        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('[data-note-btn=\"plus\"]').length > 0",
            null, new() { Timeout = 10000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Partition_svg_structure_has_expected_note_groups()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionSvgStructure.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        var noteGroupCount = await Page.EvaluateAsync<int>("""
            () => {
                var svg = document.querySelector('[data-diagram-type="plantuml"] svg');
                return svg ? svg.querySelectorAll('.note-hover-rect').length : 0;
            }
        """);
        Assert.True(noteGroupCount >= 3);
    }

    [Fact]
    public async Task Partition_long_notes_have_hover_rects()
    {
        await Page.GotoAsync(GeneratePartitionLongNoteReport("PartitionLongNoteHoverRects.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        Assert.True(await Page.Locator(".note-hover-rect").CountAsync() >= 2);
    }

    [Fact]
    public async Task Partition_long_note_double_click_cycles_state()
    {
        await Page.GotoAsync(GeneratePartitionLongNoteReport("PartitionLongNoteDblClick.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        await SetScenarioState("expanded");
        await DoubleClickFirstNoteAndWait();
    }

    [Fact]
    public async Task Partition_long_note_expand_click_works()
    {
        await Page.GotoAsync(GeneratePartitionLongNoteReport("PartitionLongNoteExpand.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        await SetScenarioState("collapsed");
        await Page.Locator("[data-note-btn='plus']").First.WaitForAsync(new() { Timeout = 10000 });

        var minusBefore = await Page.Locator("[data-note-btn='minus']").CountAsync();
        await ClickNoteButton("[data-note-btn='plus']");

        await Page.WaitForFunctionAsync(
            $"() => document.querySelectorAll('[data-note-btn=\"minus\"]').length > {minusBefore}",
            null, new() { Timeout = 10000, PollingInterval = 200 });
    }

    [Fact]
    public async Task FindNoteGroups_excludes_participant_fill_E2E2F0()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionExcludeParticipant.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        var result = await Page.EvaluateAsync<System.Text.Json.JsonElement>("""
            (() => {
                var svg = document.querySelector('[data-diagram-type="plantuml"] svg');
                var mainG = null;
                for (var i = 0; i < svg.children.length; i++) {
                    if (svg.children[i].tagName === 'g') { mainG = svg.children[i]; break; }
                }
                var SVGNS = 'http://www.w3.org/2000/svg';
                var originalCount = window._findNoteGroups(svg).length;

                var fakePath = document.createElementNS(SVGNS, 'path');
                fakePath.setAttribute('fill', '#E2E2F0');
                fakePath.setAttribute('d', 'M10,10 L200,10 L200,50 L10,50 Z');
                var fakeText = document.createElementNS(SVGNS, 'text');
                fakeText.textContent = 'FakeParticipant';
                fakeText.setAttribute('x', '50');
                fakeText.setAttribute('y', '30');

                var fakePath2 = document.createElementNS(SVGNS, 'path');
                fakePath2.setAttribute('fill', '#e2e2f0');
                fakePath2.setAttribute('d', 'M10,60 L200,60 L200,100 L10,100 Z');
                var fakeText2 = document.createElementNS(SVGNS, 'text');
                fakeText2.textContent = 'Setup';
                fakeText2.setAttribute('x', '50');
                fakeText2.setAttribute('y', '80');

                var firstChild = mainG.firstChild;
                mainG.insertBefore(fakeText2, firstChild);
                mainG.insertBefore(fakePath2, fakeText2);
                mainG.insertBefore(fakeText, fakePath2);
                mainG.insertBefore(fakePath, fakeText);

                var afterCount = window._findNoteGroups(svg).length;
                return { originalCount: originalCount, afterCount: afterCount };
            })()
        """);

        var originalCount = result.GetProperty("originalCount").GetInt32();
        var afterCount = result.GetProperty("afterCount").GetInt32();
        Assert.Equal(originalCount, afterCount);
    }

    [Fact]
    public async Task FindNoteGroups_still_detects_note_fill_FEFFDD()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionDetectNoteFill.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        Assert.True(await Page.Locator(".note-hover-rect").CountAsync() >= 1);
    }

    [Fact]
    public async Task MakeNotesCollapsible_matches_groups_to_blocks_correctly_when_extra_groups_exist()
    {
        await Page.GotoAsync(GeneratePartitionReport("PartitionGroupBlockMatch.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await WaitForNoteElements();

        var hoverCount = await Page.Locator(".note-hover-rect").CountAsync();
        var iconCount = await Page.Locator(".note-toggle-icon").CountAsync();
        Assert.Equal(hoverCount, iconCount);
    }
}