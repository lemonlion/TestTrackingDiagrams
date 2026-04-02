using TestTrackingDiagrams.PlantUml;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ComponentDiagram;

public static class ComponentDiagramReportGenerator
{
    public record ComponentDiagramResult(string PumlFilePath, string HtmlFilePath, string PlantUml);

    public static ComponentDiagramResult GenerateComponentDiagramReport(
        IEnumerable<RequestResponseLog> logs,
        ComponentDiagramOptions? options = null,
        string plantUmlServerBaseUrl = "https://plantuml.com/plantuml",
        PlantUmlImageFormat imageFormat = PlantUmlImageFormat.Svg,
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer = null)
    {
        options ??= new ComponentDiagramOptions();

        var relationships = ComponentDiagramGenerator.ExtractRelationships(logs, options.ParticipantFilter);
        var plantUml = ComponentDiagramGenerator.GeneratePlantUml(relationships, options);

        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(directory);

        var pumlPath = Path.Combine(directory, $"{options.FileName}.puml");
        File.WriteAllText(pumlPath, plantUml);

        var imgSrc = GetImageSource(plantUml, plantUmlServerBaseUrl, imageFormat, localDiagramRenderer, directory, options.FileName);
        var html = GenerateHtml(plantUml, options.Title, imgSrc, imageFormat);
        var htmlPath = Path.Combine(directory, $"{options.FileName}.html");
        File.WriteAllText(htmlPath, html);

        return new ComponentDiagramResult(pumlPath, htmlPath, plantUml);
    }

    private static string GetImageSource(
        string plantUml,
        string plantUmlServerBaseUrl,
        PlantUmlImageFormat imageFormat,
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer,
        string directory,
        string fileName)
    {
        if (localDiagramRenderer is not null)
        {
            var renderFormat = imageFormat switch
            {
                PlantUmlImageFormat.Base64Png => PlantUmlImageFormat.Png,
                PlantUmlImageFormat.Base64Svg => PlantUmlImageFormat.Svg,
                _ => imageFormat
            };
            var imageBytes = localDiagramRenderer(plantUml, renderFormat);
            var isBase64 = imageFormat is PlantUmlImageFormat.Base64Png or PlantUmlImageFormat.Base64Svg;

            if (isBase64)
            {
                var mimeType = renderFormat == PlantUmlImageFormat.Png ? "image/png" : "image/svg+xml";
                return $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
            }

            var extension = renderFormat == PlantUmlImageFormat.Png ? ".png" : ".svg";
            var imageFileName = $"{fileName}{extension}";
            File.WriteAllBytes(Path.Combine(directory, imageFileName), imageBytes);
            return imageFileName;
        }

        var encoded = PlantUmlTextEncoder.Encode(plantUml);
        var formatPath = imageFormat switch
        {
            PlantUmlImageFormat.Svg or PlantUmlImageFormat.Base64Svg => "svg",
            _ => "png"
        };
        return $"{plantUmlServerBaseUrl}/{formatPath}/{encoded}";
    }

    private static string GenerateHtml(string plantUml, string title, string imgSrc, PlantUmlImageFormat imageFormat)
    {
        var imgTag = imageFormat is PlantUmlImageFormat.Svg or PlantUmlImageFormat.Base64Svg
            ? $"""<img src="{imgSrc}" alt="{title}" style="max-width: 100%;" />"""
            : $"""<img src="{imgSrc}" alt="{title}" style="max-width: 100%;" />""";

        var encodedPlantUml = System.Net.WebUtility.HtmlEncode(plantUml);

        return $$"""
                <html>
                    <head>
                        <style>
                            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 2rem; }
                            h1 { color: #333; }
                            pre { background: #f6f8fa; padding: 1rem; border-radius: 6px; overflow-x: auto; font-size: 0.85rem; }
                            .diagram-container { margin: 1rem 0; }
                            .diagram-image { margin: 1rem 0; text-align: center; }
                            .diagram-image img { max-width: 100%; height: auto; }
                        </style>
                    </head>
                    <body>
                        <h1>{{title}}</h1>
                        <div class="diagram-image">
                            {{imgTag}}
                        </div>
                        <div class="diagram-container">
                            <details>
                                <summary><strong>PlantUML Source</strong></summary>
                                <pre>{{encodedPlantUml}}</pre>
                            </details>
                        </div>
                    </body>
                </html>
                """;
    }
}
