using Microsoft.Playwright;

namespace TestTrackingDiagrams.Tests.EndToEnd;

public class DiagramZoomTests : PlaywrightTestBase
{
    public DiagramZoomTests(PlaywrightFixture fixture) : base(fixture) { }

    private ILocator GetDiagramContainer() =>
        Page.Locator("[data-diagram-type='plantuml']").First;

    private async Task<string> GetComputedStyle(ILocator el, string prop) =>
        await el.EvaluateAsync<string>($"(e, p) => window.getComputedStyle(e).getPropertyValue(p)", prop);

    // ── Zoom toggle button ──

    [Fact]
    public async Task Zoom_button_appears_on_diagram_container()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomButtonExists.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var count = await Page.Locator(".diagram-zoom-toggle").CountAsync();
        Assert.True(count > 0, "Zoom button should exist on diagram container");
    }

    [Fact]
    public async Task Zoom_button_is_hidden_until_hover()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomHoverHidden.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var zoomControls = Page.Locator(".diagram-zoom-controls").First;
        var opacity = await GetComputedStyle(zoomControls, "opacity");
        Assert.Equal("0", opacity);
    }

    [Fact]
    public async Task Zoom_button_becomes_visible_on_container_hover()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomHoverVisible.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var container = GetDiagramContainer();
        await container.HoverAsync();

        await Page.WaitForFunctionAsync("""
            () => {
                var btn = document.querySelector('.diagram-zoom-toggle');
                return btn && window.getComputedStyle(btn).opacity !== '0';
            }
        """, null, new() { Timeout = 3000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Clicking_zoom_button_adds_natural_size_class()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomClickNatural.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var container = GetDiagramContainer();
        await container.HoverAsync();
        await Page.WaitForTimeoutAsync(200);

        await Page.EvaluateAsync("() => document.querySelector('.diagram-zoom-toggle').click()");

        await Expect(container).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("diagram-natural-size"));
    }

    [Fact]
    public async Task Clicking_zoom_button_again_removes_natural_size_class()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomClickToggle.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var container = GetDiagramContainer();
        await container.HoverAsync();
        await Page.WaitForTimeoutAsync(200);

        await Page.EvaluateAsync("() => document.querySelector('.diagram-zoom-toggle').click()");
        await Expect(container).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("diagram-natural-size"));

        await Page.EvaluateAsync("() => document.querySelector('.diagram-zoom-toggle').click()");
        await Expect(container).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("diagram-natural-size"));
    }

    [Fact]
    public async Task Zoomed_container_has_overflow_x_auto()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomOverflow.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var container = GetDiagramContainer();
        await container.HoverAsync();
        await Page.WaitForTimeoutAsync(200);

        await Page.EvaluateAsync("() => document.querySelector('.diagram-zoom-toggle').click()");

        var overflowX = await container.EvaluateAsync<string>("e => e.style.overflowX");
        Assert.Equal("auto", overflowX);
        var overflowY = await container.EvaluateAsync<string>("e => e.style.overflowY");
        Assert.True(overflowY is "" or "visible", $"Expected no vertical overflow constraint but got: {overflowY}");
    }

    [Fact]
    public async Task Unzooming_clears_overflow()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomClearOverflow.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var container = GetDiagramContainer();
        await container.HoverAsync();
        await Page.WaitForTimeoutAsync(200);

        // Zoom in then out
        await Page.EvaluateAsync("""
            () => { var btn = document.querySelector('.diagram-zoom-toggle'); btn.click(); btn.click(); }
        """);

        var overflowX = await container.EvaluateAsync<string>("e => e.style.overflowX");
        Assert.True(overflowX is "" or "visible", $"Expected overflow-x to be cleared but got: {overflowX}");
    }

    // ── Double-click zoom ──

    [Fact]
    public async Task Double_click_on_svg_toggles_zoom()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomDblClick.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var svg = Page.Locator("[data-diagram-type='plantuml'] svg").First;
        await svg.DblClickAsync();

        await Expect(GetDiagramContainer()).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("diagram-natural-size"));
    }

    [Fact]
    public async Task Double_click_again_unzooms()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomDblClickToggle.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var svg = Page.Locator("[data-diagram-type='plantuml'] svg").First;
        var container = GetDiagramContainer();

        await svg.DblClickAsync();
        await Expect(container).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("diagram-natural-size"));

        await svg.DblClickAsync();
        await Expect(container).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("diagram-natural-size"));
    }

    // ── Zoom button icon changes ──

    [Fact]
    public async Task Zoom_button_icon_changes_when_zoomed()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomIcon.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var container = GetDiagramContainer();
        var zoomBtn = Page.Locator(".diagram-zoom-toggle").First;
        var initialText = await zoomBtn.TextContentAsync();

        await container.HoverAsync();
        await Page.WaitForTimeoutAsync(200);
        await Page.EvaluateAsync("() => document.querySelector('.diagram-zoom-toggle').click()");

        var zoomedText = await zoomBtn.TextContentAsync();
        Assert.NotEqual(initialText, zoomedText);

        await Page.EvaluateAsync("() => document.querySelector('.diagram-zoom-toggle').click()");
        var revertedText = await zoomBtn.TextContentAsync();
        Assert.Equal(initialText, revertedText);
    }

    // ── Non-zoomable diagrams ──

    [Fact]
    public async Task Non_zoomable_diagram_has_no_zoom_button()
    {
        await Page.GotoAsync(GenerateReport("NonZoomableNoButton.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var count = await Page.Locator(".diagram-zoom-toggle").CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Double_click_on_non_zoomable_diagram_does_not_zoom()
    {
        await Page.GotoAsync(GenerateReport("NonZoomableDblClick.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var svg = Page.Locator("[data-diagram-type='plantuml'] svg").First;
        await svg.DblClickAsync();

        var cls = await GetDiagramContainer().GetAttributeAsync("class") ?? "";
        Assert.DoesNotContain("diagram-natural-size", cls);
    }

    // ── Text selection on double-click zoom ──

    [Fact]
    public async Task Double_click_zoom_does_not_select_text()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("ZoomDblClickNoSelect.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var svg = Page.Locator("[data-diagram-type='plantuml'] svg").First;
        await svg.DblClickAsync();

        var selectedText = await Page.EvaluateAsync<string>("() => window.getSelection().toString()");
        Assert.Equal("", selectedText);
    }

    [Fact]
    public async Task Text_in_steps_can_still_be_highlighted()
    {
        await Page.GotoAsync(GenerateReportWithWideDiagram("StepTextSelectable.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        var stepElement = Page.Locator(".scenario-steps li, .step, .scenario li").First;
        await Page.EvaluateAsync("""
            (el) => {
                var range = document.createRange();
                range.selectNodeContents(el);
                var sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(range);
            }
        """, await stepElement.ElementHandleAsync());

        var selectedText = await Page.EvaluateAsync<string>("() => window.getSelection().toString()");
        Assert.NotEqual("", selectedText.Trim());
    }

    // ── Zoom button persists after note collapse ──

    [Fact]
    public async Task Zoom_button_persists_after_note_collapse()
    {
        await Page.GotoAsync(GenerateReportWithWideNoteDiagram("ZoomAfterNoteCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.diagram-zoom-toggle').length > 0",
            null, new() { Timeout = 10000, PollingInterval = 200 });

        var container = GetDiagramContainer();
        var before = await container.Locator(".diagram-zoom-toggle").CountAsync();
        Assert.Equal(1, before);

        // Collapse notes
        await Page.Locator(".diagram-toggle .details-radio-btn[data-state='collapsed']").First.ClickAsync();

        await Page.WaitForFunctionAsync("""
            () => {
                var c = document.querySelector('[data-diagram-type="plantuml"]');
                var svg = c && c.querySelector('svg');
                return svg && svg.offsetParent !== null && c.querySelectorAll('.diagram-zoom-toggle').length > 0;
            }
        """, null, new() { Timeout = 15000, PollingInterval = 200 });

        var after = await container.Locator(".diagram-zoom-toggle").CountAsync();
        Assert.Equal(1, after);
    }

    [Fact]
    public async Task Zoom_button_works_after_note_collapse()
    {
        await Page.GotoAsync(GenerateReportWithWideNoteDiagram("ZoomWorksAfterNoteCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.diagram-zoom-toggle').length > 0",
            null, new() { Timeout = 10000, PollingInterval = 200 });

        // Collapse notes
        await Page.Locator(".diagram-toggle .details-radio-btn[data-state='collapsed']").First.ClickAsync();

        await Page.WaitForFunctionAsync("""
            () => {
                var c = document.querySelector('[data-diagram-type="plantuml"]');
                var svg = c && c.querySelector('svg');
                return svg && svg.offsetParent !== null && c.querySelectorAll('.diagram-zoom-toggle').length > 0;
            }
        """, null, new() { Timeout = 15000, PollingInterval = 200 });

        // Click the zoom button
        await Page.EvaluateAsync("""
            () => document.querySelector('[data-diagram-type="plantuml"] .diagram-zoom-toggle').click()
        """);

        await Expect(GetDiagramContainer()).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("diagram-natural-size"));
    }
}