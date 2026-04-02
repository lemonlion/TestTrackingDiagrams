using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ComponentDiagram;

public static class ComponentDiagramReportGenerator
{
    public record ComponentDiagramResult(string PumlFilePath, string HtmlFilePath, string PlantUml);

    public static ComponentDiagramResult GenerateComponentDiagramReport(
        IEnumerable<RequestResponseLog> logs,
        ComponentDiagramOptions? options = null)
    {
        options ??= new ComponentDiagramOptions();

        var relationships = ComponentDiagramGenerator.ExtractRelationships(logs, options.ParticipantFilter);
        var plantUml = ComponentDiagramGenerator.GeneratePlantUml(relationships, options);

        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(directory);

        var pumlPath = Path.Combine(directory, $"{options.FileName}.puml");
        File.WriteAllText(pumlPath, plantUml);

        var html = GenerateHtml(plantUml, options.Title);
        var htmlPath = Path.Combine(directory, $"{options.FileName}.html");
        File.WriteAllText(htmlPath, html);

        return new ComponentDiagramResult(pumlPath, htmlPath, plantUml);
    }

    private static string GenerateHtml(string plantUml, string title)
    {
        return $$"""
                <html>
                    <head>
                        <style>
                            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 2rem; }
                            h1 { color: #333; }
                            pre { background: #f6f8fa; padding: 1rem; border-radius: 6px; overflow-x: auto; font-size: 0.85rem; }
                            .diagram-container { margin: 1rem 0; }
                        </style>
                    </head>
                    <body>
                        <h1>{{title}}</h1>
                        <div class="diagram-container">
                            <details open>
                                <summary><strong>PlantUML Source</strong></summary>
                                <pre>{{plantUml}}</pre>
                            </details>
                        </div>
                    </body>
                </html>
                """;
    }
}
