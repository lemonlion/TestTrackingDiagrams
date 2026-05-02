using Microsoft.Playwright;

namespace TestTrackingDiagrams.Tests.EndToEnd;

public class IflowPopupTests : PlaywrightTestBase
{
    public IflowPopupTests(PlaywrightFixture fixture) : base(fixture) { }

    protected override int ViewportWidth => 1280;
    protected override int ViewportHeight => 900;

    private async Task<ILocator> WaitForActivityDiagramSvg(int timeoutMs = 15000)
    {
        var svg = Page.Locator(".iflow-diagram svg");
        await svg.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
        return svg;
    }

    [Fact]
    public async Task Clicking_trigger_opens_popup_overlay()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage()));

        await Page.Locator("#trigger-seg-1").ClickAsync();

        var overlay = Page.Locator(".iflow-overlay");
        await Expect(overlay).ToBeVisibleAsync();

        var popup = Page.Locator(".iflow-popup");
        await Expect(popup).ToBeVisibleAsync();

        await WaitForActivityDiagramSvg();
        var diagramText = await Page.Locator(".iflow-diagram").First.TextContentAsync();
        Assert.DoesNotContain("Loading", diagramText!);
    }

    [Fact]
    public async Task Popup_shows_segment_title_and_content()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage()));
        await Page.Locator("#trigger-seg-1").ClickAsync();

        var popup = Page.Locator(".iflow-popup");
        await Expect(popup).ToBeVisibleAsync();

        await Expect(popup.Locator("h3")).ToContainTextAsync("Internal Flow");

        await WaitForActivityDiagramSvg();
        var svgText = await Page.Locator(".iflow-diagram svg").TextContentAsync();
        Assert.Contains("HTTP GET /api/orders", svgText!);
        Assert.Contains("SELECT", svgText!);
    }

    [Fact]
    public async Task Close_button_removes_overlay()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage()));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Page.Locator(".iflow-overlay").WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await Page.Locator(".iflow-popup-close").ClickAsync();

        await Page.Locator(".iflow-overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    [Fact]
    public async Task Escape_key_closes_popup()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage()));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Page.Locator(".iflow-overlay").WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator(".iflow-overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    [Fact]
    public async Task Clicking_overlay_background_closes_popup()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage()));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        var overlay = Page.Locator(".iflow-overlay");
        await overlay.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await overlay.EvaluateAsync("el => el.click()");

        await overlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    [Fact]
    public async Task Missing_segment_shows_no_data_message()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage()));
        await Page.Locator("#trigger-seg-missing").ClickAsync();

        var noData = Page.Locator(".iflow-popup .iflow-no-data");
        await Expect(noData).ToBeVisibleAsync();
        await Expect(noData).ToContainTextAsync("No internal flow data");
    }

    [Fact]
    public async Task Empty_segment_shows_no_activity_message()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeEmptySegment: true)));
        await Page.Locator("#trigger-seg-empty").ClickAsync();

        var noData = Page.Locator(".iflow-popup .iflow-no-data");
        await Expect(noData).ToBeVisibleAsync();
        await Expect(noData).ToContainTextAsync("No internal activity");
    }

    [Fact]
    public async Task Toggle_buttons_are_rendered_when_flame_chart_enabled()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();

        var popup = Page.Locator(".iflow-popup");
        await Expect(popup).ToBeVisibleAsync();

        var toggleBtns = popup.Locator(".iflow-toggle-btn");
        Assert.Equal(2, await toggleBtns.CountAsync());
        await Expect(toggleBtns.First).ToHaveTextAsync("Activity");
        await Expect(toggleBtns.Nth(1)).ToHaveTextAsync("Flame Chart");
    }

    [Fact]
    public async Task Activity_view_is_visible_by_default_flame_is_hidden()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();

        await Expect(Page.Locator(".iflow-view-main")).ToBeVisibleAsync();
        await Expect(Page.Locator(".iflow-view-flame")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Clicking_flame_chart_toggle_shows_flame_hides_activity()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();
        await WaitForActivityDiagramSvg();

        await Page.Locator(".iflow-toggle-btn").Nth(1).ClickAsync();

        await Expect(Page.Locator(".iflow-view-main")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator(".iflow-view-flame")).ToBeVisibleAsync();

        var flameBars = Page.Locator(".iflow-flame-bar");
        Assert.True(await flameBars.CountAsync() >= 2);
    }

    [Fact]
    public async Task Clicking_activity_toggle_back_restores_activity_view()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();

        var toggleBtns = Page.Locator(".iflow-toggle-btn");
        await toggleBtns.Nth(1).ClickAsync();
        await toggleBtns.First.ClickAsync();

        await Expect(Page.Locator(".iflow-view-main")).ToBeVisibleAsync();
        await Expect(Page.Locator(".iflow-view-flame")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Active_toggle_button_has_active_class()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();

        var toggleBtns = Page.Locator(".iflow-toggle-btn");
        await Expect(toggleBtns.First).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("iflow-toggle-active"));
        var cls1 = await toggleBtns.Nth(1).GetAttributeAsync("class");
        Assert.DoesNotContain("iflow-toggle-active", cls1!);

        await toggleBtns.Nth(1).ClickAsync();
        var cls0After = await toggleBtns.First.GetAttributeAsync("class");
        Assert.DoesNotContain("iflow-toggle-active", cls0After!);
        await Expect(toggleBtns.Nth(1)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("iflow-toggle-active"));
    }

    [Fact]
    public async Task Flame_chart_has_flame_bars()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();

        await Page.Locator(".iflow-toggle-btn").Nth(1).ClickAsync();

        var flameBars = Page.Locator(".iflow-flame-bar");
        Assert.True(await flameBars.CountAsync() >= 2);
    }

    [Fact]
    public async Task Call_tree_view_renders_nested_list()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeCallTree: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();

        var callTree = Page.Locator(".iflow-call-tree");
        await Expect(callTree).ToBeVisibleAsync();

        var items = callTree.Locator("li");
        Assert.True(await items.CountAsync() >= 2);

        var treeText = await callTree.TextContentAsync();
        Assert.Contains("HTTP GET /api/orders", treeText!);
    }

    [Fact]
    public async Task Opening_new_popup_replaces_existing_one()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeEmptySegment: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Page.Locator(".iflow-popup").WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await Page.EvaluateAsync("window._iflowShowPopup('iflow-seg-empty')");
        await Page.Locator(".iflow-no-data").WaitForAsync(new() { State = WaitForSelectorState.Visible });

        Assert.Equal(1, await Page.Locator(".iflow-overlay").CountAsync());
    }

    [Fact]
    public async Task Context_menu_appears_on_activity_diagram_right_click()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeContextMenu: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();
        var svg = await WaitForActivityDiagramSvg();

        await DispatchContextMenu(svg.First);

        var menu = Page.Locator(".diagram-ctx-menu");
        await Expect(menu).ToBeVisibleAsync(new() { Timeout = 5000 });

        var menuText = await menu.TextContentAsync();
        Assert.Contains("Copy image", menuText!);
        Assert.Contains("Save image", menuText!);
        Assert.Contains("Open image in new tab", menuText!);
    }

    [Fact]
    public async Task Context_menu_z_index_is_above_popup_overlay()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeContextMenu: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();
        var svg = await WaitForActivityDiagramSvg();

        await DispatchContextMenu(svg.First);
        var menu = Page.Locator(".diagram-ctx-menu");
        await Expect(menu).ToBeVisibleAsync();

        var zIndex = await menu.EvaluateAsync<string>("el => getComputedStyle(el).zIndex");
        Assert.Equal("20001", zIndex);
    }

    [Fact]
    public async Task Context_menu_on_flame_chart_has_png_only()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true, includeContextMenu: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();
        await WaitForActivityDiagramSvg();

        await Page.Locator(".iflow-toggle-btn").Nth(1).ClickAsync();

        await DispatchContextMenu(Page.Locator(".iflow-flame-bar").First);
        var menu = Page.Locator(".diagram-ctx-menu");
        await Expect(menu).ToBeVisibleAsync();

        var menuText = await menu.TextContentAsync();
        Assert.Contains("Copy as PNG", menuText!);
        Assert.Contains("Save as PNG", menuText!);
        Assert.DoesNotContain("Copy as SVG", menuText!);
        Assert.DoesNotContain("Copy PlantUML source", menuText!);
    }

    [Fact]
    public async Task PNG_no_transparency_opaque_for_InlineSvg_sequence_diagram()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateInlineSvgSequenceDiagramPage()));
        await Page.Locator(".plantuml-inline-svg svg").WaitForAsync();

        var result = await Page.EvaluateAsync<string>(PngOpacityCheckScript(".plantuml-inline-svg svg"));
        var data = System.Text.Json.JsonDocument.Parse(result).RootElement;
        Assert.False(data.TryGetProperty("error", out _), $"Error: {result}");
        Assert.Equal(255, data.GetProperty("tl").GetInt32());
        Assert.Equal(255, data.GetProperty("tr").GetInt32());
        Assert.Equal(255, data.GetProperty("bl").GetInt32());
        Assert.Equal(255, data.GetProperty("br").GetInt32());
    }

    [Fact]
    public async Task PNG_no_transparency_opaque_for_BrowserJs_sequence_diagram()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateBrowserJsSequenceDiagramPage()));
        await Page.Locator(".plantuml-browser svg").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        var result = await Page.EvaluateAsync<string>(PngOpacityCheckScript(".plantuml-browser svg"));
        var data = System.Text.Json.JsonDocument.Parse(result).RootElement;
        Assert.False(data.TryGetProperty("error", out _), $"Error: {result}");
        Assert.Equal(255, data.GetProperty("tl").GetInt32());
        Assert.Equal(255, data.GetProperty("tr").GetInt32());
        Assert.Equal(255, data.GetProperty("bl").GetInt32());
        Assert.Equal(255, data.GetProperty("br").GetInt32());
    }

    [Fact]
    public async Task Save_as_PNG_no_transparency_produces_opaque_pixels()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeContextMenu: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        await Expect(Page.Locator(".iflow-popup")).ToBeVisibleAsync();
        await WaitForActivityDiagramSvg();

        var result = await Page.EvaluateAsync<string>(PngOpacityCheckScript(".iflow-popup svg"));
        var data = System.Text.Json.JsonDocument.Parse(result).RootElement;
        Assert.False(data.TryGetProperty("error", out _), $"Error: {result}");
        Assert.Equal(255, data.GetProperty("tl").GetInt32());
        Assert.Equal(255, data.GetProperty("tr").GetInt32());
        Assert.Equal(255, data.GetProperty("bl").GetInt32());
        Assert.Equal(255, data.GetProperty("br").GetInt32());
    }

    [Fact]
    public async Task Popup_styles_are_applied_from_stylesheet()
    {
        await Page.GotoAsync(ServePage(TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true)));
        await Page.Locator("#trigger-seg-1").ClickAsync();
        var popup = Page.Locator(".iflow-popup");
        await Expect(popup).ToBeVisibleAsync();

        // Overlay: fixed positioning
        var overlay = Page.Locator(".iflow-overlay");
        var overlayPosition = await overlay.EvaluateAsync<string>("el => getComputedStyle(el).position");
        Assert.Equal("fixed", overlayPosition);
        var overlayZIndex = await overlay.EvaluateAsync<string>("el => getComputedStyle(el).zIndex");
        Assert.Equal("20000", overlayZIndex);

        // Popup: white background, rounded corners, box-shadow
        var popupBg = await popup.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        Assert.Equal("rgb(255, 255, 255)", popupBg);
        var popupRadius = await popup.EvaluateAsync<string>("el => getComputedStyle(el).borderRadius");
        Assert.Equal("8px", popupRadius);
        var popupShadow = await popup.EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");
        Assert.NotEqual("none", popupShadow);

        // Close button: absolute positioning
        var closePos = await popup.Locator(".iflow-popup-close")
            .EvaluateAsync<string>("el => getComputedStyle(el).position");
        Assert.Equal("absolute", closePos);

        // Toggle buttons: border-radius
        var toggleBtns = popup.Locator(".iflow-toggle-btn");
        var count = await toggleBtns.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var radius = await toggleBtns.Nth(i).EvaluateAsync<string>("el => getComputedStyle(el).borderRadius");
            Assert.Equal("4px", radius);
        }

        // Active toggle: blue background
        var activeBg = await popup.Locator(".iflow-toggle-active")
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        Assert.Equal("rgb(66, 133, 244)", activeBg);
    }

    private static string PngOpacityCheckScript(string svgSelector) => $$"""
        (async () => {
            var svg = document.querySelector('{{svgSelector}}');
            if (!svg) return JSON.stringify({error:'no svg'});
            var svgData = new XMLSerializer().serializeToString(svg);
            var url = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
            return await new Promise((resolve) => {
                var img = new Image();
                img.onload = function() {
                    var w = img.naturalWidth, h = img.naturalHeight;
                    if (w === 0 || h === 0) { resolve(JSON.stringify({error:'zero size'})); return; }
                    var canvas = document.createElement('canvas');
                    canvas.width = w; canvas.height = h;
                    var ctx = canvas.getContext('2d');
                    ctx.fillStyle = '#ffffff';
                    ctx.fillRect(0, 0, w, h);
                    ctx.drawImage(img, 0, 0);
                    var tl = ctx.getImageData(0, 0, 1, 1).data[3];
                    var tr = ctx.getImageData(w-1, 0, 1, 1).data[3];
                    var bl = ctx.getImageData(0, h-1, 1, 1).data[3];
                    var br = ctx.getImageData(w-1, h-1, 1, 1).data[3];
                    resolve(JSON.stringify({tl:tl,tr:tr,bl:bl,br:br,w:w,h:h}));
                };
                img.onerror = function() { resolve(JSON.stringify({error:'img load failed'})); };
                img.src = url;
            });
        })()
    """;
}
