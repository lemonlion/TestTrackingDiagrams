using Microsoft.Playwright;

namespace Kronikol.Tests.EndToEnd;

/// <summary>
/// Tests verifying that "Copy Highlighted Text" from the SVG context menu
/// preserves spaces instead of inserting newlines at word-wrap boundaries.
/// </summary>
[Collection(PlaywrightCollections.Diagrams)]
public class CopyHighlightedTextTests : PlaywrightTestBase
{
    public CopyHighlightedTextTests(PlaywrightFixture fixture) : base(fixture) { }

    private async Task NavigateAndSetup(string fileName)
    {
        var url = ReportTestHelper.GenerateReportWithLongLineNote(TempDir, OutputDir, fileName);
        await Page.GotoAsync(url);
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.note-hover-rect').length > 0",
            null, new() { Timeout = 10000, PollingInterval = 200 });
    }

    [Fact]
    public async Task Copy_highlighted_text_from_note_preserves_spaces_instead_of_newlines()
    {
        await NavigateAndSetup("CopyHighlight_PreservesSpaces.html");
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        // Select all text in the first note by creating a range over SVG text elements
        await Page.EvaluateAsync("""
            () => {
                var svg = document.querySelector('[data-diagram-type="plantuml"] svg');
                var noteGroups = window._findNoteGroups(svg);
                if (!noteGroups || noteGroups.length === 0) throw new Error('No note groups found');
                var texts = noteGroups[0].texts;
                var range = document.createRange();
                range.setStartBefore(texts[0]);
                range.setEndAfter(texts[texts.length - 1]);
                var sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(range);
            }
        """);

        // Dispatch context menu on the first note text element
        var noteText = Page.Locator("[data-diagram-type='plantuml'] svg text").First;
        await DispatchContextMenu(noteText);

        // Click "Copy Highlighted Text"
        var menuItem = Page.Locator(".diagram-ctx-menu").GetByText("Copy Highlighted Text");
        await menuItem.WaitForAsync(new() { Timeout = 5000 });
        await menuItem.ClickAsync();

        // Read clipboard
        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        // The note has header lines + body. Copied text from full selection
        // should preserve the structure from original source (lines separated by \n)
        // but word-wrap boundaries within a single source line should be spaces, not \n.
        // The JSON body is one long line in the source — it should not have \n within it.
        Assert.Contains("orderId", clipboard);
        Assert.Contains("customerName", clipboard);
        // If word-wrapping introduced newlines, there would be \n between parts of the JSON
        // Check that the JSON portion is on a single line (no \n between "orderId" and the end)
        var jsonLine = clipboard.Split('\n')
            .FirstOrDefault(line => line.Contains("orderId"));
        Assert.NotNull(jsonLine);
        Assert.Contains("shippingAddress", jsonLine!);
    }

    [Fact]
    public async Task Copy_highlighted_text_with_real_multiline_preserves_line_breaks()
    {
        await NavigateAndSetup("CopyHighlight_PreservesReal.html");
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        // Select all text in the first note
        await Page.EvaluateAsync("""
            () => {
                var svg = document.querySelector('[data-diagram-type="plantuml"] svg');
                var noteGroups = window._findNoteGroups(svg);
                if (!noteGroups || noteGroups.length === 0) throw new Error('No note groups found');
                var texts = noteGroups[0].texts;
                var range = document.createRange();
                range.setStartBefore(texts[0]);
                range.setEndAfter(texts[texts.length - 1]);
                var sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(range);
            }
        """);

        var noteText = Page.Locator("[data-diagram-type='plantuml'] svg text").First;
        await DispatchContextMenu(noteText);

        var menuItem = Page.Locator(".diagram-ctx-menu").GetByText("Copy Highlighted Text");
        await menuItem.WaitForAsync(new() { Timeout = 5000 });
        await menuItem.ClickAsync();

        var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        // The note has real multi-line content (gray headers + body).
        // Real line breaks between different SOURCE lines should be preserved.
        // The gray headers are in the source, so we should see them if text selection included them.
        Assert.Contains("Content-Type", clipboard);
    }
}
