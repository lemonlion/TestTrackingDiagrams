using OpenQA.Selenium.Chrome;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Shared Chrome driver fixture for the default (1920x1080) window size.
/// Used as an <see cref="Xunit.IClassFixture{T}"/> so a single Chrome
/// process is reused across all tests within a class.
/// </summary>
public sealed class ChromeFixture : IDisposable
{
    public ChromeDriver Driver { get; } = ChromeDriverFactory.Create();

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}

/// <summary>
/// Shared Chrome driver fixture for the 1280x900 window size.
/// </summary>
public sealed class ChromeFixture1280X900 : IDisposable
{
    public ChromeDriver Driver { get; } = ChromeDriverFactory.Create(1280, 900);

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}
