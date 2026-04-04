using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Humanizer;
using System.Text.Json;
using TestTrackingDiagrams.Extensions;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.PlantUml;

public static partial class PlantUmlCreator
{
    private const int MaxLineWidth = 800;
    private const string EventNoteClass = "eventNote";
    private const int MaxEncodedDiagramLength = 2000;
    private const int MaxResponseNoteChunkLength = 15_000;

    public static string[] DefaultExcludedHeaders => ["Cache-Control", "Pragma"];

    private static readonly ConcurrentDictionary<string, string> AliasCache = new();

    public static IEnumerable<PlantUmlForTest> GetPlantUmlImageTagsPerTestId(
        IEnumerable<RequestResponseLog>? requestResponses,
        string plantUmlServerRendererUrl = "https://www.plantuml.com/plantuml/png",
        Func<string, string>? requestPreFormattingProcessor = null,
        Func<string, string>? requestPostFormattingProcessor = null,
        Func<string, string>? responsePreFormattingProcessor = null,
        Func<string, string>? responsePostFormattingProcessor = null,
        Func<string, string>? requestMidFormattingProcessor = null,
        Func<string, string>? responseMidFormattingProcessor = null,
        string[]? excludedHeaders = null,
        int maxUrlLength = 100,
        bool separateSetup = false,
        bool highlightSetup = true,
        bool lazyLoadImages = true,
        FocusEmphasis focusEmphasis = FocusEmphasis.Bold,
        FocusDeEmphasis focusDeEmphasis = FocusDeEmphasis.LightGray,
        string? plantUmlTheme = null,
        bool internalFlowTracking = false)
    {
        excludedHeaders ??= DefaultExcludedHeaders;

        var requestsResponseByTraceIdAndTest = requestResponses?.GroupBy(x => x.TestId);

        var plantUmlPerTestName = requestsResponseByTraceIdAndTest?
            .AsParallel()
            .AsOrdered()
            .Select(testTraces =>
        {
            var traces = testTraces.ToList();
            var testName = testTraces.First().TestName;
            var results = CreatePlantUml(
                traces, 
                requestPreFormattingProcessor,
                requestPostFormattingProcessor,
                responsePreFormattingProcessor,
                responsePostFormattingProcessor,
                requestMidFormattingProcessor,
                responseMidFormattingProcessor,
                excludedHeaders, 
                maxUrlLength,
                separateSetup,
                highlightSetup,
                focusEmphasis,
                focusDeEmphasis,
                plantUmlTheme,
                internalFlowTracking);
            var imageTags = results.Select(x => x.GetPlantUmlImageTag(plantUmlServerRendererUrl, lazyLoadImages)).ToArray();
            return new PlantUmlForTest(testTraces.Key, testName, results.Select(result => (result.PlantUml, result.PlantUmlEncoded)), testTraces.ToList(), imageTags);
        });

        return plantUmlPerTestName?.AsEnumerable() ?? [];
    }

