namespace TestTrackingDiagrams.Reports;

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
        var bracketStart = displayName.LastIndexOf('[');
        if (bracketStart >= 0 && displayName.EndsWith(']'))
        {
            // Collect all trailing bracket groups: [p1: v1] [p2: v2] ...
            var allParams = new Dictionary<string, string>();
            var remaining = displayName.AsSpan();
            while (true)
            {
                var lastOpen = remaining.LastIndexOf('[');
                if (lastOpen < 0 || remaining[^1] != ']')
                    break;
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
}
