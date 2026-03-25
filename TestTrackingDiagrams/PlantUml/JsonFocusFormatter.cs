using System.Text;
using System.Text.Json;

namespace TestTrackingDiagrams.PlantUml;

public static class JsonFocusFormatter
{
    public static string FormatWithFocus(
        string prettyPrintedJson,
        string[]? focusFields,
        FocusEmphasis emphasis,
        FocusDeEmphasis deEmphasis)
    {
        if (focusFields is null or { Length: 0 })
            return prettyPrintedJson;

        var lines = prettyPrintedJson.Split(Environment.NewLine);
        if (lines.Length < 3) // at minimum: { field }
            return prettyPrintedJson;

        var focusSet = new HashSet<string>(focusFields, StringComparer.OrdinalIgnoreCase);

        // Parse lines to determine which top-level property each belongs to
        var lineAnnotations = AnnotateLines(lines, focusSet);

        // If no fields matched, return unchanged
        if (!lineAnnotations.Any(a => a == LineType.Focused))
            return prettyPrintedJson;

        var isHidden = deEmphasis.HasFlag(FocusDeEmphasis.Hidden);

        if (isHidden)
            return BuildHiddenOutput(lines, lineAnnotations, emphasis);

        return BuildFormattedOutput(lines, lineAnnotations, emphasis, deEmphasis);
    }

    private static LineType[] AnnotateLines(string[] lines, HashSet<string> focusSet)
    {
        var annotations = new LineType[lines.Length];
        var currentFocusState = LineType.Structural;
        var nestingDepth = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            // Opening/closing braces at root level
            if (trimmed is "{" or "}" or "[" or "]" or "}," or "],")
            {
                if (nestingDepth == 0)
                {
                    annotations[i] = LineType.Structural;
                    continue;
                }
            }

            // Detect top-level property start
            if (nestingDepth == 0 && trimmed.StartsWith('"'))
            {
                var propertyName = ExtractPropertyName(trimmed);
                if (propertyName is not null && focusSet.Contains(propertyName))
                {
                    currentFocusState = LineType.Focused;
                }
                else
                {
                    currentFocusState = LineType.NonFocused;
                }
            }

            annotations[i] = currentFocusState;

            // Track nesting for multi-line values (objects/arrays)
            nestingDepth += CountNestingChange(trimmed);
        }

        return annotations;
    }

    private static string? ExtractPropertyName(string trimmedLine)
    {
        // Line format: "propertyName": value  or  "propertyName": {
        if (!trimmedLine.StartsWith('"'))
            return null;

        var endQuote = trimmedLine.IndexOf('"', 1);
        if (endQuote <= 1)
            return null;

        return trimmedLine[1..endQuote];
    }

    private static int CountNestingChange(string trimmedLine)
    {
        var change = 0;
        var inString = false;
        var escaped = false;

        foreach (var c in trimmedLine)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            switch (c)
            {
                case '{' or '[':
                    change++;
                    break;
                case '}' or ']':
                    change--;
                    break;
            }
        }

        return change;
    }

    private static string BuildFormattedOutput(
        string[] lines,
        LineType[] annotations,
        FocusEmphasis emphasis,
        FocusDeEmphasis deEmphasis)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            switch (annotations[i])
            {
                case LineType.Structural:
                    sb.AppendLine(line);
                    break;
                case LineType.Focused:
                    sb.AppendLine(ApplyEmphasis(line, emphasis));
                    break;
                case LineType.NonFocused:
                    sb.AppendLine(ApplyDeEmphasis(line, deEmphasis));
                    break;
            }
        }

        // Remove trailing newline to match input format
        return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
    }

    private static string BuildHiddenOutput(
        string[] lines,
        LineType[] annotations,
        FocusEmphasis emphasis)
    {
        var sb = new StringBuilder();
        var lastWasEllipsis = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            switch (annotations[i])
            {
                case LineType.Structural:
                    sb.AppendLine(line);
                    lastWasEllipsis = false;
                    break;
                case LineType.Focused:
                    // Remove trailing comma if the next visible line is not focused
                    // (to keep JSON-like appearance clean)
                    var formattedLine = ApplyEmphasis(RemoveTrailingCommaIfNextIsHidden(line, i, annotations), emphasis);
                    sb.AppendLine(formattedLine);
                    lastWasEllipsis = false;
                    break;
                case LineType.NonFocused:
                    if (!lastWasEllipsis)
                    {
                        sb.AppendLine("  ...");
                        lastWasEllipsis = true;
                    }
                    break;
            }
        }

        return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
    }

    private static string RemoveTrailingCommaIfNextIsHidden(string line, int index, LineType[] annotations)
    {
        // If the next non-structural line is NonFocused (hidden), and this line ends with a comma, remove it
        for (var j = index + 1; j < annotations.Length; j++)
        {
            if (annotations[j] == LineType.Structural)
                continue;
            if (annotations[j] == LineType.NonFocused)
            {
                var trimmed = line.TrimEnd();
                if (trimmed.EndsWith(','))
                    return line[..line.LastIndexOf(',')];
            }
            break;
        }

        return line;
    }

    private static string ApplyEmphasis(string line, FocusEmphasis emphasis)
    {
        if (emphasis == FocusEmphasis.None)
            return line;

        var indent = GetIndent(line);
        var content = line[indent.Length..];

        if (emphasis.HasFlag(FocusEmphasis.Colored))
            content = $"<color:blue>{content}</color>";
        if (emphasis.HasFlag(FocusEmphasis.Bold))
            content = $"<b>{content}</b>";

        return indent + content;
    }

    private static string ApplyDeEmphasis(string line, FocusDeEmphasis deEmphasis)
    {
        if (deEmphasis == FocusDeEmphasis.None)
            return line;

        var indent = GetIndent(line);
        var content = line[indent.Length..];

        if (deEmphasis.HasFlag(FocusDeEmphasis.SmallerText))
            content = $"<size:9>{content}</size>";
        if (deEmphasis.HasFlag(FocusDeEmphasis.LightGray))
            content = $"<color:lightgray>{content}</color>";

        return indent + content;
    }

    private static string GetIndent(string line)
    {
        var i = 0;
        while (i < line.Length && line[i] == ' ')
            i++;
        return line[..i];
    }

    private enum LineType
    {
        Structural,
        Focused,
        NonFocused
    }
}
