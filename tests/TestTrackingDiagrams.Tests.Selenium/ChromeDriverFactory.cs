using OpenQA.Selenium.Chrome;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Creates a <see cref="ChromeDriver"/> using the system-installed Chrome and
/// a locally cached chromedriver, bypassing Selenium Manager entirely.
/// This avoids Selenium Manager downloading Chrome for Testing, which triggers
/// false-positive antivirus detections (Trojan:Win32/Posilod.EB!cl).
/// </summary>
internal static class ChromeDriverFactory
{
    private static readonly string[] SystemChromePaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe")
    ];

    public static ChromeDriver Create(int width = 1920, int height = 1080, bool headless = true)
    {
        var options = new ChromeOptions();
        if (headless)
        {
            options.AddArgument("--headless=new");
            options.AddArgument($"--window-size={width},{height}");
        }
        else
        {
            options.AddArgument("--start-maximized");
        }
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-dev-shm-usage");

        var systemChrome = SystemChromePaths.FirstOrDefault(File.Exists);
        if (systemChrome is not null)
            options.BinaryLocation = systemChrome;

        // Look for chromedriver in the Selenium Manager cache to avoid invoking
        // Selenium Manager (which re-downloads Chrome for Testing).
        var driverDir = FindCachedChromeDriver();
        if (driverDir is not null)
        {
            var service = ChromeDriverService.CreateDefaultService(driverDir);
            return new ChromeDriver(service, options);
        }

        return new ChromeDriver(options);
    }

    private static string? FindCachedChromeDriver()
    {
        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "selenium", "chromedriver");

        if (!Directory.Exists(cacheRoot))
            return null;

        // Find the newest chromedriver directory that contains the executable
        return Directory.GetDirectories(cacheRoot, "*", SearchOption.AllDirectories)
            .Where(d => File.Exists(Path.Combine(d, "chromedriver.exe"))
                     || File.Exists(Path.Combine(d, "chromedriver")))
            .OrderByDescending(Directory.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
