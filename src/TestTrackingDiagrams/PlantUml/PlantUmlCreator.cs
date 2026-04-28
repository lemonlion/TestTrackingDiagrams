using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using TestTrackingDiagrams.Extensions;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.PlantUml;

public static partial class PlantUmlCreator
{
    private const int MaxLineWidth = 800;
    private const string EventNoteClass = "eventNote";
    public const int DefaultMaxEncodedDiagramLength = 2000;
    private const int MaxResponseNoteChunkLength = 15_000;
    private const int MaxEstimatedDiagramHeight = 12_000;
    private const int EstimatedArrowHeight = 45;
    private const int EstimatedNoteLineHeight = 18;

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
        bool internalFlowTracking = false,
        int maxEncodedDiagramLength = DefaultMaxEncodedDiagramLength,
        int truncateNotesAfterLines = 0,
        bool excludeAllHeaders = false,
        bool sequenceDiagramArrowColors = true,
        bool sequenceDiagramParticipantColors = false,
        Dictionary<string, string>? dependencyColors = null,
        Dictionary<string, string>? serviceTypeOverrides = null,
        GraphQlBodyFormat graphQlBodyFormat = GraphQlBodyFormat.FormattedWithMetadata)
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
                internalFlowTracking,
                maxEncodedDiagramLength,
                truncateNotesAfterLines,
                excludeAllHeaders,
                sequenceDiagramArrowColors,
                sequenceDiagramParticipantColors,
                dependencyColors,
                serviceTypeOverrides,
                graphQlBodyFormat);
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
        bool internalFlowTracking,
        int maxEncodedDiagramLength,
        int truncateNotesAfterLines = 0,
        bool excludeAllHeaders = false,
        bool sequenceDiagramArrowColors = true,
        bool sequenceDiagramParticipantColors = false,
        Dictionary<string, string>? dependencyColors = null,
        Dictionary<string, string>? serviceTypeOverrides = null,
        GraphQlBodyFormat graphQlBodyFormat = GraphQlBodyFormat.FormattedWithMetadata)
    {
        var builder = new DiagramBuilder(tracesForTest, plantUmlTheme, maxEncodedDiagramLength,
            sequenceDiagramArrowColors, sequenceDiagramParticipantColors, dependencyColors, serviceTypeOverrides);
        var lastTrace = tracesForTest[^1];

        var currentlyOverriding = false;
        var hasActionStart = separateSetup && tracesForTest.Any(t => t.IsActionStart);
        var actionStartIndex = tracesForTest.FindIndex(t => t.IsActionStart);
        var hasSetupTraces = hasActionStart && tracesForTest
            .Take(actionStartIndex)
            .Any(t => !t.IsOverrideStart && !t.IsOverrideEnd && !t.IsActionStart);
        var setupPartitionClosed = false;
        var partitionLine = highlightSetup ? "partition #E2E2F0 Setup" : "partition Setup";
        var isInActionPhase = actionStartIndex < 0; // no IsActionStart marker → everything is action

        foreach (var trace in tracesForTest)
        {
            if (trace.IsActionStart)
            {
                builder.ClosePartition();
                setupPartitionClosed = true;
                isInActionPhase = true;
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

            // Resolve phase variant: pick Setup or Action variant based on position relative to IsActionStart
            var activeVariant = isInActionPhase ? trace.ActionVariant : trace.SetupVariant;
            if (activeVariant is { Skip: true })
                continue;

            var effectiveMethod = activeVariant?.Method ?? trace.Method;
            var effectiveUri = activeVariant?.Uri ?? trace.Uri;
            var effectiveContent = activeVariant is not null ? activeVariant.Content : trace.Content;
            var effectiveHeaders = activeVariant?.Headers ?? trace.Headers;

            var serviceShortName = SanitizePlantUmlAlias(trace.ServiceName);
            var callerShortName = SanitizePlantUmlAlias(trace.CallerName);
            var content = effectiveContent ?? string.Empty;

            switch (trace.Type)
            {
                case RequestResponseType.Request:
                {
                    if (requestPreFormattingProcessor is not null)
                        content = requestPreFormattingProcessor(content);

                    var noteContent = FormatNoteContent(excludeAllHeaders ? [] : effectiveHeaders, content, excludedHeaders, RequestResponseType.Request, requestMidFormattingProcessor, trace.FocusFields, focusEmphasis, focusDeEmphasis, graphQlBodyFormat);

                    if (requestPostFormattingProcessor is not null)
                        noteContent = requestPostFormattingProcessor(noteContent);

                    var pathAndQuery = effectiveUri.PathAndQuery;
                    if (pathAndQuery.Length > maxUrlLength)
                        pathAndQuery = string.Join("\\n        ", pathAndQuery.ChunksUpTo(maxUrlLength));

                    var requestLabel = $"{effectiveMethod.Value}: {pathAndQuery}";

                    var graphQlLabel = GraphQlOperationDetector.TryExtractLabel(effectiveContent);
                    if (graphQlLabel is not null)
                        requestLabel = $"{requestLabel}\\n({graphQlLabel})";

                    if (internalFlowTracking)
                        requestLabel = $"[[#iflow-{trace.RequestResponseId} {requestLabel}]]";

                    var arrowColor = builder.GetArrowColor(trace.ServiceName, trace.DependencyCategory);
                    builder.AppendLine($"{callerShortName} -{arrowColor}> {serviceShortName}: {requestLabel}");
                    builder.AddArrowHeight();

                    if (!string.IsNullOrEmpty(noteContent))
                    {
                        var truncatedContent = EscapeForPlantUmlNote(TruncateNoteContent(noteContent, truncateNotesAfterLines));
                        builder.AppendLine($"note{GetNoteClass(trace.MetaType)} left");
                        builder.AppendLine(truncatedContent);
                        builder.AppendLine("end note");
                        builder.AddNoteHeight(truncatedContent);
                    }

                    break;
                }
                case RequestResponseType.Response:
                {
                    if (responsePreFormattingProcessor is not null)
                        content = responsePreFormattingProcessor(content);

                    var noteContent = FormatNoteContent(excludeAllHeaders ? [] : effectiveHeaders, content, excludedHeaders, RequestResponseType.Response, responseMidFormattingProcessor, trace.FocusFields, focusEmphasis, focusDeEmphasis);

                    if (responsePostFormattingProcessor is not null)
                        noteContent = responsePostFormattingProcessor(noteContent);

                    AppendResponseNoteContent(builder, noteContent, trace, serviceShortName, callerShortName, internalFlowTracking, truncateNotesAfterLines);
                    break;
                }
            }

            builder.IncrementStep();

            if ((builder.EncodedDiagramExceedsMaxLength || builder.EstimatedHeightExceedsMax) && trace != lastTrace)
                builder.FinishAndStartNewDiagram();
        }

        builder.FinishAndStartNewDiagram();
        return builder.GetResults();
    }

    private static string GetNoteClass(RequestResponseMetaType metaType) =>
        metaType == RequestResponseMetaType.Event ? $"<<{EventNoteClass}>>" : "";

    internal static string EscapeForPlantUmlNote(string text) =>
        text.Replace("\\", "\\\\");

    private static string TruncateNoteContent(string noteContent, int maxLines)
    {
        if (maxLines <= 0) return noteContent;
        var lines = noteContent.Split('\n');
        if (lines.Length <= maxLines) return noteContent;
        return string.Join("\n", lines.Take(maxLines)) + "\n...";
    }

    private static void AppendResponseNoteContent(
        DiagramBuilder builder,
        string noteContent,
        RequestResponseLog trace,
        string serviceShortName,
        string callerShortName,
        bool internalFlowTracking = false,
        int truncateNotesAfterLines = 0)
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

                AppendResponseNoteContent(builder, chunk, trace, serviceShortName, callerShortName, internalFlowTracking, truncateNotesAfterLines);

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

            var arrowColor = builder.GetArrowColor(trace!.ServiceName, trace.DependencyCategory);
            builder.AppendLine($"{serviceShortName} -{arrowColor}-> {callerShortName}: {responseLabel}");
            builder.AddArrowHeight();

            if (!string.IsNullOrEmpty(noteContent))
            {
                var truncatedContent = EscapeForPlantUmlNote(TruncateNoteContent(noteContent, truncateNotesAfterLines));
                builder.AppendLine($"note{GetNoteClass(trace!.MetaType)} right");
                builder.AppendLine(truncatedContent);
                builder.AppendLine("end note");
                builder.AddNoteHeight(truncatedContent);
            }
        }
    }

    private static string CreatePlantUmlPrefix(
        List<RequestResponseLog> tracesForTest,
        int stepNumber,
        string? plantUmlTheme = null,
        bool sequenceDiagramArrowColors = true,
        bool sequenceDiagramParticipantColors = false,
        Dictionary<string, string>? dependencyColors = null,
        Dictionary<string, string>? serviceTypeOverrides = null)
    {
        var entitiesPlantUml = CreateEntitiesPlantUml(tracesForTest, sequenceDiagramParticipantColors, dependencyColors, serviceTypeOverrides);
        var themeDirective = !string.IsNullOrWhiteSpace(plantUmlTheme) ? $"!theme {plantUmlTheme}\n" : "";
        return $"""

                @startuml
                {themeDirective}!pragma teoz true
                {AddEventStyling(tracesForTest)}
                skinparam wrapWidth {MaxLineWidth}
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

    private static string CreateEntitiesPlantUml(
        List<RequestResponseLog> tracesForTest,
        bool sequenceDiagramParticipantColors = false,
        Dictionary<string, string>? dependencyColors = null,
        Dictionary<string, string>? serviceTypeOverrides = null)
    {
        var sb = new StringBuilder();
        var actorDefined = false;
        var currentPlayers = new HashSet<string>();

        var relevantTraces = tracesForTest
            .Where(x => x is { IsOverrideStart: false, IsOverrideEnd: false, IsActionStart: false })
            .ToList();

        // Find the pure caller (appears as CallerName but never as ServiceName) and declare it first
        var allServiceNames = new HashSet<string>(relevantTraces.Select(t => t.ServiceName));
        var pureCaller = relevantTraces
            .Select(t => t.CallerName)
            .FirstOrDefault(c => !allServiceNames.Contains(c));

        if (pureCaller != null)
        {
            var pureCallerAlias = SanitizePlantUmlAlias(pureCaller);
            currentPlayers.Add(pureCallerAlias);
            sb.Append("actor \"")
                .Append(pureCaller)
                .Append("\" as ")
                .AppendLine(pureCallerAlias);
            actorDefined = true;
        }

        // Build a lookup: serviceName → category (user overrides then auto-detect)
        var serviceCategories = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var trace in relevantTraces)
        {
            if (serviceCategories.ContainsKey(trace.ServiceName)) continue;
            if (serviceTypeOverrides?.TryGetValue(trace.ServiceName, out var ov) == true)
            {
                serviceCategories[trace.ServiceName] = ov;
                continue;
            }
            serviceCategories[trace.ServiceName] = relevantTraces
                .Where(t => t.ServiceName == trace.ServiceName && t.DependencyCategory is not null)
                .Select(t => t.DependencyCategory)
                .FirstOrDefault();
        }

        foreach (var trace in relevantTraces)
        {
            var serviceShortName = SanitizePlantUmlAlias(trace.ServiceName);
            var callerShortName = SanitizePlantUmlAlias(trace.CallerName);

            if (currentPlayers.Add(callerShortName))
            {
                // Callers that aren't the first actor: use entity (they're HTTP services being tested)
                sb.Append(actorDefined ? "entity" : "actor")
                    .Append(" \"")
                    .Append(trace.CallerName)
                    .Append("\" as ")
                    .AppendLine(callerShortName);
            }

            if (currentPlayers.Add(serviceShortName))
            {
                var category = serviceCategories.TryGetValue(trace.ServiceName, out var cat) ? cat : null;
                var depType = DependencyPalette.Resolve(category);
                var shape = DependencyPalette.GetSequenceShape(depType);
                var colorSuffix = "";
                if (sequenceDiagramParticipantColors && category is not null)
                    colorSuffix = " " + DependencyPalette.GetColor(category, dependencyColors);

                sb.Append(shape)
                    .Append(" \"")
                    .Append(trace.ServiceName)
                    .Append("\" as ")
                    .Append(serviceShortName)
                    .AppendLine(colorSuffix);
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
        FocusDeEmphasis focusDeEmphasis = FocusDeEmphasis.LightGray,
        GraphQlBodyFormat graphQlBodyFormat = GraphQlBodyFormat.FormattedWithMetadata)
    {
        // For requests, try GraphQL formatting first (unless FocusFields are in use, which need JSON)
        string? parsedContent = null;
        var suppressHeaders = false;

        if (type is RequestResponseType.Request && graphQlBodyFormat != GraphQlBodyFormat.Json && focusFields is not { Length: > 0 })
        {
            parsedContent = GraphQlBodyFormatter.TryFormat(content, graphQlBodyFormat);
            if (parsedContent is not null && graphQlBodyFormat == GraphQlBodyFormat.FormattedQueryOnly)
                suppressHeaders = true;
        }

        parsedContent ??= TryFormatAsJson(content);

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

        var headersOnTop = suppressHeaders ? "" : string.Join(Environment.NewLine, headers
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
        return value.ChunksUpTo(100).Select(x => "<color:gray>" + x);
    }

    private sealed class DiagramBuilder(
        List<RequestResponseLog> tracesForTest,
        string? plantUmlTheme = null,
        int maxEncodedDiagramLength = DefaultMaxEncodedDiagramLength,
        bool sequenceDiagramArrowColors = true,
        bool sequenceDiagramParticipantColors = false,
        Dictionary<string, string>? dependencyColors = null,
        Dictionary<string, string>? serviceTypeOverrides = null)
    {
        private readonly List<PlantUmlResult> _results = [];
        private StringBuilder _currentDiagram = new(CreatePlantUmlPrefix(tracesForTest, 1, plantUmlTheme,
            sequenceDiagramArrowColors, sequenceDiagramParticipantColors, dependencyColors, serviceTypeOverrides));
        private int _stepNumber = 1;
        private string? _openPartitionLine;
        private string? _cachedEncoded;
        private int _lengthAtLastEncode;
        private int _estimatedHeight;

        // Build a lookup from ServiceName → resolved DependencyCategory
        private readonly Dictionary<string, string?> _serviceCategoryCache = BuildServiceCategoryCache(tracesForTest, serviceTypeOverrides);

        private static Dictionary<string, string?> BuildServiceCategoryCache(
            List<RequestResponseLog> traces,
            Dictionary<string, string>? overrides)
        {
            var cache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var trace in traces)
            {
                if (cache.ContainsKey(trace.ServiceName)) continue;

                // User override takes priority
                if (overrides?.TryGetValue(trace.ServiceName, out var overrideCategory) == true)
                {
                    cache[trace.ServiceName] = overrideCategory;
                    continue;
                }

                // Auto-detect from DependencyCategory on first request targeting this service
                var category = traces
                    .Where(t => t.ServiceName == trace.ServiceName && t.DependencyCategory is not null)
                    .Select(t => t.DependencyCategory)
                    .FirstOrDefault();
                cache[trace.ServiceName] = category;
            }
            return cache;
        }

        /// <summary>Returns the arrow color syntax (e.g. <c>[#E74C3C]</c>) for a given service, or empty if coloring is off.</summary>
        public string GetArrowColor(string serviceName, string? dependencyCategory)
        {
            if (!sequenceDiagramArrowColors) return "";

            // Use cached category for the service (accounts for overrides and auto-detection)
            var category = _serviceCategoryCache.TryGetValue(serviceName, out var cached) ? cached : dependencyCategory;
            var color = DependencyPalette.GetColor(category, dependencyColors);
            return $"[{color}]";
        }

        public void Append(string text) => _currentDiagram.Append(text);
        public void AppendLine(string text) => _currentDiagram.AppendLine(text);
        public void IncrementStep() => _stepNumber++;
        public bool HasOpenPartition => _openPartitionLine != null;

        public void AddArrowHeight() => _estimatedHeight += EstimatedArrowHeight;

        public void AddNoteHeight(string noteContent)
        {
            if (string.IsNullOrEmpty(noteContent)) return;
            var lineCount = noteContent.Split('\n').Length;
            _estimatedHeight += (lineCount * EstimatedNoteLineHeight) + EstimatedArrowHeight;
        }

        public bool EstimatedHeightExceedsMax => _estimatedHeight > MaxEstimatedDiagramHeight;

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
                if (_currentDiagram.Length <= maxEncodedDiagramLength)
                    return false;

                // Only re-encode when the diagram has grown meaningfully since the last check
                if (_cachedEncoded is not null && _currentDiagram.Length - _lengthAtLastEncode < 200)
                    return _cachedEncoded.Length > maxEncodedDiagramLength;

                _cachedEncoded = PlantUmlTextEncoder.Encode(_currentDiagram.ToString());
                _lengthAtLastEncode = _currentDiagram.Length;
                return _cachedEncoded.Length > maxEncodedDiagramLength;
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
            _estimatedHeight = 0;
            _results.Add(new PlantUmlResult(plainText, encodedPlantUml));
            _currentDiagram = new StringBuilder(CreatePlantUmlPrefix(tracesForTest, _stepNumber, plantUmlTheme,
                sequenceDiagramArrowColors, sequenceDiagramParticipantColors, dependencyColors, serviceTypeOverrides));

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