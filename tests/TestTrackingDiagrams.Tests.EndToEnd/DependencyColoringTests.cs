using Microsoft.Playwright;

namespace TestTrackingDiagrams.Tests.EndToEnd;

[Collection(PlaywrightCollections.Diagrams)]
public class DependencyColoringTests : PlaywrightTestBase
{
    public DependencyColoringTests(PlaywrightFixture fixture) : base(fixture) { }

    // ═══════════════════════════════════════════════════════════
    // Sequence Diagram — Colored Arrows
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Sequence_diagram_renders_SVG_with_colored_arrows()
    {
        await Page.GotoAsync(GenerateReport("DepColorArrows.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        var svg = (await WaitForDiagramSvg()).First;

        var svgHtml = await svg.EvaluateAsync<string>("el => el.outerHTML");
        Assert.Contains("svg", svgHtml);
        Assert.True(svgHtml.Length > 100, "SVG should contain rendered diagram content");
    }

    // ═══════════════════════════════════════════════════════════
    // Embedded Component Diagram
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Component_diagram_is_hidden_by_default()
    {
        await Page.GotoAsync(GenerateReportWithEmbeddedComponentDiagram("ComponentHiddenDefault.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        var section = Page.Locator("#component-diagram");
        var display = await section.EvaluateAsync<string>("e => window.getComputedStyle(e).display");
        Assert.Equal("none", display);
    }

    [Fact]
    public async Task Component_diagram_toggle_button_exists()
    {
        await Page.GotoAsync(GenerateReportWithEmbeddedComponentDiagram("ComponentToggleBtn.html"));

        var button = Page.Locator("button.timeline-toggle", new() { HasTextString = "Component Diagram" });
        await button.WaitForAsync(new() { Timeout = 5000 });
        await Expect(button).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Component_diagram_toggle_button_not_present_without_diagram()
    {
        await Page.GotoAsync(GenerateReport("NoComponentToggle.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        var count = await Page.Locator("button[onclick*='toggle_component_diagram']").CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Component_diagram_toggle_shows_and_hides()
    {
        await Page.GotoAsync(GenerateReportWithEmbeddedComponentDiagram("ComponentToggleShowHide.html"));

        var button = Page.Locator("button[onclick*='toggle_component_diagram']");
        await button.WaitForAsync(new() { Timeout = 5000 });
        var section = Page.Locator("#component-diagram");

        // Initially hidden
        Assert.Equal("none", await section.EvaluateAsync<string>("e => window.getComputedStyle(e).display"));

        // Click to show
        await button.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => window.getComputedStyle(document.getElementById('component-diagram')).display !== 'none'",
            null, new() { Timeout = 3000, PollingInterval = 200 });

        // Click to hide
        await button.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => window.getComputedStyle(document.getElementById('component-diagram')).display === 'none'",
            null, new() { Timeout = 3000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Toggling_component_diagram_deactivates_timeline()
    {
        await Page.GotoAsync(GenerateReportWithEmbeddedComponentDiagram("ComponentRadioTimeline.html"));

        var tlButton = Page.Locator("button[onclick*='toggle_timeline']");
        var cdButton = Page.Locator("button[onclick*='toggle_component_diagram']");
        await tlButton.WaitForAsync(new() { Timeout = 5000 });
        await cdButton.WaitForAsync(new() { Timeout = 5000 });

        // Show timeline first
        await tlButton.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => window.getComputedStyle(document.getElementById('scenario-timeline')).display !== 'none'",
            null, new() { Timeout = 3000, PollingInterval = 200 });

        // Show component diagram — timeline should hide
        await cdButton.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => {
                var cd = document.getElementById('component-diagram');
                var tl = document.getElementById('scenario-timeline');
                return window.getComputedStyle(cd).display !== 'none' &&
                       window.getComputedStyle(tl).display === 'none';
            }
        """, null, new() { Timeout = 3000, PollingInterval = 200 });

        var tlClass = await tlButton.GetAttributeAsync("class") ?? "";
        Assert.DoesNotContain("timeline-toggle-active", tlClass);
        var cdClass = await cdButton.GetAttributeAsync("class") ?? "";
        Assert.Contains("timeline-toggle-active", cdClass);
    }

    [Fact]
    public async Task Toggling_timeline_deactivates_component_diagram()
    {
        await Page.GotoAsync(GenerateReportWithEmbeddedComponentDiagram("TimelineRadioComponent.html"));

        var tlButton = Page.Locator("button[onclick*='toggle_timeline']");
        var cdButton = Page.Locator("button[onclick*='toggle_component_diagram']");
        await tlButton.WaitForAsync(new() { Timeout = 5000 });

        // Show component diagram first
        await cdButton.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => window.getComputedStyle(document.getElementById('component-diagram')).display !== 'none'",
            null, new() { Timeout = 3000, PollingInterval = 200 });

        // Show timeline — component should hide
        await tlButton.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => {
                var cd = document.getElementById('component-diagram');
                var tl = document.getElementById('scenario-timeline');
                return window.getComputedStyle(tl).display !== 'none' &&
                       window.getComputedStyle(cd).display === 'none';
            }
        """, null, new() { Timeout = 3000, PollingInterval = 200 });

        var cdClass = await cdButton.GetAttributeAsync("class") ?? "";
        Assert.DoesNotContain("timeline-toggle-active", cdClass);
        var tlClass = await tlButton.GetAttributeAsync("class") ?? "";
        Assert.Contains("timeline-toggle-active", tlClass);
    }

    [Fact]
    public async Task Component_diagram_toggle_button_has_active_class_when_shown()
    {
        await Page.GotoAsync(GenerateReportWithEmbeddedComponentDiagram("ComponentToggleActive.html"));

        var button = Page.Locator("button[onclick*='toggle_component_diagram']");
        await button.WaitForAsync(new() { Timeout = 5000 });

        var cls = await button.GetAttributeAsync("class") ?? "";
        Assert.DoesNotContain("timeline-toggle-active", cls);

        // Click to show
        await button.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => document.querySelector("button[onclick*='toggle_component_diagram']")
                .classList.contains('timeline-toggle-active')
        """, null, new() { Timeout = 3000, PollingInterval = 200 });

        // Click to hide
        await button.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => !document.querySelector("button[onclick*='toggle_component_diagram']")
                .classList.contains('timeline-toggle-active')
        """, null, new() { Timeout = 3000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Component_diagram_renders_SVG_after_toggle()
    {
        await Page.GotoAsync(GenerateReportWithEmbeddedComponentDiagram("ComponentSvgToggle.html"));

        var button = Page.Locator("button[onclick*='toggle_component_diagram']");
        await button.WaitForAsync(new() { Timeout = 5000 });
        await button.ClickAsync();

        var svg = Page.Locator("#component-diagram [data-diagram-type='plantuml'] svg");
        await svg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });

        var svgHtml = await svg.EvaluateAsync<string>("el => el.outerHTML");
        Assert.Contains("svg", svgHtml);
    }

    [Fact]
    public async Task Report_without_component_diagram_has_no_section()
    {
        await Page.GotoAsync(GenerateReport("NoComponentSection.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        var count = await Page.Locator(".component-diagram-section").CountAsync();
        Assert.Equal(0, count);
    }
}