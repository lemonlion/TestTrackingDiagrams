using Humanizer;
using System.Text.Json;
using System.Text.Json.Nodes;
using TestTrackingDiagrams.Extensions;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.PlantUml;

public static class PlantUmlCreator
{
    private const int MaxLineWidth = 800;

    public static string[] DefaultExcludedHeaders => ["Cache-Control", "Pragma"];

    public static IEnumerable<PlantUmlForTest> GetPlantUmlImageTagsPerTestId(
        IEnumerable<RequestResponseLog>? requestResponses,
        string plantUmlServerRendererUrl = "https://www.plantuml.com/plantuml/png",
        Func<string, string>? processor = null,
        string[]? excludedHeaders = null,
        int maxUrlLength = 100)
    {
        excludedHeaders ??= DefaultExcludedHeaders;

        var requestsResponseByTraceIdAndTest = requestResponses?.GroupBy(x => x.TestId);

        var plantUmlPerTestName = requestsResponseByTraceIdAndTest?.Select(testTraces =>
        {
            var traces = testTraces.ToList();
            var testName = testTraces.First().TestName;
            var results = CreatePlantUml(traces, processor, excludedHeaders, maxUrlLength);
            var imageTags = results.Select(x => x.GetPlantUmlImageTag(plantUmlServerRendererUrl)).ToArray();
            return new PlantUmlForTest(testTraces.Key, testName, results.Select(result => (result.PlantUml, result.PlantUmlEncoded)), testTraces.ToList(), imageTags);
        });

        return plantUmlPerTestName ?? [];
    }

