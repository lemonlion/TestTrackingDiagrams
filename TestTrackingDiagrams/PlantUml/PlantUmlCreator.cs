using Humanizer;
using System.Text.Json;
using System.Text.Json.Nodes;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.PlantUml;

public static class PlantUmlCreator
{
    private const int MaxLineWidth = 600;

    public static string[] DefaultExcludedHeaders => ["Cache-Control", "Pragma"];

    public static IEnumerable<PlantUmlForTest> GetPlantUmlImageTagsPerTestName(
        IEnumerable<RequestResponseLog>? requestResponses,
        string plantUmlServerRendererUrl = "https://www.plantuml.com/plantuml/png",
        Func<string, string>? processor = null,
        string[]? excludedHeaders = null)
    {
        excludedHeaders ??= DefaultExcludedHeaders;

        var requestsResponseByTraceIdAndTest = requestResponses?.GroupBy(x => x.TestId);

        var plantUmlPerTestName = requestsResponseByTraceIdAndTest?.Select(testTraces =>
        {
            var traces = testTraces.ToList();
            var testName = testTraces.First().TestName;
            var result = CreatePlantUml(traces, processor, excludedHeaders);
            var imageTag = result.GetPlantUmlImageTag(plantUmlServerRendererUrl);
            return new PlantUmlForTest(testTraces.Key, testName, result.PlantUml, result.PlantUmlEncoded, testTraces.ToList(), imageTag);
        });

        return plantUmlPerTestName ?? [];
    }

    private static PlantUmlResult CreatePlantUml(
        List<RequestResponseLog> tracesForTest,
        Func<string, string>? processor,
        string[] excludedHeaders)
    {
        var plantUml =
            $"@startuml{Environment.NewLine}" +
            $"skinparam wrapWidth {MaxLineWidth}{Environment.NewLine}" +
            $"!function $my_code($fgcolor){Environment.NewLine}" +
            $"!return \"<color:\"+$fgcolor+\">\"{Environment.NewLine}" +
            $"!endfunction{Environment.NewLine}";

        var actorDefined = false;
        var currentPlayers = new List<string>();

        foreach (var trace in tracesForTest)
        {
            var serviceShortName = trace.ServiceName.Camelize();
            var callerShortName = trace.CallerName.Camelize();

            if (!currentPlayers.Contains(callerShortName))
            {
                plantUml += $"{(actorDefined ? "entity" : "actor")} \"{trace.CallerName}\" as {callerShortName}{Environment.NewLine}";
                currentPlayers.Add(callerShortName);
            }

            if (!currentPlayers.Contains(serviceShortName))
            {
                plantUml += $"entity \"{trace.ServiceName}\" as {serviceShortName}{Environment.NewLine}";
                currentPlayers.Add(serviceShortName);
            }

            if (trace.Type == RequestResponseType.Request)
            {
                var requestPlantUmlNoteContent = GetPlantUmlForRequestOrResponseNote
                    (trace.Headers, trace.Content, excludedHeaders);

                if (processor != null)
                    requestPlantUmlNoteContent = processor.Invoke(requestPlantUmlNoteContent);

                plantUml +=
                    $"{callerShortName} -> {serviceShortName}: {trace.Method}: {trace.Uri.PathAndQuery}{Environment.NewLine}";

                if (!string.IsNullOrEmpty(requestPlantUmlNoteContent))
                {
                    plantUml +=
                        $"note left{Environment.NewLine}" +
                        $"{requestPlantUmlNoteContent}{Environment.NewLine}" +
                        $"end note{Environment.NewLine}";
                }
            }

            if (trace.Type == RequestResponseType.Response)
            {
                var responsePlantUmlNoteContent = GetPlantUmlForRequestOrResponseNote
                    (trace.Headers, trace.Content, excludedHeaders);

                if (processor != null)
                    responsePlantUmlNoteContent = processor.Invoke(responsePlantUmlNoteContent);

                plantUml +=
                    $"{serviceShortName} --> {callerShortName}: {trace.StatusCode.ToString().Titleize()}{Environment.NewLine}";

                if (!string.IsNullOrEmpty(responsePlantUmlNoteContent))
                {
                    plantUml +=
                        $"note right{Environment.NewLine}" +
                        $"{responsePlantUmlNoteContent}{Environment.NewLine}" +
                        $"end note{Environment.NewLine}";
                }
            }
        }

        plantUml += $"@enduml{Environment.NewLine}";

        var encodedPlantUml = PlantUmlTextEncoder.Encode(plantUml); ;

        return new PlantUmlResult(plantUml, encodedPlantUml);
    }

    private static string GetPlantUmlForRequestOrResponseNote(IEnumerable<(string Key, string? Value)> headers, string? content, string[] excludedHeaders)
    {
        var parsedContent = string.Empty;
        var isContentJson = false;
        if (content?.StartsWith("{") ?? false)
        {
            try
            {
                parsedContent = JsonNode.Parse(content).ToString();
                isContentJson = true;
            }
            catch (JsonException) { }
        }

        if (!isContentJson)
        {
            parsedContent = content?.Replace("&", Environment.NewLine);
        }

        return (($"{string.Join(Environment.NewLine, headers
            .Where(y => !excludedHeaders.Contains(y.Key))
            .Select(y => $"$my_code(gray)[{y.Key}={y.Value}]"))}" + Environment.NewLine).TrimStart() +
                Environment.NewLine +
                $"{parsedContent}".Trim()).Trim();
    }

    private record PlantUmlResult(string PlantUml, string PlantUmlEncoded)
    {
        public string GetPlantUmlImageTag(string plantUmlServerRendererUrl) => $"<img src=\"{plantUmlServerRendererUrl.TrimEnd('/')}/{PlantUmlEncoded}\">";
    };

    public record PlantUmlForTest(Guid TestId, string TestName, string PlantUml, string PlantUmlEncoded, IEnumerable<RequestResponseLog> Traces, string ImageTag);
}