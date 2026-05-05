using Microsoft.Playwright;

namespace TestTrackingDiagrams.Tests.EndToEnd;

/// <summary>
/// Tests for the scenario-level and report-level Assertions Show/Hide radio buttons.
/// Verifies that toggling assertions on one scenario does NOT affect other scenarios (bug fix).
/// Also tests combinations with other radio buttons (Details, Headers) and hover buttons.
/// </summary>
[Collection(PlaywrightCollections.Diagrams)]
public class AssertionToggleTests : DiagramNotePlaywrightBase
{
    public AssertionToggleTests(PlaywrightFixture fixture) : base(fixture) { }

    private string GenerateAssertionReport(string fileName) =>
        ReportTestHelper.GenerateReportWithAssertionNotes(TempDir, OutputDir, fileName);

    private async Task ExpandBothScenariosAndRender(string fileName)
    {
        await Page.GotoAsync(GenerateAssertionReport(fileName));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();

        // Render all diagrams (both scenarios)
        await RenderAllDiagramsAndWait();
    }

    private ILocator ScenarioLocator(int index) =>
        Page.Locator("details.scenario").Nth(index);

    private ILocator AssertionShowBtn(ILocator scenario) =>
        scenario.Locator(".assertions-radio-btn[data-astate='show']");

    private ILocator AssertionHideBtn(ILocator scenario) =>
        scenario.Locator(".assertions-radio-btn[data-astate='hide']");

    private ILocator ReportAssertionShowBtn =>
        Page.Locator(".toolbar-right .assertions-radio-btn[data-astate='show']");

    private ILocator ReportAssertionHideBtn =>
        Page.Locator(".toolbar-right .assertions-radio-btn[data-astate='hide']");

    /// <summary>
    /// Checks if the rendered SVG for a scenario contains assertion note text.
    /// This checks the actual visual output, not data-plantuml (which retains the full source).
    /// Scenario 1's assertions contain "Scenario1 status code" and scenario 2's contain "Scenario2 status code".
    /// </summary>
    private async Task<bool> SvgContainsAssertionText(ILocator scenario, string scenarioLabel)
    {
        var svg = scenario.Locator("[data-diagram-type='plantuml'] svg");
        var html = await svg.First.EvaluateAsync<string>("el => el.outerHTML");
        return html.Contains($"{scenarioLabel} status code");
    }

