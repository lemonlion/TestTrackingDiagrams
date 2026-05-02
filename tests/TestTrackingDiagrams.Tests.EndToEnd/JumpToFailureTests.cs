namespace TestTrackingDiagrams.Tests.EndToEnd;

public class JumpToFailureTests : PlaywrightTestBase
{
    public JumpToFailureTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Jump_to_failure_scrolls_scenario_title_into_view()
    {
        await Page.GotoAsync(GenerateReport("JumpToFailureScroll.html"));

        var jumpBtn = Page.Locator("button.jump-to-failure");
        await jumpBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await jumpBtn.ClickAsync();

        // Wait for smooth scroll to settle
        await Page.WaitForTimeoutAsync(1000);

        var failedSummary = Page.Locator("details.scenario[data-status='Failed'] > summary");
        var isInViewport = await failedSummary.EvaluateAsync<bool>("""
            el => {
                var rect = el.getBoundingClientRect();
                return rect.top >= 0 && rect.top < window.innerHeight;
            }
        """);

        Assert.True(isInViewport, "Failed scenario summary should be visible in viewport after jump-to-failure");
    }

    [Fact]
    public async Task Jump_to_failure_counter_updates()
    {
        await Page.GotoAsync(GenerateReport("JumpToFailureCounter.html"));

        var jumpBtn = Page.Locator("button.jump-to-failure");
        await jumpBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var counter = Page.Locator("#failure-counter");
        var text = await counter.TextContentAsync();
        Assert.Contains("0/", text!);

        await jumpBtn.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        text = await counter.TextContentAsync();
        Assert.Contains("1/", text!);
    }

    [Fact]
    public async Task Jump_to_failure_opens_feature_and_scenario()
    {
        await Page.GotoAsync(GenerateReport("JumpToFailureOpens.html"));

        var jumpBtn = Page.Locator("button.jump-to-failure");
        await jumpBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await jumpBtn.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        var failedScenario = Page.Locator("details.scenario[data-status='Failed']");
        var isOpen = await failedScenario.GetAttributeAsync("open");
        Assert.NotNull(isOpen);

        var parentFeature = Page.Locator("details.scenario[data-status='Failed']")
            .Locator("xpath=ancestor::details[contains(@class,'feature')]");
        var featureOpen = await parentFeature.GetAttributeAsync("open");
        Assert.NotNull(featureOpen);
    }
}
