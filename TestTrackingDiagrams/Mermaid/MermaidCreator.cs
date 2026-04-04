using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Humanizer;
using TestTrackingDiagrams.Extensions;
using TestTrackingDiagrams.PlantUml;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Mermaid;

public static partial class MermaidCreator
{
    private const int MaxPlainTextDiagramLength = 10_000;
    private const int MaxResponseNoteChunkLength = 5_000;

    public static string[] DefaultExcludedHeaders => ["Cache-Control", "Pragma"];

    private static readonly ConcurrentDictionary<string, string> AliasCache = new();

    public static IEnumerable<MermaidForTest> GetMermaidDiagramsPerTestId(
        IEnumerable<RequestResponseLog>? requestResponses,
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
        FocusEmphasis focusEmphasis = FocusEmphasis.Bold,
        FocusDeEmphasis focusDeEmphasis = FocusDeEmphasis.LightGray)
    {
        excludedHeaders ??= DefaultExcludedHeaders;

        var requestsResponseByTraceIdAndTest = requestResponses?.GroupBy(x => x.TestId);

        var mermaidPerTestName = requestsResponseByTraceIdAndTest?
            .AsParallel()
            .AsOrdered()
            .Select(testTraces =>
            {
                var traces = testTraces.ToList();
                var testName = testTraces.First().TestName;
                var results = CreateMermaid(
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
                    focusDeEmphasis);
                return new MermaidForTest(testTraces.Key, testName, results.Select(r => r.MermaidText).ToArray(), testTraces.ToList());
            });

        return mermaidPerTestName?.AsEnumerable() ?? [];
    }

    private static MermaidResult[] CreateMermaid(
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
        FocusDeEmphasis focusDeEmphasis)
    {
        var builder = new DiagramBuilder(tracesForTest);
        var lastTrace = tracesForTest[^1];

        var currentlyOverriding = false;
        var hasActionStart = separateSetup && tracesForTest.Any(t => t.IsActionStart);
        var actionStartIndex = tracesForTest.FindIndex(t => t.IsActionStart);
        var hasSetupTraces = hasActionStart && tracesForTest
            .Take(actionStartIndex)
            .Any(t => !t.IsOverrideStart && !t.IsOverrideEnd && !t.IsActionStart);
        var setupRectClosed = false;
        var setupRectLine = highlightSetup ? "rect rgb(226, 226, 240)" : "rect rgb(245, 245, 245)";

        foreach (var trace in tracesForTest)
        {
            if (trace.IsActionStart)
            {
                builder.CloseRect();
                setupRectClosed = true;
                continue;
            }

            if (trace.IsOverrideStart && currentlyOverriding)
                continue;

            if (trace.IsOverrideEnd)
            {
                currentlyOverriding = false;
                builder.Append(trace.PlantUml ?? "");
                continue;
            }

            if (trace.IsOverrideStart)
            {
                if (hasActionStart && !setupRectClosed)
                {
                    builder.CloseRect();
                    setupRectClosed = true;
                }
                currentlyOverriding = true;
                builder.Append(trace.PlantUml ?? "");
                continue;
            }

            if (currentlyOverriding)
                continue;

            if (hasSetupTraces && !builder.HasOpenRect && !setupRectClosed)
                builder.OpenRect(setupRectLine);

            var serviceAlias = SanitizeAlias(trace.ServiceName);
            var callerAlias = SanitizeAlias(trace.CallerName);
            var content = trace.Content ?? string.Empty;
            var isEvent = trace.MetaType == RequestResponseMetaType.Event;

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
                        pathAndQuery = string.Join("<br/>", pathAndQuery.ChunksUpTo(maxUrlLength));

                    if (isEvent)
                        builder.OpenEventRect();

                    builder.AppendLine($"{callerAlias}->>{serviceAlias}: {EscapeMermaid(trace.Method.Value?.ToString() ?? "")}: {EscapeMermaid(pathAndQuery)}");

                    if (!string.IsNullOrEmpty(noteContent))
                        builder.AppendLine($"Note left of {callerAlias}: {ToMermaidNote(noteContent)}");

                    break;
                }
                case RequestResponseType.Response:
                {
                    if (responsePreFormattingProcessor is not null)
                        content = responsePreFormattingProcessor(content);

                    var noteContent = FormatNoteContent(trace.Headers, content, excludedHeaders, RequestResponseType.Response, responseMidFormattingProcessor, trace.FocusFields, focusEmphasis, focusDeEmphasis);

                    if (responsePostFormattingProcessor is not null)
                        noteContent = responsePostFormattingProcessor(noteContent);

                    AppendResponseNoteContent(builder, noteContent, trace, serviceAlias, callerAlias);

                    if (isEvent)
                        builder.CloseEventRect();

                    break;
                }
            }

