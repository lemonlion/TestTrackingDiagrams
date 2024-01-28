using Humanizer;
using System.Text.Json.Nodes;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.PlantUml;

public class PlantUmlCreator
{
    private static readonly string[] ExcludedHeaders = { "Cache-Control", "Pragma" };
    private const int MaxLineWidth = 600;

    public static IEnumerable<PlantUmlForTest> GetPlantUmlImageTagsPerTestName(IEnumerable<RequestResponseLog>? requestResponses, string plantUmlServerRendererUrl = "https://www.plantuml.com/plantuml/png", Func<string, string>? processor = null)
    {
        var requestsResponseByTraceIdAndTest = requestResponses.GroupBy(x => x.TestId);

        var plantUmlPerTestName = requestsResponseByTraceIdAndTest?.Select(testTraces =>
        {
            var traces = testTraces.ToList();
            var testName = testTraces.First().TestName;
            var result = CreatePlantUml(traces, processor);
            var imageTag = result.GetPlantUmlImageTag(plantUmlServerRendererUrl);
            return new PlantUmlForTest(testTraces.Key, testName, result.PlantUml, result.PlantUmlEncoded, testTraces.ToList(), imageTag);
        });

        return plantUmlPerTestName ?? Enumerable.Empty<PlantUmlForTest>();
    }

    private static PlantUmlResult CreatePlantUml(List<RequestResponseLog> tracesForTest, Func<string, string>? processor = null)
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
                    (trace.Headers, trace.Content, ExcludedHeaders);

                requestPlantUmlNoteContent = processor.Invoke(requestPlantUmlNoteContent);

                plantUml +=
                    $"{callerShortName} -> {serviceShortName}: {trace.Method}: {trace.Uri.PathAndQuery}{Environment.NewLine}" +
                    $"note left{Environment.NewLine}" +
                    $"{requestPlantUmlNoteContent}{Environment.NewLine}" +
                    $"end note{Environment.NewLine}";
            }

            if (trace.Type == RequestResponseType.Response)
            {
                var responsePlantUmlNoteContent = GetPlantUmlForRequestOrResponseNote
                    (trace.Headers, trace.Content, ExcludedHeaders);

                responsePlantUmlNoteContent = processor.Invoke(responsePlantUmlNoteContent);

                plantUml +=
                    $"{serviceShortName} --> {callerShortName}: {trace.StatusCode.ToString().Titleize()}{Environment.NewLine}" +
                    $"note right{Environment.NewLine}" +
                    $"{responsePlantUmlNoteContent}{Environment.NewLine}" +
                    $"end note{Environment.NewLine}";
            }
        }

        plantUml += $"@enduml{Environment.NewLine}";

        var encodedPlantUml = PlantUmlTextEncoder.Encode(plantUml); ;

        return new PlantUmlResult(plantUml, encodedPlantUml);
    }

    private static string GetPlantUmlForRequestOrResponseNote(IEnumerable<(string Key, string? Value)> headers, string? content, string[] excludedHeaders)
    {
        return (($"{string.Join(Environment.NewLine, headers
            .Where(y => !excludedHeaders.Contains(y.Key))
            .Select(y => $"$my_code(gray)[{y.Key}={y.Value}]"))}" + Environment.NewLine).TrimStart() +
                Environment.NewLine +
                $"{(content?.StartsWith("{") ?? false 
                    ? JsonNode.Parse(content).ToString() 
                    : content?.Replace("&", Environment.NewLine))}".Trim()).Trim();
    }

    public record PlantUmlResult(string PlantUml, string PlantUmlEncoded)
    {
        public string GetPlantUmlImageTag(string plantUmlServerRendererUrl) => $"<img src=\"{plantUmlServerRendererUrl.TrimEnd('/')}/{PlantUmlEncoded}\">";
    };

    public record PlantUmlForTest(Guid TestId, string TestName, string PlantUml, string PlantUmlEncoded, IEnumerable<RequestResponseLog> Traces, string ImageTag);
}