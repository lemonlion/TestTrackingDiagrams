namespace TestTrackingDiagrams.Tests.EndToEnd;

[Collection(PlaywrightCollections.Reports)]
public class AttachmentRenderingTests : PlaywrightTestBase
{
    public AttachmentRenderingTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Step_with_attachment_renders_link()
    {
        await Page.GotoAsync(GenerateReportWithAttachments("AttLink.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        var attachmentLink = Page.Locator("a.step-attachment").First;
        await attachmentLink.WaitForAsync();
        var text = await attachmentLink.InnerTextAsync();
        Assert.Equal("screenshot.png", text);
    }

    [Fact]
    public async Task Attachment_link_has_correct_href()
    {
        await Page.GotoAsync(GenerateReportWithAttachments("AttHref.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        var attachmentLink = Page.Locator("a.step-attachment").First;
        await attachmentLink.WaitForAsync();
        var href = await attachmentLink.GetAttributeAsync("href");
        Assert.Equal("files/screenshot.png", href);
    }

    [Fact]
    public async Task Step_with_multiple_attachments_renders_all()
    {
        await Page.GotoAsync(GenerateReportWithAttachments("AttMultiple.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // Find the scenario with multiple attachments by title
        var scenario = Page.Locator("details.scenario", new() { HasTextString = "Upload with multiple attachments" });
        var links = scenario.Locator("a.step-attachment");
        Assert.Equal(2, await links.CountAsync());

        var texts = await links.AllInnerTextsAsync();
        Assert.Contains("log.txt", texts);
        Assert.Contains("trace.json", texts);
    }

    [Fact]
    public async Task Step_without_attachments_has_no_link()
    {
        await Page.GotoAsync(GenerateReportWithAttachments("AttNone.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Features" }).ClickAsync();
        await Page.Locator("button.collapse-expand-all", new() { HasTextString = "Expand All Scenarios" }).ClickAsync();

        // Find the scenario without attachments by title
        var scenario = Page.Locator("details.scenario", new() { HasTextString = "Step without attachments" });
        var links = scenario.Locator("a.step-attachment");
        Assert.Equal(0, await links.CountAsync());
    }
}