    private static PlantUmlResult[] CreatePlantUml(
        List<RequestResponseLog> tracesForTest,
        Func<string, string>? requestPreFormattingProcessor,
        Func<string, string>? requestPostFormattingProcessor,
        Func<string, string>? responsePreFormattingProcessor,
        Func<string, string>? responsePostFormattingProcessor,
        Func<string, string>? requestMidFormattingProcessor,
        Func<string, string>? responseMidFormattingProcessor,
        string[] excludedHeaders,
        int maxUrlLength,
        bool separateSetup,
        bool highlightSetup,
        FocusEmphasis focusEmphasis,
        FocusDeEmphasis focusDeEmphasis,
        string? plantUmlTheme,
        bool internalFlowTracking)
    {
        var builder = new DiagramBuilder(tracesForTest, plantUmlTheme);
        var lastTrace = tracesForTest[^1];

        var currentlyOverriding = false;
        var hasActionStart = separateSetup && tracesForTest.Any(t => t.IsActionStart);
        var actionStartIndex = tracesForTest.FindIndex(t => t.IsActionStart);
        var hasSetupTraces = hasActionStart && tracesForTest
            .Take(actionStartIndex)
            .Any(t => !t.IsOverrideStart && !t.IsOverrideEnd && !t.IsActionStart);
        var setupPartitionClosed = false;
        var partitionLine = highlightSetup ? "partition #E2E2F0 Setup" : "partition Setup";

        foreach (var trace in tracesForTest)
        {
            if (trace.IsActionStart)
            {
                builder.ClosePartition();
                setupPartitionClosed = true;
                continue;
            }

            if (trace.IsOverrideStart && currentlyOverriding)
            {
                Debug.Write("Ignoring an override as you're already overriding");
                continue;
            }

            if (trace.IsOverrideEnd)
            {
                currentlyOverriding = false;
                builder.Append(trace.PlantUml ?? "");
                continue;
            }

            if (trace.IsOverrideStart)
            {
                if (hasActionStart && !setupPartitionClosed)
                {
                    builder.ClosePartition();
                    setupPartitionClosed = true;
                }
                currentlyOverriding = true;
                builder.Append(trace.PlantUml ?? "");
                continue;
            }

            if (currentlyOverriding)
                continue;

            if (hasSetupTraces && !builder.HasOpenPartition && !setupPartitionClosed)
                builder.OpenPartition(partitionLine);

            var serviceShortName = SanitizePlantUmlAlias(trace.ServiceName);
            var callerShortName = SanitizePlantUmlAlias(trace.CallerName);
            var content = trace.Content ?? string.Empty;

            switch (trace.Type)
            {
                case RequestResponseType.Request:
                {
                    if (requestPreFormattingProcessor is not null)
                        content = requestPreFormattingProcessor(content);

                    var noteContent = FormatNoteContent(trace.Headers, content, excludedHeaders, RequestResponseType.Request, requestMidFormattingProcessor, trace.FocusFields, focusEmphasis, focusDeEmphasis);

                    if (requestPostFormattingProcessor is not null)
                        noteContent = requestPostFormattingProcessor(noteContent);

                    var pathAndQuery = trace.Uri.PathAndQuery;
                    if (pathAndQuery.Length > maxUrlLength)
                        pathAndQuery = string.Join("\\n        ", pathAndQuery.ChunksUpTo(maxUrlLength));

                    var requestLabel = $"{trace.Method.Value}: {pathAndQuery}";
                    if (internalFlowTracking)
                        requestLabel = $"[[#iflow-{trace.RequestResponseId} {requestLabel}]]";

                    builder.AppendLine($"{callerShortName} -> {serviceShortName}: {requestLabel}");

                    if (!string.IsNullOrEmpty(noteContent))
                    {
                        builder.AppendLine($"note{GetNoteClass(trace.MetaType)} left");
                        builder.AppendLine(noteContent);
                        builder.AppendLine("end note");
                    }

                    break;
                }
                case RequestResponseType.Response:
                {
                    if (responsePreFormattingProcessor is not null)
                        content = responsePreFormattingProcessor(content);

                    var noteContent = FormatNoteContent(trace.Headers, content, excludedHeaders, RequestResponseType.Response, responseMidFormattingProcessor, trace.FocusFields, focusEmphasis, focusDeEmphasis);

                    if (responsePostFormattingProcessor is not null)
                        noteContent = responsePostFormattingProcessor(noteContent);

                    AppendResponseNoteContent(builder, noteContent, trace, serviceShortName, callerShortName, internalFlowTracking);
                    break;
                }
            }

            builder.IncrementStep();

            if (builder.EncodedDiagramExceedsMaxLength && trace != lastTrace)
                builder.FinishAndStartNewDiagram();
        }

        builder.FinishAndStartNewDiagram();
        return builder.GetResults();
    }

    private static string GetNoteClass(RequestResponseMetaType metaType) =>
        metaType == RequestResponseMetaType.Event ? $"<<{EventNoteClass}>>" : "";

