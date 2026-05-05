namespace TestTrackingDiagrams.Tests.EndToEnd;

[Collection(PlaywrightCollections.Reports)]
public class LoadingMessageTests : PlaywrightTestBase
{
    public LoadingMessageTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Body_has_plantuml_ready_class_after_page_load()
    {
        await Page.GotoAsync(GenerateReport("BodyPlantumlReady.html"));
        await Page.WaitForFunctionAsync(
            "() => document.body.classList.contains('plantuml-ready')");
        var classes = await Page.Locator("body").GetAttributeAsync("class");
        Assert.Contains("plantuml-ready", classes!);
    }

    [Fact]
    public async Task Unrendered_diagram_shows_rendering_message_not_waiting()
    {
        await Page.GotoAsync(GenerateReport("LoadingMsgRendering.html"));
        await Page.WaitForFunctionAsync(
            "() => document.body.classList.contains('plantuml-ready')");
        await ExpandFirstScenarioWithDiagram();

        var message = await Page.EvaluateAsync<string?>("""
            () => {
                var diagrams = document.querySelectorAll('.plantuml-browser:not([data-rendered])');
                for (var i = 0; i < diagrams.length; i++) {
                    var before = window.getComputedStyle(diagrams[i], '::before').getPropertyValue('content');
                    if (before && before !== 'none' && before !== 'normal') return before;
                }
                return null;
            }
        """);

        Assert.NotNull(message);
        Assert.DoesNotContain("Waiting for page load", message);
        Assert.Contains("Rendering diagram", message);
    }
}
