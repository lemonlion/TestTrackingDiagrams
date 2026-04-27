using System.Text.Json;
using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.PlantUml;

public static partial class GraphQlBodyFormatter
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Attempts to format a GraphQL JSON request body according to the specified format.
    /// Returns <c>null</c> if the content is not a GraphQL body or the format is <see cref="GraphQlBodyFormat.Json"/>.
    /// </summary>
    public static string? TryFormat(string? content, GraphQlBodyFormat format)
    {
        if (format == GraphQlBodyFormat.Json)
            return null;

        if (string.IsNullOrWhiteSpace(content))
            return null;

        var span = content.AsSpan().TrimStart();
        if (span.Length == 0 || span[0] != '{')
            return null;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(content);
        }
        catch (JsonException)
        {
            return null;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("query", out var queryElement) ||
                queryElement.ValueKind != JsonValueKind.String)
                return null;

            var rawQuery = queryElement.GetString();
            if (rawQuery is null)
                return null;

            // Verify this is actually a GraphQL query (not a SQL string, etc.)
            if (GraphQlOperationDetector.TryExtractLabel(content) is null)
                return null;

            // Unescape JSON escape sequences the JSON parser already handled,
            // then format
            var formattedQuery = GraphQlQueryFormatter.FormatQuery(rawQuery);

            if (format == GraphQlBodyFormat.FormattedQueryOnly || format == GraphQlBodyFormat.Formatted)
                return formattedQuery;

            // FormattedWithMetadata: append variables and extensions if present
            var result = formattedQuery;

            var variablesSection = TryFormatJsonProperty(doc.RootElement, "variables");
            if (variablesSection is not null)
                result += "\n\nvariables:\n" + variablesSection;

            var extensionsSection = TryFormatJsonProperty(doc.RootElement, "extensions");
            if (extensionsSection is not null)
                result += "\n\nextensions:\n" + extensionsSection;

            return result;
        }
    }

    private static string? TryFormatJsonProperty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
            return null;

        if (element.ValueKind == JsonValueKind.Null)
            return null;

        if (element.ValueKind == JsonValueKind.Object && element.EnumerateObject().Any() == false)
            return null;

        return JsonSerializer.Serialize(element, IndentedJsonOptions).ReplaceLineEndings("\n");
    }
}