    private static void AppendResponseNoteContent(
        DiagramBuilder builder,
        string noteContent,
        RequestResponseLog trace,
        string serviceShortName,
        string callerShortName,
        bool internalFlowTracking = false)
    {
        var prefix = "..Continued From Previous Diagram.." + Environment.NewLine;
        var suffix = Environment.NewLine + "..Continued On Next Diagram..";
        var maxResponseLength = MaxResponseNoteChunkLength + suffix.Length + prefix.Length;

        if (noteContent.Length > maxResponseLength)
        {
            var chunks = noteContent.ChunksUpTo(MaxResponseNoteChunkLength).ToArray();
            for (var i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var isFirst = i == 0;
                var isLast = i == chunks.Length - 1;

                if (!isFirst) chunk = prefix + chunk;
                if (!isLast) chunk += suffix;

                AppendResponseNoteContent(builder, chunk, trace, serviceShortName, callerShortName, internalFlowTracking);

                if (!isLast)
                    builder.FinishAndStartNewDiagram();
            }
        }
        else
        {
            var status = trace.StatusCode?.Value?.ToString()?.Titleize();
            if (trace?.StatusCode?.Value as HttpStatusCode? == (HttpStatusCode)302)
                status += " (Redirect)"; // The name of 302 'Found' is a bit ambiguous, so we make it clearer for the reader

            var responseLabel = status ?? "";
            if (internalFlowTracking)
                responseLabel = $"[[#iflow-{trace!.RequestResponseId}-res {responseLabel}]]";

            builder.AppendLine($"{serviceShortName} --> {callerShortName}: {responseLabel}");

            if (!string.IsNullOrEmpty(noteContent))
            {
                builder.AppendLine($"note{GetNoteClass(trace!.MetaType)} right");
                builder.AppendLine(noteContent);
                builder.AppendLine("end note");
            }
        }
    }

    private static string CreatePlantUmlPrefix(List<RequestResponseLog> tracesForTest, int stepNumber, string? plantUmlTheme = null)
    {
        var entitiesPlantUml = CreateEntitiesPlantUml(tracesForTest);
        var themeDirective = !string.IsNullOrWhiteSpace(plantUmlTheme) ? $"!theme {plantUmlTheme}\n" : "";
        return $"""

                @startuml
                {themeDirective}!pragma teoz true
                {AddEventStyling(tracesForTest)}
                skinparam wrapWidth {MaxLineWidth}
                !function $color($value)
                !return "<color:"+$value+" >"
                !endfunction
                autonumber {stepNumber}

                {entitiesPlantUml}

                """.TrimStart();
    }

    private static string AddEventStyling(List<RequestResponseLog> tracesForTest) =>
        tracesForTest.Any(x => x.MetaType == RequestResponseMetaType.Event)
            ? $$"""

                <style>
                 .{{EventNoteClass}} {
                     BackgroundColor #cfecf7
                     FontSize 11
                     RoundCorner 10
                 }
                </style>
                """.TrimStart()
            : "";