    private async Task WaitForSvgChange(ILocator scenario, string previousSvgHtml, int timeoutMs = 15000)
    {
        var idx = await GetScenarioIndex(scenario);
        await Page.WaitForFunctionAsync(
            @"(args) => {
                var sc = document.querySelectorAll('details.scenario')[args.idx];
                if (!sc) return false;
                var svg = sc.querySelector('[data-diagram-type=""plantuml""] svg');
                return svg && svg.outerHTML !== args.prev;
            }",
            new { idx, prev = previousSvgHtml },
            new() { Timeout = timeoutMs, PollingInterval = 200 });
    }

    private async Task<string> GetScenarioSvgHtml(ILocator scenario)
    {
        return await scenario.Locator("[data-diagram-type='plantuml'] svg").First
            .EvaluateAsync<string>("el => el.outerHTML");
    }

    private async Task<int> GetScenarioIndex(ILocator scenario)
    {
        var all = Page.Locator("details.scenario");
        var count = await all.CountAsync();
        var targetSummary = await scenario.Locator("summary").First.InnerTextAsync();
        for (int i = 0; i < count; i++)
        {
            var t = await all.Nth(i).Locator("summary").First.InnerTextAsync();
            if (t == targetSummary) return i;
        }
        return 0;
    }

    // === BUG FIX: Scenario-level Show should NOT affect other scenarios ===

    [Fact]
    public async Task Scenario_show_assertions_does_not_affect_other_scenario()
    {
        await ExpandBothScenariosAndRender("AssertIsolation1.html");

        var scenario1 = ScenarioLocator(0);
        var scenario2 = ScenarioLocator(1);

        // Both scenarios start with assertions hidden
        Assert.False(await SvgContainsAssertionText(scenario1, "Scenario1"));
        Assert.False(await SvgContainsAssertionText(scenario2, "Scenario2"));

        // Click Show for scenario 1 only
        var s1SvgBefore = await GetScenarioSvgHtml(scenario1);
        await AssertionShowBtn(scenario1).ClickAsync();
        await WaitForSvgChange(scenario1, s1SvgBefore);

        // Scenario 1 should now show assertion text
        Assert.True(await SvgContainsAssertionText(scenario1, "Scenario1"));

        // Scenario 2 should still NOT show assertion text
        Assert.False(await SvgContainsAssertionText(scenario2, "Scenario2"));
    }

    [Fact]
    public async Task Scenario_hide_assertions_does_not_affect_other_scenario()
    {
        await ExpandBothScenariosAndRender("AssertIsolation2.html");

        var scenario1 = ScenarioLocator(0);
        var scenario2 = ScenarioLocator(1);

        // First show assertions for both via report-level
        await ReportAssertionShowBtn.ClickAsync();
        await Page.WaitForFunctionAsync(
            @"() => {
                var svgs = document.querySelectorAll('details.scenario [data-diagram-type=""plantuml""] svg');
                return svgs.length >= 2 &&
                    Array.from(svgs).every(s => s.outerHTML.includes('status code'));
            }",
            null, new() { Timeout = 30000, PollingInterval = 200 });

        // Now hide assertions for scenario 1 only
        var s1SvgBefore = await GetScenarioSvgHtml(scenario1);
        await AssertionHideBtn(scenario1).ClickAsync();
        await WaitForSvgChange(scenario1, s1SvgBefore);

        // Scenario 1 should NOT show assertion text
        Assert.False(await SvgContainsAssertionText(scenario1, "Scenario1"));

        // Scenario 2 should STILL show assertion text
        Assert.True(await SvgContainsAssertionText(scenario2, "Scenario2"));
    }

    // === Report-level Show/Hide affects all scenarios ===

    [Fact]
    public async Task Report_show_assertions_shows_for_all_scenarios()
    {
        await ExpandBothScenariosAndRender("AssertReportShow.html");

        await ReportAssertionShowBtn.ClickAsync();

        await Page.WaitForFunctionAsync(
            @"() => {
                var svgs = document.querySelectorAll('details.scenario [data-diagram-type=""plantuml""] svg');
                return svgs.length >= 2 &&
                    Array.from(svgs).every(s => s.outerHTML.includes('status code'));
            }",
            null, new() { Timeout = 30000, PollingInterval = 200 });

        Assert.True(await SvgContainsAssertionText(ScenarioLocator(0), "Scenario1"));
        Assert.True(await SvgContainsAssertionText(ScenarioLocator(1), "Scenario2"));
    }

    [Fact]
    public async Task Report_hide_assertions_hides_for_all_scenarios()
    {
        await ExpandBothScenariosAndRender("AssertReportHide.html");

        // First show them
        await ReportAssertionShowBtn.ClickAsync();
        await Page.WaitForFunctionAsync(
            @"() => {
                var svgs = document.querySelectorAll('details.scenario [data-diagram-type=""plantuml""] svg');
                return svgs.length >= 2 &&
                    Array.from(svgs).every(s => s.outerHTML.includes('status code'));
            }",
            null, new() { Timeout = 30000, PollingInterval = 200 });

        // Now hide them
        await ReportAssertionHideBtn.ClickAsync();
        await Page.WaitForFunctionAsync(
            @"() => {
                var svgs = document.querySelectorAll('details.scenario [data-diagram-type=""plantuml""] svg');
                return svgs.length >= 2 &&
                    Array.from(svgs).every(s => !s.outerHTML.includes('status code'));
            }",
            null, new() { Timeout = 30000, PollingInterval = 200 });

        Assert.False(await SvgContainsAssertionText(ScenarioLocator(0), "Scenario1"));
        Assert.False(await SvgContainsAssertionText(ScenarioLocator(1), "Scenario2"));
    }

    // === Assertion toggle combined with Details radio buttons ===

    [Fact]
    public async Task Assertions_survive_details_state_change()
    {
        await ExpandBothScenariosAndRender("AssertWithDetails.html");

        var scenario1 = ScenarioLocator(0);

        // Show assertions for scenario 1
        var svgBefore = await GetScenarioSvgHtml(scenario1);
        await AssertionShowBtn(scenario1).ClickAsync();
        await WaitForSvgChange(scenario1, svgBefore);
        Assert.True(await SvgContainsAssertionText(scenario1, "Scenario1"));

        // Now change details to Expand — wait for render queue to process
        await scenario1.Locator(".details-radio-btn[data-state='expanded']").ClickAsync();
        await Page.WaitForFunctionAsync("() => !window._plantumlRendering",
            null, new() { Timeout = 15000, PollingInterval = 200 });

        // Assertions should still be visible
        Assert.True(await SvgContainsAssertionText(scenario1, "Scenario1"));
    }

    [Fact]
    public async Task Assertions_survive_details_collapse()
    {
        await ExpandBothScenariosAndRender("AssertWithCollapse.html");

        var scenario1 = ScenarioLocator(0);

        // Show assertions
        var svgBefore = await GetScenarioSvgHtml(scenario1);
        await AssertionShowBtn(scenario1).ClickAsync();
        await WaitForSvgChange(scenario1, svgBefore);

        // Change to collapsed
        svgBefore = await GetScenarioSvgHtml(scenario1);
        await scenario1.Locator(".details-radio-btn[data-state='collapsed']").ClickAsync();
        await WaitForSvgChange(scenario1, svgBefore);

        // Assertions should still be present
        Assert.True(await SvgContainsAssertionText(scenario1, "Scenario1"));
    }

    // === Assertion toggle combined with Headers radio buttons ===

    [Fact]
    public async Task Assertions_survive_headers_hide()
    {
        await ExpandBothScenariosAndRender("AssertWithHeadersHide.html");

        var scenario1 = ScenarioLocator(0);

        // Show assertions
        var svgBefore = await GetScenarioSvgHtml(scenario1);
        await AssertionShowBtn(scenario1).ClickAsync();
        await WaitForSvgChange(scenario1, svgBefore);

        // Hide headers
        svgBefore = await GetScenarioSvgHtml(scenario1);
        await scenario1.Locator(".headers-radio-btn[data-hstate='hidden']").ClickAsync();
        await WaitForSvgChange(scenario1, svgBefore);

        // Assertions should still be present
        Assert.True(await SvgContainsAssertionText(scenario1, "Scenario1"));
    }

    [Fact]
    public async Task Headers_hide_does_not_restore_hidden_assertions()
    {
        await ExpandBothScenariosAndRender("AssertHeadersNoRestore.html");

        var scenario1 = ScenarioLocator(0);

        // Assertions start hidden (default). Toggle headers hide — wait for render queue
        await scenario1.Locator(".headers-radio-btn[data-hstate='hidden']").ClickAsync();
        await Page.WaitForFunctionAsync("() => !window._plantumlRendering",
            null, new() { Timeout = 15000, PollingInterval = 200 });

        // Assertions should still be hidden
        Assert.False(await SvgContainsAssertionText(scenario1, "Scenario1"));
    }

    // === Radio button active-state CSS class correctness ===

    [Fact]
    public async Task Scenario_show_button_gets_active_class()
    {
        await ExpandBothScenariosAndRender("AssertActiveClass.html");

        var scenario1 = ScenarioLocator(0);

        // Initially Hide should be active
        await Expect(AssertionHideBtn(scenario1)).ToHaveClassAsync(
            new System.Text.RegularExpressions.Regex("details-active"));
        await Expect(AssertionShowBtn(scenario1)).Not.ToHaveClassAsync(
            new System.Text.RegularExpressions.Regex("details-active"));

        // Click Show
        await AssertionShowBtn(scenario1).ClickAsync();

        // Show should now be active, Hide should not
        await Expect(AssertionShowBtn(scenario1)).ToHaveClassAsync(
            new System.Text.RegularExpressions.Regex("details-active"));
        await Expect(AssertionHideBtn(scenario1)).Not.ToHaveClassAsync(
            new System.Text.RegularExpressions.Regex("details-active"));
    }

    [Fact]
    public async Task Report_show_syncs_all_scenario_buttons()
    {
        await ExpandBothScenariosAndRender("AssertReportSync.html");

        await ReportAssertionShowBtn.ClickAsync();

        // Both scenario-level Show buttons should become active
        await Expect(AssertionShowBtn(ScenarioLocator(0))).ToHaveClassAsync(
            new System.Text.RegularExpressions.Regex("details-active"));
        await Expect(AssertionShowBtn(ScenarioLocator(1))).ToHaveClassAsync(
            new System.Text.RegularExpressions.Regex("details-active"));
    }

    // === Scenario isolation after report-level then individual override ===

    [Fact]
    public async Task Scenario_override_after_report_show_is_isolated()
    {
        await ExpandBothScenariosAndRender("AssertOverrideIsolated.html");

        var scenario1 = ScenarioLocator(0);
        var scenario2 = ScenarioLocator(1);

        // Report-level Show all
        await ReportAssertionShowBtn.ClickAsync();
        await Page.WaitForFunctionAsync(
            @"() => {
                var svgs = document.querySelectorAll('details.scenario [data-diagram-type=""plantuml""] svg');
                return svgs.length >= 2 &&
                    Array.from(svgs).every(s => s.outerHTML.includes('status code'));
            }",
            null, new() { Timeout = 30000, PollingInterval = 200 });

        // Now hide for scenario 1 only
        var s1SvgBefore = await GetScenarioSvgHtml(scenario1);
        await AssertionHideBtn(scenario1).ClickAsync();
        await WaitForSvgChange(scenario1, s1SvgBefore);

        // Scenario 1 hidden, scenario 2 still shown
        Assert.False(await SvgContainsAssertionText(scenario1, "Scenario1"));
        Assert.True(await SvgContainsAssertionText(scenario2, "Scenario2"));
    }

    [Fact]
    public async Task Scenario_show_after_report_hide_is_isolated()
    {
        await ExpandBothScenariosAndRender("AssertShowAfterHide.html");

        var scenario1 = ScenarioLocator(0);
        var scenario2 = ScenarioLocator(1);

        // Assertions are already hidden by default; show for scenario 1 only
        var s1SvgBefore = await GetScenarioSvgHtml(scenario1);
        await AssertionShowBtn(scenario1).ClickAsync();
        await WaitForSvgChange(scenario1, s1SvgBefore);

        // Scenario 1 shown, scenario 2 still hidden
        Assert.True(await SvgContainsAssertionText(scenario1, "Scenario1"));
        Assert.False(await SvgContainsAssertionText(scenario2, "Scenario2"));
    }
}
