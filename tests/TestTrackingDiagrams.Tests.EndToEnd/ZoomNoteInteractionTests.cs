using Microsoft.Playwright;

namespace TestTrackingDiagrams.Tests.EndToEnd;

public class ZoomNoteInteractionTests : PlaywrightTestBase
{
    public ZoomNoteInteractionTests(PlaywrightFixture fixture) : base(fixture) { }

    private new string GenerateReport(string fileName) =>
        GenerateReportWithWideNoteDiagram(fileName);

    private ILocator GetDiagramContainer() =>
        Page.Locator("[data-diagram-type='plantuml']").First;

    private async Task ZoomIn()
    {
        // Wait for ALL diagram rendering to complete before interacting
        await WaitForAllDiagramsRendered();

        var container = GetDiagramContainer();
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle') !== null",
            null, new() { Timeout = 10000, PollingInterval = 200 });
        await Page.EvaluateAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle').click()");
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"]').classList.contains('diagram-natural-size')",
            null, new() { Timeout = 5000, PollingInterval = 200 });
    }

    private async Task WaitForAllDiagramsRendered()
    {
        await Page.WaitForFunctionAsync("""
            () => {
                var cs = document.querySelectorAll('[data-diagram-type="plantuml"]');
                if (cs.length === 0) return false;
                for (var i = 0; i < cs.length; i++) {
                    if (!cs[i].querySelector('svg')) return false;
                }
                return !window._plantumlRendering;
            }
        """, null, new() { Timeout = 60000, PollingInterval = 200 });
    }

    private async Task WaitForReRender(int timeoutMs = 30000)
    {
        await Page.WaitForFunctionAsync("""
            () => {
                var c = document.querySelector('[data-diagram-type="plantuml"]');
                if (!c) return false;
                var svg = c.querySelector('svg');
                if (!svg) return false;
                return !(c._noteRendering || window._plantumlRendering);
            }
        """, null, new() { Timeout = timeoutMs, PollingInterval = 200 });
    }

    private async Task ClickRadioButton(string state)
    {
        await Page.Locator($".diagram-toggle .details-radio-btn[data-state='{state}']").First.ClickAsync();
    }

    private async Task<bool> IsZoomedIn() =>
        await Page.EvaluateAsync<bool>(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"]').classList.contains('diagram-natural-size')");

    private async Task<string> GetSvgMaxWidth() =>
        await Page.EvaluateAsync<string>("""
            () => {
                var c = document.querySelector('[data-diagram-type="plantuml"]');
                var svg = c && c.querySelector('svg');
                return svg ? svg.style.maxWidth : '';
            }
        """);

    private async Task<string> GetContainerOverflow() =>
        await Page.EvaluateAsync<string>(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"]').style.overflow");

    // ── Zoom state preserved after note collapse ──

    [Fact]
    public async Task Zoom_state_preserved_after_note_collapse_via_radio()
    {
        await Page.GotoAsync(GenerateReport("ZoomPreservedAfterCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();
        Assert.True(await IsZoomedIn());
        Assert.Equal("none", await GetSvgMaxWidth());

        await ClickRadioButton("collapsed");
        await WaitForReRender();

        Assert.True(await IsZoomedIn(), "Container should still have diagram-natural-size class");
        Assert.Equal("none", await GetSvgMaxWidth());
        Assert.Equal("auto", await GetContainerOverflow());
    }

    [Fact]
    public async Task Zoom_state_preserved_after_truncation_change()
    {
        await Page.GotoAsync(GenerateReport("ZoomPreservedAfterTruncation.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();
        Assert.True(await IsZoomedIn());

        await ClickRadioButton("expanded");
        await WaitForReRender();

        Assert.True(await IsZoomedIn());
        Assert.Equal("none", await GetSvgMaxWidth());
        Assert.Equal("auto", await GetContainerOverflow());
    }

    [Fact]
    public async Task Zoom_state_preserved_after_headers_toggle()
    {
        await Page.GotoAsync(GenerateReport("ZoomPreservedAfterHeaders.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();
        Assert.True(await IsZoomedIn());

        var headersHidden = Page.Locator(".diagram-toggle .headers-radio-btn[data-hstate='hidden']");
        if (await headersHidden.CountAsync() == 0) return;
        await headersHidden.First.ClickAsync();
        await WaitForReRender(60000);

        Assert.True(await IsZoomedIn());
        Assert.Equal("none", await GetSvgMaxWidth());
    }

    // ── Zoom toggle correct after re-render ──

    [Fact]
    public async Task Zoom_toggle_out_works_correctly_after_note_collapse()
    {
        await Page.GotoAsync(GenerateReport("ZoomToggleAfterCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();
        await ClickRadioButton("collapsed");
        await WaitForReRender();

        // Toggle zoom OFF
        await Page.EvaluateAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle').click()");
        Assert.False(await IsZoomedIn(), "Should have zoomed out");
        Assert.Equal("", await GetSvgMaxWidth());
        Assert.Equal("", await GetContainerOverflow());
    }

    [Fact]
    public async Task Zoom_toggle_in_again_after_collapse_and_unzoom()
    {
        await Page.GotoAsync(GenerateReport("ZoomToggleInAgain.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();
        await ClickRadioButton("collapsed");
        await WaitForReRender();

        // Zoom out
        await Page.EvaluateAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle').click()");
        Assert.False(await IsZoomedIn());

        // Zoom in again
        await Page.EvaluateAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle').click()");
        Assert.True(await IsZoomedIn());
        Assert.Equal("none", await GetSvgMaxWidth());
    }

    // ── Note button interactions while zoomed ──

    [Fact]
    public async Task Note_collapse_button_works_while_zoomed()
    {
        await Page.GotoAsync(GenerateReport("NoteCollapseWhileZoomed.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();

        // Double-click first note to cycle state
        await Page.EvaluateAsync("""
            () => {
                var c = document.querySelector('[data-diagram-type="plantuml"]');
                var rects = c.querySelectorAll('.note-hover-rect');
                if (rects.length > 0) rects[0].dispatchEvent(new MouseEvent('dblclick', {bubbles: true}));
            }
        """);
        await WaitForReRender();

        Assert.True(await IsZoomedIn());
        Assert.Equal("none", await GetSvgMaxWidth());
    }

    [Fact]
    public async Task Multiple_note_state_changes_while_zoomed_preserves_zoom()
    {
        await Page.GotoAsync(GenerateReport("MultiNoteChangesZoomed.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();

        await ClickRadioButton("collapsed");
        await WaitForReRender();
        Assert.True(await IsZoomedIn());

        await ClickRadioButton("expanded");
        await WaitForReRender();
        Assert.True(await IsZoomedIn());

        await ClickRadioButton("truncated");
        await WaitForReRender();
        Assert.True(await IsZoomedIn());
        Assert.Equal("none", await GetSvgMaxWidth());
    }

    // ── Zoom after unzoomed state + re-render ──

    [Fact]
    public async Task Unzoomed_state_not_affected_by_note_collapse()
    {
        await Page.GotoAsync(GenerateReport("UnzoomedNotAffected.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        Assert.False(await IsZoomedIn());

        await ClickRadioButton("collapsed");
        await WaitForReRender();

        Assert.False(await IsZoomedIn());
        Assert.Equal("", await GetSvgMaxWidth());
    }

    [Fact]
    public async Task Zoom_in_works_after_unzoomed_collapse()
    {
        await Page.GotoAsync(GenerateReport("ZoomInAfterCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ClickRadioButton("collapsed");
        await WaitForReRender();

        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle') !== null",
            null, new() { Timeout = 10000, PollingInterval = 200 });
        await Page.EvaluateAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle').click()");

        Assert.True(await IsZoomedIn());
        Assert.Equal("none", await GetSvgMaxWidth());
    }

    // ── Rapid toggling ──

    [Fact]
    public async Task Rapid_zoom_toggle_produces_consistent_state()
    {
        await Page.GotoAsync(GenerateReport("RapidZoomToggle.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle') !== null",
            null, new() { Timeout = 10000, PollingInterval = 200 });

        // Toggle 4 times rapidly (should end up unzoomed)
        await Page.EvaluateAsync("""
            () => {
                var btn = document.querySelector('[data-diagram-type="plantuml"] .diagram-zoom-toggle');
                for (var i = 0; i < 4; i++) btn.click();
            }
        """);

        Assert.False(await IsZoomedIn());
        Assert.Equal("", await GetSvgMaxWidth());
    }

    [Fact]
    public async Task Zoom_icon_correct_after_collapse_re_render()
    {
        await Page.GotoAsync(GenerateReport("ZoomIconAfterCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();
        await ClickRadioButton("collapsed");
        await WaitForReRender();

        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle') !== null",
            null, new() { Timeout = 10000, PollingInterval = 200 });
        var iconText = await Page.EvaluateAsync<string>(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle').textContent");
        Assert.Equal("\u2921", iconText); // ⤡ = zoomed-in icon
    }

    [Fact]
    public async Task Zoom_icon_correct_when_unzoomed_after_collapse()
    {
        await Page.GotoAsync(GenerateReport("ZoomIconUnzoomedCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ClickRadioButton("collapsed");
        await WaitForReRender();

        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle') !== null",
            null, new() { Timeout = 10000, PollingInterval = 200 });
        var iconText = await Page.EvaluateAsync<string>(
            "() => document.querySelector('[data-diagram-type=\"plantuml\"] .diagram-zoom-toggle').textContent");
        Assert.Equal("\u2922", iconText); // ⤢ = unzoomed icon
    }

    // ── Report-level controls ──

    [Fact]
    public async Task Report_level_truncation_change_preserves_zoom()
    {
        await Page.GotoAsync(GenerateReport("ReportTruncationPreservesZoom.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();

        var reportExpandedBtn = Page.Locator(".toolbar-row .details-radio-btn[data-state='expanded']");
        if (await reportExpandedBtn.CountAsync() > 0)
        {
            await reportExpandedBtn.First.ClickAsync();
            await WaitForReRender();
            Assert.True(await IsZoomedIn());
            Assert.Equal("none", await GetSvgMaxWidth());
        }
    }

    [Fact]
    public async Task Report_level_headers_toggle_preserves_zoom()
    {
        await Page.GotoAsync(GenerateReport("ReportHeadersPreservesZoom.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await ZoomIn();

        var reportHeadersHidden = Page.Locator(".toolbar-row .headers-radio-btn[data-hstate='hidden']");
        if (await reportHeadersHidden.CountAsync() > 0)
        {
            await reportHeadersHidden.First.ClickAsync();
            await WaitForReRender(60000);
            Assert.True(await IsZoomedIn());
            Assert.Equal("none", await GetSvgMaxWidth());
        }
    }
}