    private static string CreateEntitiesPlantUml(List<RequestResponseLog> tracesForTest)
    {
        var sb = new StringBuilder();
        var actorDefined = false;
        var currentPlayers = new HashSet<string>();

        foreach (var trace in tracesForTest.Where(x => x is { IsOverrideStart: false, IsOverrideEnd: false, IsActionStart: false }))
        {
            var serviceShortName = SanitizePlantUmlAlias(trace.ServiceName);
            var callerShortName = SanitizePlantUmlAlias(trace.CallerName);

            if (currentPlayers.Add(callerShortName))
            {
                sb.Append(actorDefined ? "entity" : "actor")
                    .Append(" \"")
                    .Append(trace.CallerName)
                    .Append("\" as ")
                    .AppendLine(callerShortName);
            }

            if (currentPlayers.Add(serviceShortName))
            {
                sb.Append("entity \"")
                    .Append(trace.ServiceName)
                    .Append("\" as ")
                    .AppendLine(serviceShortName);
            }
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex SanitizeAliasRegex();

    private static string SanitizePlantUmlAlias(string name)
    {
        return AliasCache.GetOrAdd(name, n => SanitizeAliasRegex().Replace(n.Camelize(), "_"));
    }

    private static string FormatNoteContent(
        IEnumerable<(string Key, string? Value)> headers,
        string? content,
        string[] excludedHeaders,
        RequestResponseType type,
        Func<string, string>? midFormattingProcessor = null,
        string[]? focusFields = null,
        FocusEmphasis focusEmphasis = FocusEmphasis.Bold,
        FocusDeEmphasis focusDeEmphasis = FocusDeEmphasis.LightGray)
    {
        var parsedContent = TryFormatAsJson(content);

        if (parsedContent is null)
        {
            parsedContent = type is RequestResponseType.Response
                ? content ?? string.Empty
                : FormatFormUrlEncodedContent(content);
        }

        var formattedContent = parsedContent!;

        if (midFormattingProcessor is not null)
            formattedContent = midFormattingProcessor(formattedContent);

        if (focusFields is { Length: > 0 })
        {
            formattedContent = JsonFocusFormatter.FormatWithFocus(formattedContent, focusFields, focusEmphasis, focusDeEmphasis);
        }

        var headersOnTop = string.Join(Environment.NewLine, headers
            .Where(y => !excludedHeaders.Contains(y.Key))
            .OrderBy(y => y.Key)
            .SelectMany(y => BatchGray($"[{y.Key}={y.Value}]")));

        return ((headersOnTop + Environment.NewLine + Environment.NewLine).TrimStart() + formattedContent.Trim()).TrimEnd();
    }

    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    private static string? TryFormatAsJson(string? content)
    {
        if (content is null || (!content.StartsWith('{') && !content.StartsWith('[')))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(doc.RootElement, IndentedJsonOptions);
        }
        catch (JsonException) { return null; }
    }

    private static string FormatFormUrlEncodedContent(string? content)
    {
        const string divider = "<font color=\"lightgray\">&";
        return content?
            .Split("&")
            .SelectMany(x =>
            {
                var chunks = x.ChunksUpTo(100).ToArray();
                if (chunks.Length == 0)
                    return chunks;
                chunks[^1] += divider;
                return chunks;
            })
            .StringJoin(Environment.NewLine)
            .TrimEnd(divider) ?? string.Empty;
    }

    private static IEnumerable<string> BatchGray(string value)
    {
        return value.ChunksUpTo(100).Select(x => $"$color(gray){x}");
    }

    private sealed class DiagramBuilder(List<RequestResponseLog> tracesForTest, string? plantUmlTheme = null)
    {
        private readonly List<PlantUmlResult> _results = [];
        private StringBuilder _currentDiagram = new(CreatePlantUmlPrefix(tracesForTest, 1, plantUmlTheme));
        private int _stepNumber = 1;
        private string? _openPartitionLine;
        private string? _cachedEncoded;
        private int _lengthAtLastEncode;

        public void Append(string text) => _currentDiagram.Append(text);
        public void AppendLine(string text) => _currentDiagram.AppendLine(text);
        public void IncrementStep() => _stepNumber++;
        public bool HasOpenPartition => _openPartitionLine != null;

        public void OpenPartition(string partitionLine)
        {
            AppendLine(partitionLine);
            _openPartitionLine = partitionLine;
        }

        public void ClosePartition()
        {
            if (_openPartitionLine != null)
            {
                AppendLine("end");
                _openPartitionLine = null;
            }
        }

        public bool EncodedDiagramExceedsMaxLength
        {
            get
            {
                if (_currentDiagram.Length <= MaxEncodedDiagramLength)
                    return false;

                // Only re-encode when the diagram has grown meaningfully since the last check
                if (_cachedEncoded is not null && _currentDiagram.Length - _lengthAtLastEncode < 200)
                    return _cachedEncoded.Length > MaxEncodedDiagramLength;

                _cachedEncoded = PlantUmlTextEncoder.Encode(_currentDiagram.ToString());
                _lengthAtLastEncode = _currentDiagram.Length;
                return _cachedEncoded.Length > MaxEncodedDiagramLength;
            }
        }

        public void FinishAndStartNewDiagram()
        {
            var partitionToReopen = _openPartitionLine;
            if (_openPartitionLine != null)
                AppendLine("end");

            AppendLine("@enduml");
            var plainText = _currentDiagram.ToString();
            var encodedPlantUml = PlantUmlTextEncoder.Encode(plainText);
            _cachedEncoded = null;
            _lengthAtLastEncode = 0;
            _results.Add(new PlantUmlResult(plainText, encodedPlantUml));
            _currentDiagram = new StringBuilder(CreatePlantUmlPrefix(tracesForTest, _stepNumber, plantUmlTheme));

            if (partitionToReopen != null)
            {
                AppendLine(partitionToReopen);
                _openPartitionLine = partitionToReopen;
            }
        }

        public PlantUmlResult[] GetResults() => [.. _results];
    }

    private record PlantUmlResult(string PlantUml, string PlantUmlEncoded)
    {
        public string GetPlantUmlImageTag(string plantUmlServerRendererUrl, bool lazyLoad = true) =>
            $"<img{(lazyLoad ? " loading=\"lazy\"" : "")} src=\"{plantUmlServerRendererUrl.TrimEnd('/')}/{PlantUmlEncoded}\">";
    }

    public record PlantUmlForTest(
        string TestId,
        string TestName,
        IEnumerable<(string PlainText, string PlantUmlEncoded)> PlantUmls,
        IEnumerable<RequestResponseLog> Traces,
        string[] ImageTags);
}