    private static PlantUmlResult[] CreatePlantUml(
        List<RequestResponseLog> tracesForTest,
        Func<string, string>? processor,
        string[] excludedHeaders,
        int maxUrlLength)
    {
        const string eventNoteClass = "eventNote";
        List<PlantUmlResult> plantUmls = [];

        var plantUml = CreatePlantUmlPrefix();

        foreach (var trace in tracesForTest)
        {
            string GetNoteClass() =>
                trace.MetaType == RequestResponseMetaType.Event ? "<<" + eventNoteClass + ">>" : "";

            var serviceShortName = trace.ServiceName.Camelize();
            var callerShortName = trace.CallerName.Camelize();

            if (trace.Type == RequestResponseType.Request)
            {
                var requestPlantUmlNoteContent = GetPlantUmlForRequestOrResponseNote
                    (trace.Headers, trace.Content, excludedHeaders);

                if (processor != null)
                    requestPlantUmlNoteContent = processor.Invoke(requestPlantUmlNoteContent);

                var pathAndQuery = trace.Uri.PathAndQuery;
                if (pathAndQuery.Length > maxUrlLength)
                    pathAndQuery = string.Join("\\n        ", pathAndQuery.ChunksUpTo(maxUrlLength));

                plantUml +=
                    $"{callerShortName} -> {serviceShortName}: {trace.Method.Value}: {pathAndQuery}{Environment.NewLine}";

                if (!string.IsNullOrEmpty(requestPlantUmlNoteContent))
                {
                    plantUml +=
                        $"note{GetNoteClass()} left{Environment.NewLine}" +
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
                    $"{serviceShortName} --> {callerShortName}: {trace.StatusCode?.Value?.ToString().Titleize()}{Environment.NewLine}";

                if (!string.IsNullOrEmpty(responsePlantUmlNoteContent))
                {
                    plantUml +=
                        $"note{GetNoteClass()} right{Environment.NewLine}" +
                        $"{responsePlantUmlNoteContent}{Environment.NewLine}" +
                        $"end note{Environment.NewLine}";
                }
            }

            var currentEncodedPlantUml = PlantUmlTextEncoder.Encode(plantUml);
            if (currentEncodedPlantUml.Length > 2000 && trace != tracesForTest.Last())
            {
                CreatePlantUmlResponse();
            }
        }
        CreatePlantUmlResponse();

        return plantUmls.ToArray();

        string CreatePlantUmlPrefix()
        {
            var entitiesPlantUml = CreateEntitiesPlantUml(tracesForTest);
            return $@"
@startuml{Environment.NewLine}
{AddStyling()}
skinparam wrapWidth {MaxLineWidth}{Environment.NewLine}
!function $color($fgcolor){Environment.NewLine}
!return ""<color:""+$fgcolor+"" >""{Environment.NewLine}
!endfunction{Environment.NewLine}{Environment.NewLine}{entitiesPlantUml}{Environment.NewLine}".TrimStart();

            string AddStyling() => tracesForTest.Any(x => x.MetaType == RequestResponseMetaType.Event)
                ? $@"
<style>
 .{eventNoteClass} {{
     BackgroundColor #cfecf7
     FontSize 11
     RoundCorner 10
 }}
</style>".TrimStart()
                : "";
        }

        void CreatePlantUmlResponse()
        {
            plantUml += $"@enduml{Environment.NewLine}";
            var encodedPlantUml = PlantUmlTextEncoder.Encode(plantUml);
            plantUmls.Add(new(plantUml, encodedPlantUml));
            plantUml = CreatePlantUmlPrefix();
        }
    }

    private static string CreateEntitiesPlantUml(List<RequestResponseLog> tracesForTest)
    {
        var entitiesPlantUml = "";
        var actorDefined = false;
        var currentPlayers = new List<string>();

        foreach (var trace in tracesForTest)
        {
            var serviceShortName = trace.ServiceName.Camelize();
            var callerShortName = trace.CallerName.Camelize();

            if (!currentPlayers.Contains(callerShortName))
            {
                entitiesPlantUml +=
                    $"{(actorDefined ? "entity" : "actor")} \"{trace.CallerName}\" as {callerShortName}{Environment.NewLine}";
                currentPlayers.Add(callerShortName);
            }

            if (!currentPlayers.Contains(serviceShortName))
            {
                entitiesPlantUml += $"entity \"{trace.ServiceName}\" as {serviceShortName}{Environment.NewLine}";
                currentPlayers.Add(serviceShortName);
            }
        }

        return entitiesPlantUml;
    }

    private static string GetPlantUmlForRequestOrResponseNote(IEnumerable<(string Key, string? Value)> headers, string? content, string[] excludedHeaders)
    {
        var parsedContent = string.Empty;
        var isContentJson = false;
        if (content?.StartsWith("{") ?? false)
        {
            try
            {
                parsedContent = JsonNode.Parse(content)!.ToString();
                isContentJson = true;
            }
            catch (JsonException) { }
        }

        if (!isContentJson)
        {
            var formUrlEncodedDivider = "<font color=\"lightgray\">&</font>";
            parsedContent = content?
                .Replace("&", formUrlEncodedDivider + Environment.NewLine)
                .Split(Environment.NewLine)
                .SelectMany(x => x.ChunksUpTo(100))
                .StringJoin(Environment.NewLine);
        }

        return (($"{string.Join(Environment.NewLine, headers
            .Where(y => !excludedHeaders.Contains(y.Key))
            .OrderBy(y => y.Key)
            .SelectMany(y => BatchGray($"[{y.Key}={y.Value}]"))
        )}" + Environment.NewLine).TrimStart() +
                Environment.NewLine +
                $"{parsedContent}".Trim()).Trim();

        IEnumerable<string> BatchGray(string value)
        {
            return value.ChunksUpTo(100).Select(x => $"$color(gray){x}");
        }
    }

    private record PlantUmlResult(string PlantUml, string PlantUmlEncoded)
    {
        public string GetPlantUmlImageTag(string plantUmlServerRendererUrl) => $"<img src=\"{plantUmlServerRendererUrl.TrimEnd('/')}/{PlantUmlEncoded}\">";
    };

    public record PlantUmlForTest(Guid TestId, string TestName, IEnumerable<(string PlainText, string PlantUmlEncoded)> PlantUmls, IEnumerable<RequestResponseLog> Traces, string[] ImageTags);
}