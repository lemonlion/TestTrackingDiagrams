namespace TestTrackingDiagrams.Tests.EndToEnd;

/// <summary>
/// xUnit assembly fixture that shares a single Playwright browser process across all test classes.
/// Each test creates its own BrowserContext + Page, enabling full parallel execution.
/// </summary>
[CollectionDefinition(Name)]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "Playwright";
}
