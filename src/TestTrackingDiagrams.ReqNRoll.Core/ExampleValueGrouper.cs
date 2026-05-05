using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Groups flat ReqNRoll Example column values into structured objects based on step table data.
/// Detects which Example columns are consumed by step tables (via value matching) and produces
/// ExampleRawValues with dictionary objects for sub-table rendering (matching xUnit3 MemberData behavior).
/// </summary>
internal static class ExampleValueGrouper
{
    // Extracts a group name from step text patterns like "the following X:" or "with X:"
    private static readonly Regex TableNamePattern = new(
        @"(?:the\s+following\s+)?(?:\w+\s+)?(\w+)\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Builds structured ExampleValues and ExampleRawValues from flat Example columns and step table data.
    /// Single-row tables produce Dictionary&lt;string, object?&gt; (R3 sub-table rendering).
    /// Multi-row tables produce List&lt;Dictionary&lt;string, object?&gt;&gt; (R4 expandable rendering).
    /// Remaining columns not consumed by any table stay as flat scalars.
    /// </summary>
    public static (Dictionary<string, string> ExampleValues, Dictionary<string, object?>? ExampleRawValues)
        BuildStructured(Dictionary<string, string> flatValues, List<ReqNRollStepInfo> steps)
    {
        if (flatValues.Count == 0)
            return (flatValues, null);

        var consumedKeys = new HashSet<string>();
        var groupedValues = new Dictionary<string, string>();
        var groupedRawValues = new Dictionary<string, object?>();

        // Process each step that has table data
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.TableText))
                continue;

            var tableResult = TryGroupFromTable(step, flatValues, consumedKeys);
            if (tableResult is null)
                continue;