            if (builder.PlainTextExceedsMaxLength && trace != lastTrace)
                builder.FinishAndStartNewDiagram();
        }

        builder.FinishAndStartNewDiagram();
        return builder.GetResults();
    }

    private static void AppendResponseNoteContent(
        DiagramBuilder builder,
        string noteContent,
        RequestResponseLog trace,
        string serviceAlias,
        string callerAlias)
    {
        var prefix = "...Continued From Previous Diagram...<br/>";
        var suffix = "<br/>...Continued On Next Diagram...";
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

                AppendResponseNoteContent(builder, chunk, trace, serviceAlias, callerAlias);

                if (!isLast)
                    builder.FinishAndStartNewDiagram();
            }
        }
        else
        {
            var status = trace.StatusCode?.Value?.ToString()?.Titleize();
            if (trace?.StatusCode?.Value as HttpStatusCode? == (HttpStatusCode)302)
                status += " (Redirect)";

            builder.AppendLine($"{serviceAlias}-->>{callerAlias}: {EscapeMermaid(status ?? "")}");

            if (!string.IsNullOrEmpty(noteContent))
                builder.AppendLine($"Note right of {serviceAlias}: {ToMermaidNote(noteContent)}");
        }
    }

    private static string CreateMermaidPrefix(List<RequestResponseLog> tracesForTest)
    {
        var entities = CreateEntitiesMermaid(tracesForTest);
        return $"sequenceDiagram{Environment.NewLine}autonumber{Environment.NewLine}{entities}{Environment.NewLine}";
    }

    private static string CreateEntitiesMermaid(List<RequestResponseLog> tracesForTest)
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
            var pureCallerAlias = SanitizeAlias(pureCaller);
            currentPlayers.Add(pureCallerAlias);
            sb.Append("actor ")
                .Append(pureCallerAlias)
                .Append(" as ")
                .AppendLine(pureCaller);
            actorDefined = true;
        }

        foreach (var trace in relevantTraces)
        {
            var serviceAlias = SanitizeAlias(trace.ServiceName);
            var callerAlias = SanitizeAlias(trace.CallerName);

            if (currentPlayers.Add(callerAlias))
            {
                sb.Append(actorDefined ? "participant" : "actor")
                    .Append(' ')
                    .Append(callerAlias)
                    .Append(" as ")
                    .AppendLine(trace.CallerName);
                actorDefined = true;
            }

            if (currentPlayers.Add(serviceAlias))
            {
                sb.Append("participant ")
                    .Append(serviceAlias)
                    .Append(" as ")
                    .AppendLine(trace.ServiceName);
            }
        }

        return sb.ToString().TrimEnd();
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex SanitizeAliasRegex();

    internal static string SanitizeAlias(string name)
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
            formattedContent = JsonFocusFormatter.FormatWithFocus(formattedContent, focusFields, focusEmphasis, focusDeEmphasis);

        var headersOnTop = string.Join("<br/>", headers
            .Where(y => !excludedHeaders.Contains(y.Key))
            .OrderBy(y => y.Key)
            .Select(y => $"[{y.Key}={y.Value}]"));

        var separator = !string.IsNullOrEmpty(headersOnTop) && !string.IsNullOrEmpty(formattedContent.Trim()) ? "<br/><br/>" : "";
        return (headersOnTop + separator + formattedContent.Trim()).Trim();
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
        return content?
            .Split("&")
            .SelectMany(x =>
            {
                var chunks = x.ChunksUpTo(100).ToArray();
                if (chunks.Length == 0)
                    return chunks;
                chunks[^1] += "&";
                return chunks;
            })
            .StringJoin("<br/>")
            .TrimEnd("&") ?? string.Empty;
    }

    internal static string EscapeMermaid(string text)
    {
        // Mermaid uses # for entity codes and ; as line-break alternatives.
        // Replace # first (using placeholder to avoid double-escaping), then ;.
        const string placeholder = "\uFFFD";
        text = text.Replace("#", placeholder);
        text = text.Replace(";", "#59;");
        text = text.Replace(placeholder, "#35;");
        return text;
    }

    internal static string ToMermaidNote(string content)
    {
        // Convert multi-line content to single-line with <br/> separators for Mermaid notes
        return content
            .Replace("\r\n", "<br/>")
            .Replace("\n", "<br/>")
            .Replace(":", "#colon;");
    }

    private sealed class DiagramBuilder(List<RequestResponseLog> tracesForTest)
    {
        private readonly List<MermaidResult> _results = [];
        private StringBuilder _currentDiagram = new(CreateMermaidPrefix(tracesForTest));
        private string? _openRectLine;
        private bool _eventRectOpen;

        public void Append(string text) => _currentDiagram.Append(text);
        public void AppendLine(string text) => _currentDiagram.AppendLine(text);
        public bool HasOpenRect => _openRectLine != null;

        public void OpenRect(string rectLine)
        {
            AppendLine(rectLine);
            _openRectLine = rectLine;
        }

        public void CloseRect()
        {
            if (_openRectLine != null)
            {
                AppendLine("end");
                _openRectLine = null;
            }
        }

        public void OpenEventRect()
        {
            AppendLine("rect rgb(207, 236, 247)");
            _eventRectOpen = true;
        }

        public void CloseEventRect()
        {
            if (_eventRectOpen)
            {
                AppendLine("end");
                _eventRectOpen = false;
            }
        }

        public bool PlainTextExceedsMaxLength =>
            _currentDiagram.Length > MaxPlainTextDiagramLength;

        public void FinishAndStartNewDiagram()
        {
            var rectToReopen = _openRectLine;
            var eventRectToReopen = _eventRectOpen;

            if (_eventRectOpen)
            {
                AppendLine("end");
                _eventRectOpen = false;
            }

            if (_openRectLine != null)
            {
                AppendLine("end");
            }

            var text = _currentDiagram.ToString();
            _results.Add(new MermaidResult(text));
            _currentDiagram = new StringBuilder(CreateMermaidPrefix(tracesForTest));

            if (rectToReopen != null)
            {
                AppendLine(rectToReopen);
                _openRectLine = rectToReopen;
            }

            if (eventRectToReopen)
            {
                AppendLine("rect rgb(207, 236, 247)");
                _eventRectOpen = true;
            }
        }

        public MermaidResult[] GetResults() => [.. _results];
    }

    private record MermaidResult(string MermaidText);

    public record MermaidForTest(
        string TestId,
        string TestName,
        string[] Diagrams,
        IEnumerable<RequestResponseLog> Traces);
}
