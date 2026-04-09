using System.IO.Compression;
using System.Net;
using System.Text;
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

        // Server-rendered mode: raw-plantuml pre
        var sources = document.QuerySelectorAll("div.raw-plantuml pre")
            .Select(el => WebUtility.HtmlDecode(el.InnerHtml).Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        if (sources.Length > 0)
            return sources;

        // BrowserJs / InlineSvg mode: data-plantuml inside scenario containers
        // Filter to sequence diagrams only (exclude activity/component diagrams)
        sources = document.QuerySelectorAll("details.scenario [data-plantuml], div.scenario [data-plantuml]")
            .Select(el => WebUtility.HtmlDecode(el.GetAttribute("data-plantuml") ?? "").Trim())
            .Where(s => s.Length > 0 && s.Contains("participant "))
            .ToArray();

        if (sources.Length > 0)
            return sources;

        // BrowserJs / InlineSvg mode: data-plantuml-z (gzip+base64 compressed)
        return document.QuerySelectorAll("details.scenario [data-plantuml-z], div.scenario [data-plantuml-z]")
            .Select(el => DecompressFromBase64(el.GetAttribute("data-plantuml-z") ?? ""))
            .Where(s => s.Length > 0 && s.Contains("participant "))
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

            // BrowserJs / InlineSvg fallback
            if (plantUmlSources.Length == 0)
            {
                plantUmlSources = scenario.QuerySelectorAll("[data-plantuml]")
                    .Select(el => WebUtility.HtmlDecode(el.GetAttribute("data-plantuml") ?? "").Trim())
                    .Where(s => s.Length > 0)
                    .ToArray();
            }

            // Compressed fallback (data-plantuml-z)
            if (plantUmlSources.Length == 0)
            {
                plantUmlSources = scenario.QuerySelectorAll("[data-plantuml-z]")
                    .Select(el => DecompressFromBase64(el.GetAttribute("data-plantuml-z") ?? ""))
                    .Where(s => s.Length > 0)
                    .ToArray();
            }

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

    public record DiagramImgInfo(string Src, bool HasLazyLoading);

    public static async Task<DiagramImgInfo[]> ExtractDiagramImgsAsync(string htmlFilePath)
    {
        var html = await File.ReadAllTextAsync(htmlFilePath);
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(req => req.Content(html));

        return document.QuerySelectorAll("details.example img, details.example-diagrams img")
            .Select(img => new DiagramImgInfo(
                img.GetAttribute("src") ?? "",
                img.GetAttribute("loading") == "lazy"))
            .Where(d => d.Src.Length > 0)
            .ToArray();
    }

    private static string DecompressFromBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return "";

        var bytes = Convert.FromBase64String(base64);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd().Trim();
    }
}
