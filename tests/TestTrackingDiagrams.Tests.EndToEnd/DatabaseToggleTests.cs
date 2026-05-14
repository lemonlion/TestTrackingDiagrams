namespace TestTrackingDiagrams.Tests.EndToEnd;

/// <summary>
/// Tests that verify the "Databases Shown/Hidden" toggle removes database
/// participant declarations and all arrows to/from database aliases.
/// </summary>
[Collection(PlaywrightCollections.Notes)]
public class DatabaseToggleTests : DiagramNotePlaywrightBase
{
    public DatabaseToggleTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Databases_toggle_button_is_rendered_when_database_participant_exists()
    {
        await Page.GotoAsync(GenerateDatabaseParticipantReport("DbToggleBtn.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        var btn = Page.Locator("button[data-toggle='databases']").First;
        await btn.WaitForAsync(new() { Timeout = 5000 });
        var text = await btn.TextContentAsync();
        Assert.Equal("Databases Shown", text);
        Assert.True(await btn.EvaluateAsync<bool>("el => el.classList.contains('details-active')"));
    }

    [Fact]
    public async Task Databases_toggle_hides_database_participant_and_arrows()
    {
        await Page.GotoAsync(GenerateDatabaseParticipantReport("DbToggleHide.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        // Verify database is present initially
        var sourceBefore = await GetDataPlantuml();
        Assert.Contains("database", sourceBefore);
        Assert.Contains("cosmosdb", sourceBefore);

        // Click "Databases Shown" to hide
        await Page.Locator("button[data-toggle='databases']").First.ClickAsync();

        // Wait for re-render
        await Page.WaitForFunctionAsync("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                if (!container || container._noteRendering || window._plantumlRendering) return false;
                var source = container.getAttribute('data-plantuml');
                return source && !source.includes('cosmosdb');
            }
        """, null, new() { Timeout = 15000, PollingInterval = 200 });

        // Verify database declaration and arrows are gone
        var sourceAfter = await GetDataPlantuml();
        Assert.DoesNotContain("database", sourceAfter);
        Assert.DoesNotContain("cosmosdb", sourceAfter);
        // Non-database arrows should remain
        Assert.Contains("caller", sourceAfter);
        Assert.Contains("svc", sourceAfter);
    }

    [Fact]
    public async Task Databases_toggle_shows_database_after_hiding()
    {
        await Page.GotoAsync(GenerateDatabaseParticipantReport("DbToggleShow.html"));
        await Page.Locator("details.feature").First.WaitForAsync();
        await ExpandFirstScenarioWithDiagram();
        await WaitForDiagramSvg();

        // Hide databases
        await Page.Locator("button[data-toggle='databases']").First.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                if (!container || container._noteRendering || window._plantumlRendering) return false;
                var source = container.getAttribute('data-plantuml');
                return source && !source.includes('cosmosdb');
            }
        """, null, new() { Timeout = 15000, PollingInterval = 200 });

        // Show databases again
        await Page.Locator("button[data-toggle='databases']").First.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => {
                var container = document.querySelector('[data-diagram-type="plantuml"]');
                if (!container || container._noteRendering || window._plantumlRendering) return false;
                var source = container.getAttribute('data-plantuml');
                return source && source.includes('cosmosdb');
            }
        """, null, new() { Timeout = 15000, PollingInterval = 200 });

        var sourceAfterShow = await GetDataPlantuml();
        Assert.Contains("database", sourceAfterShow);
        Assert.Contains("cosmosdb", sourceAfterShow);
    }

    [Fact]
    public async Task Databases_toggle_button_text_changes_on_click()
    {
        await Page.GotoAsync(GenerateDatabaseParticipantReport("DbToggleText.html"));
        await Page.Locator("details.feature").First.WaitForAsync();

        var btn = Page.Locator("button[data-toggle='databases']").First;
        await btn.WaitForAsync(new() { Timeout = 5000 });

        Assert.Equal("Databases Shown", await btn.TextContentAsync());
        await btn.ClickAsync();

        await Page.WaitForFunctionAsync("""
            () => {
                var btn = document.querySelector("button[data-toggle='databases']");
                return btn && btn.textContent === 'Databases Hidden';
            }
        """, null, new() { Timeout = 5000, PollingInterval = 200 });

        Assert.Equal("Databases Hidden", await btn.TextContentAsync());
        Assert.False(await btn.EvaluateAsync<bool>("el => el.classList.contains('details-active')"));
    }

    private async Task<string> GetDataPlantuml()
    {
        return await Page.EvaluateAsync<string>("""
            () => document.querySelector('[data-diagram-type="plantuml"]').getAttribute('data-plantuml') || ''
        """);
    }
}
