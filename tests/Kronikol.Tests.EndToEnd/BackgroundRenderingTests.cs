namespace Kronikol.Tests.EndToEnd;

[Collection(PlaywrightCollections.Reports)]
public class BackgroundRenderingTests : PlaywrightTestBase
{
    public BackgroundRenderingTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Scenarios_with_background_steps_render_background_section()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgSection.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        // Expand feature and scenarios
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // First scenario has background steps
        var firstScenario = Page.Locator("details.scenario").First;
        var background = firstScenario.Locator("details.scenario-background");
        Assert.Equal(1, await background.CountAsync());
    }

    [Fact]
    public async Task Background_section_has_correct_summary_text()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgSummary.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        var bgSummary = Page.Locator("details.scenario").First.Locator("details.scenario-background > summary");
        var text = await bgSummary.InnerTextAsync();
        Assert.Equal("Background Steps", text);
    }

    [Fact]
    public async Task Background_section_is_collapsed_by_default()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgCollapsed.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        var background = Page.Locator("details.scenario").First.Locator("details.scenario-background");
        await background.WaitForAsync();

        // Should NOT have the 'open' attribute (collapsed by default)
        Assert.Null(await background.GetAttributeAsync("open"));
    }

    [Fact]
    public async Task Background_section_contains_correct_number_of_steps()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgStepCount.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // Open the background section
        var background = Page.Locator("details.scenario").First.Locator("details.scenario-background");
        await background.Locator("summary").ClickAsync();

        // Should have 2 background steps (Given + And)
        var steps = background.Locator(".step");
        Assert.Equal(2, await steps.CountAsync());
    }

    [Fact]
    public async Task Background_steps_display_correct_text()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgStepText.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // Open the background section
        var background = Page.Locator("details.scenario").First.Locator("details.scenario-background");
        await background.Locator("summary").ClickAsync();

        var stepsText = await background.Locator(".step").AllInnerTextsAsync();
        Assert.Contains(stepsText, t => t.Contains("the registration service is running"));
        Assert.Contains(stepsText, t => t.Contains("the database is available"));
    }

    [Fact]
    public async Task Background_section_renders_before_steps_section()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgOrder.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // Background should come before Steps in DOM order
        var firstScenario = Page.Locator("details.scenario").First;
        var bgIndex = await firstScenario.EvaluateAsync<int>("""
            (el) => {
                var children = [...el.querySelectorAll(':scope > details')];
                var bgIdx = children.findIndex(d => d.classList.contains('scenario-background'));
                return bgIdx;
            }
        """);
        var stepsIndex = await firstScenario.EvaluateAsync<int>("""
            (el) => {
                var children = [...el.querySelectorAll(':scope > details')];
                var stepsIdx = children.findIndex(d => d.classList.contains('scenario-steps'));
                return stepsIdx;
            }
        """);

        Assert.True(bgIndex >= 0, "Background section should exist");
        Assert.True(stepsIndex >= 0, "Steps section should exist");
        Assert.True(bgIndex < stepsIndex, "Background should come before Steps");
    }

    [Fact]
    public async Task Scenario_without_background_has_no_background_section()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgAbsent.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // Third scenario (bg3) has no background steps
        var thirdScenario = Page.Locator("details.scenario").Nth(2);
        var background = thirdScenario.Locator("details.scenario-background");
        Assert.Equal(0, await background.CountAsync());
    }

    [Fact]
    public async Task Multiple_scenarios_with_same_background_each_render_background()
    {
        await Page.GotoAsync(GenerateReportWithBackground("BgMultiple.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // Both first and second scenarios should have background sections
        var firstBg = Page.Locator("details.scenario").Nth(0).Locator("details.scenario-background");
        var secondBg = Page.Locator("details.scenario").Nth(1).Locator("details.scenario-background");

        Assert.Equal(1, await firstBg.CountAsync());
        Assert.Equal(1, await secondBg.CountAsync());
    }
}
