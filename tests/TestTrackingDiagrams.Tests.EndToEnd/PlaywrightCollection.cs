namespace TestTrackingDiagrams.Tests.EndToEnd;

/// <summary>
/// Collection definitions that split E2E tests into parallel-running groups.
/// Each collection shares a single PlaywrightFixture (browser process), and
/// xUnit runs different collections concurrently up to maxParallelThreads.
/// </summary>
public static class PlaywrightCollections
{
    public const string Zoom = "Playwright.Zoom";
    public const string Notes = "Playwright.Notes";
    public const string Search = "Playwright.Search";
    public const string Diagrams = "Playwright.Diagrams";
    public const string Reports = "Playwright.Reports";
    public const string Scenarios = "Playwright.Scenarios";
}

[CollectionDefinition(PlaywrightCollections.Zoom)]
public class ZoomCollection : ICollectionFixture<PlaywrightFixture> { }

[CollectionDefinition(PlaywrightCollections.Notes)]
public class NotesCollection : ICollectionFixture<PlaywrightFixture> { }

[CollectionDefinition(PlaywrightCollections.Search)]
public class SearchCollection : ICollectionFixture<PlaywrightFixture> { }

[CollectionDefinition(PlaywrightCollections.Diagrams)]
public class DiagramsCollection : ICollectionFixture<PlaywrightFixture> { }

[CollectionDefinition(PlaywrightCollections.Reports)]
public class ReportsCollection : ICollectionFixture<PlaywrightFixture> { }

[CollectionDefinition(PlaywrightCollections.Scenarios)]
public class ScenariosCollection : ICollectionFixture<PlaywrightFixture> { }
