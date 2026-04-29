using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.PlantUml;

/// <summary>
/// Detects GraphQL operations (query, mutation, subscription) from HTTP request bodies
/// and extracts operation type and name for diagram arrow labels.
/// </summary>
public static partial class GraphQlOperationDetector
{
    /// <summary>
    /// Attempts to detect a GraphQL request body and extract a human-readable label
    /// (e.g. "query GetUser", "mutation CreateOrder", "subscription OnMessage", or just "query" for anonymous).
    /// Returns null if the content is not a GraphQL request.
    /// </summary>
    public static string? TryExtractLabel(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Fast bail: must look like a JSON object
        var span = content.AsSpan().TrimStart();
        if (span.Length == 0 || span[0] != '{')
            return null;

        // Find "query" : "..." pattern anywhere in the body
        var queryMatch = QueryKeyValuePattern().Match(content);
        if (!queryMatch.Success)
            return null;

        // Verify the "query" key is at the top level of the JSON (depth 1, not nested)
        if (!IsAtTopLevel(content, queryMatch.Index))
            return null;

        var queryValue = queryMatch.Groups["qval"].Value;

        // Strip leading JSON-escaped whitespace (\n, \r, \t) and literal whitespace
        var trimmed = LeadingJsonWhitespace().Replace(queryValue, "");

        // Determine operation type
        string? operationType = null;
        string? inlineName = null;

        if (trimmed.Length > 0 && trimmed[0] == '{')
        {
            // Anonymous query shorthand: { user { name } }
            operationType = "query";
        }
        else
        {
            var opMatch = OperationPattern().Match(trimmed);
            if (!opMatch.Success)
                return null;

            operationType = opMatch.Groups["type"].Value;
            var nameGroup = opMatch.Groups["name"];
            if (nameGroup.Success && !string.IsNullOrEmpty(nameGroup.Value))
                inlineName = nameGroup.Value;
        }

        // Check for explicit operationName field
        string? explicitName = null;
        var opNameMatch = OperationNamePattern().Match(content);
        if (opNameMatch.Success && IsAtTopLevel(content, opNameMatch.Index))
        {
            var val = opNameMatch.Groups["opname"].Value;
            if (!string.IsNullOrEmpty(val))
                explicitName = val;
        }

        var name = explicitName ?? inlineName;
        return name is not null ? $"{operationType} {name}" : operationType;
    }

    /// <summary>
    /// Checks whether the character at <paramref name="position"/> is inside the
    /// outermost JSON object (depth == 1) rather than inside a nested object.
    /// Correctly skips over JSON string literals so that braces inside strings are ignored.
    /// </summary>
    internal static bool IsAtTopLevel(string content, int position)
    {
        var depth = 0;
        for (var i = 0; i < position; i++)
        {
            var c = content[i];
            switch (c)
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    break;
                case '"':
                    // Skip the entire string literal to avoid counting { } inside strings
                    i++;
                    while (i < position)
                    {
                        if (content[i] == '\\')
                        {
                            i++; // skip escaped character
                        }
                        else if (content[i] == '"')
                        {
                            break;
                        }
                        i++;
                    }
                    break;
            }
        }
        return depth == 1;
    }

    // Matches "query" : "<captured value>" — extracts the query string value.
    // Handles escaped characters inside the JSON string value (e.g. \" \n \t).
    [GeneratedRegex(
        """"query"\s*:\s*"(?<qval>[^"\\]*(?:\\.[^"\\]*)*)"""",
        RegexOptions.Singleline)]
    private static partial Regex QueryKeyValuePattern();

    // Strips leading JSON-escaped whitespace sequences and literal whitespace
    [GeneratedRegex(@"^(?:\\[nrt]|\s)+")]
    private static partial Regex LeadingJsonWhitespace();

    // Matches a GraphQL operation: query|mutation|subscription followed by optional name
    // Name stops at whitespace, `(`, or `{`
    [GeneratedRegex(@"^(?<type>query|mutation|subscription)(?:\s+(?<name>\w+))?")]
    private static partial Regex OperationPattern();

    // Matches an explicit "operationName" field in the JSON body
    [GeneratedRegex(""""""
"operationName"\s*:\s*"(?<opname>[^"]*)"
"""""")]
    private static partial Regex OperationNamePattern();
}
