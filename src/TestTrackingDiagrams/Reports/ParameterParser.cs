using System.Text.RegularExpressions;

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
                if (remaining.Length < 3 || remaining[^1] != ']')
                    break;
                var matchingOpen = FindMatchingOpenBracket(remaining);
                if (matchingOpen < 1 || remaining[matchingOpen - 1] != ' ')
                    break;
                var lastOpen = matchingOpen;
                var inner = remaining.Slice(lastOpen + 1, remaining.Length - lastOpen - 2).Trim();
                if (inner.Length == 0)
                    break;
                var result = ParseParams(inner.ToString());
                if (result is not { Count: > 0 })
                    break;
                foreach (var kv in result)
                    allParams.TryAdd(kv.Key, kv.Value);
                remaining = remaining[..(lastOpen - 1)].TrimEnd();
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
            if (current.Length < 3 || current[^1] != ']')
                break;
            var matchingOpen = FindMatchingOpenBracket(current);
            if (matchingOpen < 1 || current[matchingOpen - 1] != ' ')
                break;
            current = current[..(matchingOpen - 1)].TrimEnd();
        }
        if (current.Length < displayName.Length)
            return current.ToString();

        // Try parens: "Method(params)"
        var parenStart = FindOpenParen(displayName);
        if (parenStart >= 0 && displayName.EndsWith(')'))
            return displayName[..parenStart].TrimEnd();

        return displayName;
    }

    /// <summary>
    /// Finds the index of the '[' that matches the trailing ']' in a span,
    /// considering nested bracket pairs.  Returns -1 if no match found.
    /// </summary>
    private static int FindMatchingOpenBracket(ReadOnlySpan<char> span)
    {
        var depth = 0;
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (span[i] == ']') depth++;
            else if (span[i] == '[') depth--;
            if (depth == 0)
                return i;
        }
        return -1;
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
        // Find ':' that isn't inside quotes, parens, braces or brackets
        var inQuote = false;
        var parenDepth = 0;
        var braceDepth = 0;
        var bracketDepth = 0;
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
                else if (c == '[') bracketDepth++;
                else if (c == ']') bracketDepth--;
                else if (c == ':' && parenDepth == 0 && braceDepth == 0 && bracketDepth == 0)
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
        var bracketDepth = 0;

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
                else if (c == '[') bracketDepth++;
                else if (c == ']') bracketDepth--;
                else if (c == ',' && parenDepth == 0 && braceDepth == 0 && bracketDepth == 0)
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
                var value = args[i]?.ToString() ?? "null";
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
                stringResult[name] = args[i]?.ToString() ?? "null";
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
                // Handle trailing property with null value: "PropName =" (no trailing space after trim)
                var trimmedTail = inner[pos..].TrimEnd();
                if (trimmedTail.EndsWith(" ="))
                {
                    var trailingPropName = trimmedTail[..^2].Trim();
                    if (trailingPropName.Length > 0)
                        result[trailingPropName] = "null";
                    break;
                }

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

                // C# record ToString() renders null properties as empty after " = "
                if (propValue.Length == 0)
                    propValue = "null";
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

    private static readonly Regex GenericTypePattern = new(@"^(?:[\w.]+\.)?(\w+)`\d+\[(.+)\]$", RegexOptions.Compiled);

    /// <summary>
    /// Returns true if the string value represents a complex object — either a record-style
    /// ToString ("TypeName { Prop = Val, ... }") or a generic collection type ("List`1[...]").
    /// </summary>
    public static bool IsComplexObjectString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Record-style: "TypeName { Prop = Val, ... }"
        if (TryParseRecordToString(value) is not null)
            return true;

        // Generic collection: "List`1[System.String]"
        return GenericTypePattern.IsMatch(value);
    }

    /// <summary>
    /// Extracts a short type name from a complex object string.
    /// For record-style ("TypeName { ... }"), returns the type name before " { ".
    /// For generic collection ("List`1[Namespace.Type]"), returns "List&lt;Type&gt;".
    /// Returns null if the value is not a complex object string.
    /// </summary>
    public static string? ExtractTypeNameFromComplexString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // Record-style: extract text before " { "
        var braceOpen = value.IndexOf(" { ", StringComparison.Ordinal);
        if (braceOpen > 0 && TryParseRecordToString(value) is not null)
            return value[..braceOpen];

        // Generic collection: "List`1[System.String]" → "List<String>"
        var match = GenericTypePattern.Match(value);
        if (match.Success)
        {
            var baseName = match.Groups[1].Value;
            var argsRaw = match.Groups[2].Value;
            // Split type args on ", " (top-level only)
            var typeArgs = SplitTypeArgs(argsRaw);
            var shortArgs = typeArgs.Select(a =>
            {
                var lastDot = a.LastIndexOf('.');
                return lastDot >= 0 ? a[(lastDot + 1)..] : a;
            });
            return $"{baseName}<{string.Join(", ", shortArgs)}>";
        }

        return null;
    }

    private static List<string> SplitTypeArgs(string argsRaw)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < argsRaw.Length; i++)
        {
            var c = argsRaw[i];
            if (c == '[') depth++;
            else if (c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                result.Add(argsRaw[start..i].Trim());
                start = i + 1;
            }
        }
        result.Add(argsRaw[start..].Trim());
        return result;
    }

    /// <summary>
    /// Returns true if the complex object value is small enough to render inline
    /// (record with fewer than 5 simple fields, without nested records).
    /// </summary>
    public static bool IsSmallComplexValue(string? value)
    {
        var props = TryParseRecordToString(value);
        if (props is null || props.Count == 0)
            return false;

        if (props.Count >= 5)
            return false;

        // If any value itself looks like a nested record, it's not small
        foreach (var v in props.Values)
        {
            if (TryParseRecordToString(v) is not null)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Formats a small complex object value for inline display.
    /// Record-style "TypeName { Name = Val, Flour = Plain }" → "{ Name: Val, Flour: Plain }".
    /// Returns null if the value cannot be parsed.
    /// </summary>
    public static string? FormatComplexValueInline(string? value)
    {
        var props = TryParseRecordToString(value);
        if (props is null || props.Count == 0)
            return null;

        var parts = props.Select(kv => $"{kv.Key}: {kv.Value}");
        return $"{{ {string.Join(", ", parts)} }}";
    }

    /// <summary>
    /// Formats a complex object value as pretty-printed JSON (without the TypeName).
    /// Record-style values are converted to JSON objects with proper quoting of string values.
    /// Returns null if the value cannot be parsed.
    /// </summary>
    public static string? FormatComplexValueAsJson(string? value)
    {
        var props = TryParseRecordToString(value);
        if (props is null || props.Count == 0)
            return null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        var entries = props.ToArray();
        for (var i = 0; i < entries.Length; i++)
        {
            var (key, val) = entries[i];
            sb.Append($"  \"{key}\": ");
            sb.Append(FormatJsonValue(val));
            if (i < entries.Length - 1)
                sb.Append(',');
            sb.AppendLine();
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string FormatJsonValue(string val)
    {
        if (val == "null")
            return "null";
        if (val.Equals("true", StringComparison.OrdinalIgnoreCase))
            return "true";
        if (val.Equals("false", StringComparison.OrdinalIgnoreCase))
            return "false";
        if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            return val;
        return $"\"{val.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }
}