            groupedValues[tableResult.Value.Name] = tableResult.Value.DisplayValue;
            groupedRawValues[tableResult.Value.Name] = tableResult.Value.RawValue;
        }

        // If no tables matched any columns, return flat values only
        if (consumedKeys.Count == 0)
            return (flatValues, null);

        // Build final dictionaries: unconsumed scalars + grouped table data
        var finalValues = new Dictionary<string, string>();
        var finalRawValues = new Dictionary<string, object?>();

        // Add unconsumed scalar values first (preserving original order)
        foreach (var kvp in flatValues)
        {
            if (!consumedKeys.Contains(kvp.Key))
            {
                finalValues[kvp.Key] = kvp.Value;
                finalRawValues[kvp.Key] = kvp.Value;
            }
        }

        // Add grouped values
        foreach (var kvp in groupedValues)
            finalValues[kvp.Key] = kvp.Value;
        foreach (var kvp in groupedRawValues)
            finalRawValues[kvp.Key] = kvp.Value;

        return (finalValues, finalRawValues);
    }

    private static (string Name, string DisplayValue, object RawValue)?
        TryGroupFromTable(ReqNRollStepInfo step, Dictionary<string, string> flatValues, HashSet<string> consumedKeys)
    {
        var lines = step.TableText!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) // Need header + at least one data row
            return null;

        var headers = ParseTableRow(lines[0]);
        if (headers.Length == 0)
            return null;

        // Parse data rows
        var dataRows = new List<string[]>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cells = ParseTableRow(lines[i]);
            if (cells.Length > 0)
                dataRows.Add(cells);
        }

        if (dataRows.Count == 0)
            return null;

        // Match table cell values to Example columns (detect which flat columns are consumed)
        var columnMappings = DetectColumnMappings(headers, dataRows, flatValues, consumedKeys);
        if (columnMappings is null || columnMappings.Count == 0)
            return null;

        // Mark matched columns as consumed
        foreach (var mapping in columnMappings.Values)
            consumedKeys.Add(mapping);

        // Derive group name from step text
        var groupName = DeriveGroupName(step.Text, headers);

        if (dataRows.Count == 1)
        {
            // Single-row table → object-like dictionary (R3 sub-table)
            var dict = new Dictionary<string, object?>();
            for (var col = 0; col < headers.Length; col++)
            {
                var cellValue = col < dataRows[0].Length ? dataRows[0][col] : "";
                dict[headers[col]] = cellValue;
            }

            var displayValue = string.Join(", ", dict.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            return (groupName, displayValue, dict);
        }
        else
        {
            // Multi-row table → list of dictionaries (R4 expandable)
            var list = new List<Dictionary<string, object?>>();
            foreach (var row in dataRows)
            {
                var dict = new Dictionary<string, object?>();
                for (var col = 0; col < headers.Length; col++)
                {
                    var cellValue = col < row.Length ? row[col] : "";
                    dict[headers[col]] = cellValue;
                }
                list.Add(dict);
            }

            var displayValue = $"{list.Count} items";
            return (groupName, displayValue, list);
        }
    }

    /// <summary>
    /// Detects which table columns map to which Example columns by value matching.
    /// For single-row tables: matches by header name or cell value.
    /// For multi-row tables: matches ALL cell values across ALL rows to find consumed Example columns.
    /// Returns the set of consumed Example key names, or null if not enough matches.
    /// </summary>
    private static Dictionary<int, string>? DetectColumnMappings(
        string[] headers, List<string[]> dataRows, Dictionary<string, string> flatValues, HashSet<string> alreadyConsumed)
    {
        var mappings = new Dictionary<int, string>();
        var usedExampleKeys = new HashSet<string>(alreadyConsumed);

        if (dataRows.Count == 1)
        {
            // Single-row table: match by header name or cell value
            for (var col = 0; col < headers.Length; col++)
            {
                // Strategy 1: Exact header name match (case-insensitive)
                var matchByName = flatValues.Keys
                    .FirstOrDefault(k => k.Equals(headers[col], StringComparison.OrdinalIgnoreCase)
                                      && !usedExampleKeys.Contains(k));
                if (matchByName is not null)
                {
                    mappings[col] = matchByName;
                    usedExampleKeys.Add(matchByName);
                    continue;
                }

                // Strategy 2: Value match in the data row
                if (col < dataRows[0].Length)
                {
                    var cellValue = dataRows[0][col];
                    var matchByValue = flatValues
                        .Where(kvp => !usedExampleKeys.Contains(kvp.Key)
                                   && kvp.Value.Equals(cellValue, StringComparison.Ordinal))
                        .Select(kvp => kvp.Key)
                        .FirstOrDefault();

                    if (matchByValue is not null)
                    {
                        mappings[col] = matchByValue;
                        usedExampleKeys.Add(matchByValue);
                    }
                }
            }
        }
        else
        {
            // Multi-row table: match ALL cell values across ALL rows to Example columns.
            // Each cell value in the table should correspond to a different Example column.
            for (var row = 0; row < dataRows.Count; row++)
            {
                for (var col = 0; col < headers.Length; col++)
                {
                    if (col >= dataRows[row].Length)
                        continue;

                    var cellValue = dataRows[row][col];
                    var matchByValue = flatValues
                        .Where(kvp => !usedExampleKeys.Contains(kvp.Key)
                                   && kvp.Value.Equals(cellValue, StringComparison.Ordinal))
                        .Select(kvp => kvp.Key)
                        .FirstOrDefault();

                    if (matchByValue is not null)
                    {
                        // Use a row-specific key for multi-row tracking
                        mappings[row * headers.Length + col] = matchByValue;
                        usedExampleKeys.Add(matchByValue);
                    }
                }
            }
        }

        // Only group if we matched at least one column
        return mappings.Count > 0 ? mappings : null;
    }

    /// <summary>
    /// Derives a meaningful group name from the step text.
    /// Looks for patterns like "the following toppings:", "with the following ingredients:", etc.
    /// Falls back to using table headers joined with "+".
    /// </summary>
    internal static string DeriveGroupName(string stepText, string[] headers)
    {
        // Try common patterns: "the following X:" or "the following Y X:"
        var patterns = new[]
        {
            @"(?:the\s+following\s+(?:\w+\s+)?)(\w+)\s*:?\s*$",  // "the following [adj] NOUN:"
            @"with\s+(?:the\s+)?(\w+)\s*:?\s*$",                  // "with [the] NOUN:"
            @"(\w+)\s*:?\s*$"                                      // "NOUN:" at end
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(stepText, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                // Skip generic words
                if (!IsGenericWord(name))
                    return TitleCase(name);
            }
        }

        // Fallback: join table headers
        return string.Join(" + ", headers);
    }

    private static bool IsGenericWord(string word)
    {
        var generic = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "is", "are", "was", "were", "be", "been",
            "have", "has", "had", "do", "does", "did", "will", "would",
            "could", "should", "may", "might", "shall", "can", "that", "this"
        };
        return generic.Contains(word);
    }

    private static string TitleCase(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;
        return char.ToUpperInvariant(word[0]) + word[1..];
    }

    private static string[] ParseTableRow(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith('|') || !trimmed.EndsWith('|'))
            return [];

        return trimmed[1..^1]
            .Split('|')
            .Select(cell => cell.Trim())
            .ToArray();
    }
}
