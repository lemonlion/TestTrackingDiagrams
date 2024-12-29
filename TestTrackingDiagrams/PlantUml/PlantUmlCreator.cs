using System.Net;
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
        Func<string, string>? requestPreFormattingProcessor = null,
        Func<string, string>? requestPostFormattingProcessor = null,
        Func<string, string>? responsePreFormattingProcessor = null,
        Func<string, string>? responsePostFormattingProcessor = null,
        string[]? excludedHeaders = null,
        int maxUrlLength = 100)
    {
        excludedHeaders ??= DefaultExcludedHeaders;

        var requestsResponseByTraceIdAndTest = requestResponses?.GroupBy(x => x.TestId);

        var plantUmlPerTestName = requestsResponseByTraceIdAndTest?.Select(testTraces =>
        {
            var traces = testTraces.ToList();
            var testName = testTraces.First().TestName;
            var results = CreatePlantUml(
                traces, 
                requestPreFormattingProcessor,
                requestPostFormattingProcessor,
                responsePreFormattingProcessor,
                responsePostFormattingProcessor,
                excludedHeaders, 
                maxUrlLength);
            var imageTags = results.Select(x => x.GetPlantUmlImageTag(plantUmlServerRendererUrl)).ToArray();
            return new PlantUmlForTest(testTraces.Key, testName, results.Select(result => (result.PlantUml, result.PlantUmlEncoded)), testTraces.ToList(), imageTags);
        });

        return plantUmlPerTestName ?? [];
    }

    private static PlantUmlResult[] CreatePlantUml(
        List<RequestResponseLog> tracesForTest,
        Func<string, string>? requestPreFormattingProcessor,
        Func<string, string>? requestPostFormattingProcessor,
        Func<string, string>? responsePreFormattingProcessor,
        Func<string, string>? responsePostFormattingProcessor,
        string[] excludedHeaders,
        int maxUrlLength)
    {
        const string eventNoteClass = "eventNote";
        List<PlantUmlResult> plantUmls = [];

        var stepNumber = 1;
        var plantUml = CreatePlantUmlPrefix();

        foreach (var trace in tracesForTest)
        {
            string GetNoteClass() =>
                trace.MetaType == RequestResponseMetaType.Event ? "<<" + eventNoteClass + ">>" : "";

            var serviceShortName = trace.ServiceName.Camelize();
            var callerShortName = trace.CallerName.Camelize();

            var content = trace.Content ?? string.Empty;
            if (trace.Type == RequestResponseType.Request)
            {
                if (requestPreFormattingProcessor is not null)
                    content = requestPreFormattingProcessor(content);

                var requestPlantUmlNoteContent = FormatContentForRequestOrResponseNote
                    (trace.Headers, content, excludedHeaders, RequestResponseType.Request);

                if (requestPostFormattingProcessor is not null)
                    requestPlantUmlNoteContent = requestPostFormattingProcessor(requestPlantUmlNoteContent);

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
                if (responsePreFormattingProcessor is not null)
                    content = responsePreFormattingProcessor(content);

                var responsePlantUmlNoteContent = FormatContentForRequestOrResponseNote
                    (trace.Headers, content, excludedHeaders, RequestResponseType.Response);

                if (responsePostFormattingProcessor is not null)
                    responsePlantUmlNoteContent = responsePostFormattingProcessor(responsePlantUmlNoteContent);

                CreateResponseNote(responsePlantUmlNoteContent);

                void CreateResponseNote(string noteContent)
                {
                    var prefix = "..Continued From Previous Diagram.." + Environment.NewLine;
                    var suffix = Environment.NewLine + "..Continued On Next Diagram..";
                    var maxChunkLength = 15_000;
                    var maxResponseLength = maxChunkLength + suffix.Length + prefix.Length;
                    if (noteContent.Length > maxResponseLength)
                    {
                        var chunks = noteContent.ChunksUpTo(maxChunkLength).ToArray();
                        for (var i = 0; i < chunks.Length; i++)
                        {
                            var noteContentChunk = chunks[i];
                            var isFirstChunk = i == 0;
                            var isLastChunk = i == chunks.Length - 1;

                            if (!isFirstChunk)
                                noteContentChunk = prefix + noteContentChunk;

                            if (!isLastChunk)
                                noteContentChunk += suffix;

                            CreateResponseNote(noteContentChunk);

                            if (!isLastChunk)
                                FinishPlantUmlDiagramAndStartNewOne();
                        }
                    }
                    else
                    {
                        var status = trace.StatusCode?.Value?.ToString().Titleize();
                        if (trace?.StatusCode?.Value as HttpStatusCode? == (HttpStatusCode)302)
                            status += " (Redirect)"; // The name of 302 'Found' is a bit ambiguous, so we make it clearer for the reader

                        plantUml +=
                            $"{serviceShortName} --> {callerShortName}: {status}{Environment.NewLine}";

                        if (!string.IsNullOrEmpty(noteContent))
                        {
                            plantUml +=
                                $"note{GetNoteClass()} right{Environment.NewLine}" +
                                $"{noteContent}{Environment.NewLine}" +
                                $"end note{Environment.NewLine}";
                        }
                    }
                }
            }

            var currentEncodedPlantUml = PlantUmlTextEncoder.Encode(plantUml);

            stepNumber++;

            if (currentEncodedPlantUml.Length > 2000 && trace != tracesForTest.Last())
                FinishPlantUmlDiagramAndStartNewOne();
        }
        FinishPlantUmlDiagramAndStartNewOne();

        return plantUmls.ToArray();

        string CreatePlantUmlPrefix()
        {
            var entitiesPlantUml = CreateEntitiesPlantUml(tracesForTest);
            return $"""

                    @startuml
                    {AddStyling()}
                    skinparam wrapWidth {MaxLineWidth}
                    !function $color($value)
                    !return "<color:"+$value+" >"
                    !endfunction
                    autonumber {stepNumber}

                    {entitiesPlantUml}

                    """.TrimStart();

            string AddStyling() => tracesForTest.Any(x => x.MetaType == RequestResponseMetaType.Event)
                ? $$"""

                    <style>
                     .{{eventNoteClass}} {
                         BackgroundColor #cfecf7
                         FontSize 11
                         RoundCorner 10
                     }
                    </style>
                    """.TrimStart()
                : "";
        }

        void FinishPlantUmlDiagramAndStartNewOne()
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

    private static string FormatContentForRequestOrResponseNote(IEnumerable<(string Key, string? Value)> headers, string? content, string[] excludedHeaders, RequestResponseType type)
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
            if (type is RequestResponseType.Response)
                parsedContent = content ?? string.Empty;
            else
            {
                var formUrlEncodedDivider = "<font color=\"lightgray\">&";
                parsedContent = content?
                    .Split("&")
                    .SelectMany(x =>
                    {
                        var chunks = x.ChunksUpTo(100).ToArray();
                        if (chunks.Length == 0)
                            return chunks;
                        chunks[^1] += formUrlEncodedDivider;
                        return chunks;
                    })
                    .StringJoin(Environment.NewLine)
                    .TrimEnd(formUrlEncodedDivider) ?? string.Empty;
            }
        }

        var headersOnTop = $"{string.Join(Environment.NewLine, headers
            .Where(y => !excludedHeaders.Contains(y.Key))
            .OrderBy(y => y.Key)
            .SelectMany(y => BatchGray($"[{y.Key}={y.Value}]"))
        )}";

        return ((headersOnTop + Environment.NewLine + Environment.NewLine).TrimStart() + parsedContent.Trim()).TrimEnd();

        IEnumerable<string> BatchGray(string value)
        {
            return value.ChunksUpTo(100).Select(x => $"$color(gray){x}");
        }
    }

    private record PlantUmlResult(string PlantUml, string PlantUmlEncoded)
    {
        public string GetPlantUmlImageTag(string plantUmlServerRendererUrl) => $"<img src=\"{plantUmlServerRendererUrl.TrimEnd('/')}/{PlantUmlEncoded}\">";
    };

    public record PlantUmlForTest(string TestId, string TestName, IEnumerable<(string PlainText, string PlantUmlEncoded)> PlantUmls, IEnumerable<RequestResponseLog> Traces, string[] ImageTags);
}