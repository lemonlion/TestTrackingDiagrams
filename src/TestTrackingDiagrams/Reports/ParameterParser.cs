namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Parses parameter name-value pairs from test display names.
/// Supports named, positional, and bracketed formats.
/// </summary>
public static class ParameterParser
{
    /// <summary>
    /// Parses parameter name-value pairs from a test display name string.
    /// Supports: "Method(name: val, name2: val2)" (named), "Method(val1, val2)" (positional),
    /// "Scenario [name: val]" (bracketed).
    /// Returns null if no params are found or input is null/empty.
    /// </summary>
    public static Dictionary<string, string>? Parse(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName))
            return null;

        // Try bracketed format first: "Name [key: val, key2: val2]" or "Name [k1: v1] [k2: v2]"
        var spBracket = displayName.LastIndexOf(" [");
        if (spBracket >= 0 && displayName.EndsWith(']'))
        {
            // Collect all trailing bracket groups: [p1: v1] [p2: v2] ...
            var allParams = new Dictionary<string, string>();
            var remaining = displayName.AsSpan();
            while (true)
            {
                var lastSpace = remaining.LastIndexOf(" [".AsSpan());
                if (lastSpace < 0 || remaining[^1] != ']')
                    break;
                var lastOpen = lastSpace + 1; // point to '['
                var inner = remaining.Slice(lastOpen + 1, remaining.Length - lastOpen - 2).Trim();
                if (inner.Length == 0)
                    break;
                var result = ParseParams(inner.ToString());
                if (result is not { Count: > 0 })
                    break;
                foreach (var kv in result)
                    allParams.TryAdd(kv.Key, kv.Value);
                remaining = remaining[..lastOpen].TrimEnd();
            }
            if (allParams.Count > 0)
                return allParams;
        }

        // Try parens format: "Method(params...)"
        var parenStart = FindOpenParen(displayName);
        if (parenStart < 0 || !displayName.EndsWith(')'))
            return null;

        var parenInner = displayName.Substring(parenStart + 1, displayName.Length - parenStart - 2).Trim();
        if (parenInner.Length == 0)
            return null;

        var parsed = ParseParams(parenInner);
        return parsed is { Count: > 0 } ? parsed : null;
    }

    /// <summary>
    /// Extracts the base name (without parameter suffix) from a display name.
    /// Strips trailing "(params)" or " [params]".
    /// </summary>
    public static string? ExtractBaseName(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName))
            return null;

        // Strip all trailing " [params]" brackets
        var current = displayName.AsSpan();
        while (true)
        {
            var lastOpen = current.LastIndexOf(" [".AsSpan());
            if (lastOpen < 0 || current[^1] != ']')
                break;
            current = current[..lastOpen].TrimEnd();
        }
        if (current.Length < displayName.Length)
            return current.ToString();

        // Try parens: "Method(params)"
        var parenStart = FindOpenParen(displayName);
        if (parenStart >= 0 && displayName.EndsWith(')'))
            return displayName[..parenStart].TrimEnd();

        return displayName;
    }

    private static int FindOpenParen(string s)
    {
        // Find the first '(' that isn't inside quotes
        var inQuote = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '"' && (i == 0 || s[i - 1] != '\\'))
                inQuote = !inQuote;
            if (c == '(' && !inQuote)
                return i;
        }
        return -1;
    }

    private static Dictionary<string, string> ParseParams(string inner)
    {
        var result = new Dictionary<string, string>();
        var tokens = SplitParams(inner);
        var positionalIndex = 0;

        foreach (var token in tokens)
        {
            var trimmed = token.Trim();
            if (trimmed.Length == 0)
                continue;

            // Check for "name: value" pattern
            var colonIdx = FindColon(trimmed);
            if (colonIdx > 0)
            {
                var key = trimmed[..colonIdx].Trim();
                var value = StripQuotes(trimmed[(colonIdx + 1)..].Trim());
                result[key] = value;
            }
            else
            {
                var value = StripQuotes(trimmed);
                result[$"arg{positionalIndex}"] = value;
            }
            positionalIndex++;
        }

        return result;
    }

    private static int FindColon(string s)
    {
        // Find ':' that isn't inside quotes, parens or braces
        var inQuote = false;
        var parenDepth = 0;
        var braceDepth = 0;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '"' && (i == 0 || s[i - 1] != '\\'))
                inQuote = !inQuote;
            if (!inQuote)
            {
                if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                else if (c == '{') braceDepth++;
                else if (c == '}') braceDepth--;
                else if (c == ':' && parenDepth == 0 && braceDepth == 0)
                    return i;
            }
        }
        return -1;
    }

    private static List<string> SplitParams(string inner)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;
        var parenDepth = 0;
        var braceDepth = 0;

        for (var i = 0; i < inner.Length; i++)
        {
            var c = inner[i];

            if (c == '"' && (i == 0 || inner[i - 1] != '\\'))
                inQuote = !inQuote;

            if (!inQuote)
            {
                if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                else if (c == '{') braceDepth++;
                else if (c == '}') braceDepth--;
                else if (c == ',' && parenDepth == 0 && braceDepth == 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    continue;
                }
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1];
        return value;
    }

    /// <summary>
    /// Builds a named parameter dictionary from raw argument values and their corresponding parameter names.
    /// Returns null if inputs are null, empty, or mismatched in length.
    /// </summary>
    public static Dictionary<string, string>? ExtractStructuredParameters(object[]? args, string?[]? paramNames)
    {
        try
        {
            if (args is not { Length: > 0 } || paramNames is not { Length: > 0 })
                return null;

            if (args.Length != paramNames.Length)
                return null;

            var result = new Dictionary<string, string>();
            for (var i = 0; i < args.Length; i++)
            {
                var name = paramNames[i] ?? $"param{i}";
                var value = args[i]?.ToString() ?? "";
                result[name] = value;
            }

            return result.Count > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds both string and raw parameter dictionaries from raw argument values and their corresponding parameter names.
    /// Returns null if inputs are null, empty, or mismatched in length.
    /// </summary>
    public static (Dictionary<string, string> StringValues, Dictionary<string, object?> RawValues)?
        ExtractStructuredParametersWithRaw(object?[]? args, string?[]? paramNames)
    {
        try
        {
            if (args is not { Length: > 0 } || paramNames is not { Length: > 0 })
                return null;

            if (args.Length != paramNames.Length)
                return null;

            var stringResult = new Dictionary<string, string>();
            var rawResult = new Dictionary<string, object?>();
            for (var i = 0; i < args.Length; i++)
            {
                var name = paramNames[i] ?? $"param{i}";
                stringResult[name] = args[i]?.ToString() ?? "";
                rawResult[name] = args[i];
            }

            return stringResult.Count > 0 ? (stringResult, rawResult) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a C# record/class ToString() representation into property name-value pairs.
    /// Handles the pattern: "TypeName { Prop1 = Val1, Prop2 = Val2, ... }"
    /// Values can be: null, numeric, quoted strings (with commas), booleans, or truncated strings (with ··...).
    /// Returns null if the input doesn't match the record pattern.
    /// </summary>
    public static Dictionary<string, string>? TryParseRecordToString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // Match pattern: "TypeName { ... }" or truncated "TypeName { ... ··..."
        var braceOpen = value.IndexOf(" { ", StringComparison.Ordinal);
        if (braceOpen < 0 || braceOpen == 0)
            return null;

        // Detect whether the record is truncated (ends with ··... or ... instead of " }")
        var isTruncated = !value.EndsWith(" }");
        if (isTruncated && !IsTruncationSuffix(value))
            return null;

        // inner = everything between "{ " and " }" (or end for truncated)
        var innerStart = braceOpen + 3;
        var innerEnd = isTruncated ? value.Length : value.Length - 2;
        if (innerEnd <= innerStart)
            return null;

        var inner = value[innerStart..innerEnd].Trim();
        if (inner.Length == 0)
            return null;

        // Strip trailing truncation markers from the inner content
        if (isTruncated)
            inner = StripTrailingTruncation(inner);

        var result = new Dictionary<string, string>();
        var pos = 0;

        while (pos < inner.Length)
        {
            // Skip whitespace
            while (pos < inner.Length && inner[pos] == ' ') pos++;
            if (pos >= inner.Length) break;

            // Read property name (until ' = ')
            var eqIdx = inner.IndexOf(" = ", pos, StringComparison.Ordinal);
            if (eqIdx < 0)
            {
                // Truncation mid-property-name: discard incomplete property and stop
                if (isTruncated) break;
                return null; // malformed
            }

            var propName = inner[pos..eqIdx].Trim();
            if (propName.Length == 0) return null;

            pos = eqIdx + 3; // skip " = "

            // Read value
            string propValue;
            if (pos < inner.Length && inner[pos] == '"')
            {
                // Quoted string — read until unescaped closing quote
                var sb = new System.Text.StringBuilder();
                pos++; // skip opening quote
                var closedQuote = false;
                while (pos < inner.Length)
                {
                    var c = inner[pos];
                    if (c == '\\' && pos + 1 < inner.Length)
                    {
                        sb.Append(inner[pos + 1]);
                        pos += 2;
                        continue;
                    }
                    if (c == '"')
                    {
                        pos++; // skip closing quote
                        closedQuote = true;
                        break;
                    }
                    sb.Append(c);
                    pos++;
                }
                propValue = sb.ToString();

                if (!closedQuote && isTruncated)
                {
                    // Truncation inside a quoted value — keep what we have and stop
                    result[propName] = propValue;
                    break;
                }

                // Skip any trailing ··... (truncation marker)
                while (pos < inner.Length && (inner[pos] == '\u00B7' || inner[pos] == '.'))
                    pos++;
            }
            else
            {
                // Unquoted value — read until ", " at brace-depth 0 or end
                var valueStart = pos;
                var braceDepth = 0;
                while (pos < inner.Length)
                {
                    var c = inner[pos];
                    if (c == '{') braceDepth++;
                    else if (c == '}') braceDepth--;
                    else if (c == ',' && braceDepth == 0)
                        break;
                    pos++;
                }
                propValue = inner[valueStart..pos].Trim();

                // Strip trailing truncation markers from value
                propValue = StripTrailingTruncation(propValue);
            }

            result[propName] = propValue;

            // Skip ", "
            if (pos < inner.Length && inner[pos] == ',')
            {
                pos++;
                while (pos < inner.Length && inner[pos] == ' ') pos++;
            }
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Checks if the string ends with a truncation suffix (··... or ... patterns).
    /// </summary>
    private static bool IsTruncationSuffix(string value)
    {
        var i = value.Length - 1;
        // Walk backwards over dots and middle dots
        while (i >= 0 && (value[i] == '.' || value[i] == '\u00B7'))
            i--;
        // We consumed at least one truncation character, and we're not at the same position
        return i < value.Length - 1;
    }

    /// <summary>
    /// Strips trailing truncation markers (··... or ...) from a string.
    /// </summary>
    private static string StripTrailingTruncation(string value)
    {
        var i = value.Length - 1;
        while (i >= 0 && (value[i] == '.' || value[i] == '\u00B7'))
            i--;
        return i < value.Length - 1 ? value[..(i + 1)].TrimEnd() : value;
    }
}
