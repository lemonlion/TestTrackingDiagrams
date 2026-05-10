using Microsoft.Playwright;

namespace TestTrackingDiagrams.Tests.EndToEnd;

[Collection(PlaywrightCollections.Scenarios)]
public class StepTableToggleTests : PlaywrightTestBase
{
    public StepTableToggleTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Toggle_button_renders_with_up_arrow()
    {
        await Page.GotoAsync(GenerateReportWithStepTableToggle("StepToggleRender.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        // Expand all to see step content
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // The toggle button should exist with "recipe ▴" text
        var toggleBtn = Page.Locator("button.step-table-ref");
        await toggleBtn.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        var text = await toggleBtn.First.InnerTextAsync();
        Assert.Contains("recipe", text);
        Assert.Contains("\u25B4", text); // ▴ up-pointing triangle
    }

    [Fact]
    public async Task Toggle_button_click_collapses_table()
    {
        await Page.GotoAsync(GenerateReportWithStepTableToggle("StepToggleCollapse.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        var toggleBtn = Page.Locator("button.step-table-ref").First;
        await toggleBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // Table should be visible before toggling
        var table = Page.Locator(".step-param-table[data-param='recipe']").First;
        await Expect(table).ToBeVisibleAsync();

        // Click toggle — table should become hidden (collapsed)
        await toggleBtn.ClickAsync();

        await Expect(table).ToBeHiddenAsync();
        // Arrow should change to down-pointing
        var text = await toggleBtn.InnerTextAsync();
        Assert.Contains("\u25BE", text); // ▾ down-pointing triangle
    }

    [Fact]
    public async Task Toggle_button_click_twice_restores_table()
    {
        await Page.GotoAsync(GenerateReportWithStepTableToggle("StepToggleRestore.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        var toggleBtn = Page.Locator("button.step-table-ref").First;
        await toggleBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var table = Page.Locator(".step-param-table[data-param='recipe']").First;

        // Click once — collapse
        await toggleBtn.ClickAsync();
        await Expect(table).ToBeHiddenAsync();

        // Click again — expand
        await toggleBtn.ClickAsync();
        await Expect(table).ToBeVisibleAsync();

        // Arrow should be back to up-pointing
        var text = await toggleBtn.InnerTextAsync();
        Assert.Contains("\u25B4", text); // ▴
    }

    [Fact]
    public async Task Table_contains_expected_data()
    {
        await Page.GotoAsync(GenerateReportWithStepTableToggle("StepToggleData.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        var table = Page.Locator(".step-param-table[data-param='recipe']").First;
        await Expect(table).ToBeVisibleAsync();

        // Verify the table has the expected column headers
        var headers = table.Locator("thead th");
        var headerTexts = await headers.AllInnerTextsAsync();
        Assert.Contains("Name", headerTexts);
        Assert.Contains("Flour", headerTexts);

        // Verify the table has data rows
        var cells = table.Locator("tbody td");
        var cellTexts = await cells.AllInnerTextsAsync();
        Assert.Contains("Classic", cellTexts);
        Assert.Contains("Plain Flour", cellTexts);
    }
}
