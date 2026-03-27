using System.Net;
using AngleSharp;
using AngleSharp.Dom;

namespace Example.Api.Tests.Integration.Helpers;

public record ReportFiles(string? SpecificationsHtml, string? FeaturesReportHtml, string? SpecificationsYaml);

public record ParsedScenario(string Name, bool IsHappyPath, string[] PlantUmlSources);

public static class ReportParser
{
    public static ReportFiles GetReportFiles(string reportsFolderPath)
    {
        if (!Directory.Exists(reportsFolderPath))
            return new ReportFiles(null, null, null);

        var files = Directory.GetFiles(reportsFolderPath);
        return new ReportFiles(
            files.FirstOrDefault(f => f.EndsWith("ComponentSpecificationsWithExamples.html")),
            files.FirstOrDefault(f => f.EndsWith("FeaturesReport.html")),
            files.FirstOrDefault(f => f.EndsWith("ComponentSpecifications.yml")));
    }

    public static async Task<string[]> ExtractPlantUmlSourcesAsync(string htmlFilePath)
    {
        var html = await File.ReadAllTextAsync(htmlFilePath);
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(req => req.Content(html));

        return document.QuerySelectorAll("div.raw-plantuml pre")
            .Select(el => WebUtility.HtmlDecode(el.InnerHtml).Trim())
            .Where(s => s.Length > 0)
            .ToArray();
    }

    public static async Task<ParsedScenario[]> ExtractScenariosAsync(string htmlFilePath)
    {
        var html = await File.ReadAllTextAsync(htmlFilePath);
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(req => req.Content(html));

        var results = new List<ParsedScenario>();

        foreach (var scenario in document.QuerySelectorAll("details.scenario, div.scenario"))
        {
            var h3 = scenario.QuerySelector("summary.h3") ?? scenario.QuerySelector("h3");
            var name = h3?.TextContent.Replace("Happy Path", "").Trim() ?? "";
            var isHappyPath = scenario.ClassList.Contains("happy-path")
                          || scenario.QuerySelector("span.label")?.TextContent.Contains("Happy Path", StringComparison.OrdinalIgnoreCase) == true;

            var plantUmlSources = scenario.QuerySelectorAll("div.raw-plantuml pre")
                .Select(el => el.TextContent.Trim())
                .Where(s => s.Length > 0)
                .ToArray();

            results.Add(new ParsedScenario(name, isHappyPath, plantUmlSources));
        }

        return results.ToArray();
    }

    public static async Task<string?> ExtractTitleAsync(string htmlFilePath)
    {
        var html = await File.ReadAllTextAsync(htmlFilePath);
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(req => req.Content(html));
        return document.QuerySelector("h1")?.TextContent.Trim();
    }

    public static async Task<string> ReadYamlAsync(string yamlFilePath) =>
        await File.ReadAllTextAsync(yamlFilePath);
}
