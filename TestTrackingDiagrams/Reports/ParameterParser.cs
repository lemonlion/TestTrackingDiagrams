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

        // Try bracketed format first: "Name [key: val, key2: val2]"
        var bracketStart = displayName.LastIndexOf('[');
        if (bracketStart >= 0 && displayName.EndsWith(']'))
        {
            var inner = displayName.Substring(bracketStart + 1, displayName.Length - bracketStart - 2).Trim();
            if (inner.Length > 0)
            {
                var result = ParseParams(inner);
                return result is { Count: > 0 } ? result : null;
            }
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

        // Try bracketed: "Name [params]"
        var bracketStart = displayName.LastIndexOf(" [");
        if (bracketStart >= 0 && displayName.EndsWith(']'))
            return displayName[..bracketStart].TrimEnd();

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
        // Find ':' that isn't inside quotes or parens
        var inQuote = false;
        var parenDepth = 0;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '"' && (i == 0 || s[i - 1] != '\\'))
                inQuote = !inQuote;
            if (!inQuote)
            {
                if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                else if (c == ':' && parenDepth == 0)
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

        for (var i = 0; i < inner.Length; i++)
        {
            var c = inner[i];

            if (c == '"' && (i == 0 || inner[i - 1] != '\\'))
                inQuote = !inQuote;

            if (!inQuote)
            {
                if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                else if (c == ',' && parenDepth == 0)
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
}
