using Microsoft.Playwright;

namespace TestTrackingDiagrams.Tests.EndToEnd;

/// <summary>
/// Shared Playwright browser instance for all test classes via xUnit assembly fixture.
/// Each test class gets its own <see cref="BrowserContext"/> via <see cref="NewContextAsync"/>,
/// so tests are fully isolated and can run in parallel.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public IBrowser Browser => _browser;

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    public Task<IBrowserContext> NewContextAsync(int width = 1920, int height = 1080)
    {
        return _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
    }
}
