using System.Collections;
using System.Reflection;
using System.Text;

namespace TestTrackingDiagrams.Reports;

internal static class ParameterValueRenderer
{
    private static readonly HashSet<Type> ScalarTypes =
    [
        typeof(string), typeof(bool), typeof(char),
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan),
        typeof(Guid), typeof(Uri)
    ];

    internal static bool IsScalarType(Type type) =>
        type.IsPrimitive || type.IsEnum || ScalarTypes.Contains(type) ||
        Nullable.GetUnderlyingType(type) is { } underlying && IsScalarType(underlying);

    internal static bool IsScalarValue(object? value) =>
        value is null || IsScalarType(value.GetType());

    /// <summary>
    /// Gets the readable public instance properties of a type, excluding compiler-generated backing fields.
    /// </summary>
    internal static PropertyInfo[] GetReadableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();
    }

    /// <summary>
    /// Returns true if the object is a "small complex" suitable for R3 sub-table rendering:
    /// ≤5 scalar properties, no nested complex objects.
    /// </summary>
    internal static bool IsSmallComplexObject(object? value, int maxProperties = 5)
    {
        if (value is null || IsScalarType(value.GetType()))
            return false;
        if (value is IEnumerable)
            return false;

        var props = GetReadableProperties(value.GetType());
        if (props.Length == 0 || props.Length > maxProperties)
            return false;

        return props.All(p =>
        {
            try { return IsScalarValue(p.GetValue(value)); }
            catch { return false; }
        });
    }

    /// <summary>
    /// Returns true if the object is complex (not scalar) — suitable for R4 expandable rendering.
    /// </summary>
    internal static bool IsComplexValue(object? value)
    {
        if (value is null)
            return false;
        return !IsScalarType(value.GetType());
    }

    /// <summary>
    /// Tries to flatten a single complex parameter object into column names and per-scenario values.
    /// Returns the property names if all public properties are scalar and count ≤ maxColumns.
    /// Returns null if the object is not suitable for flattening (R2).
    /// </summary>
    internal static string[]? TryGetFlattenableProperties(object? value, int maxColumns)
    {
        if (value is null || IsScalarType(value.GetType()))
            return null;
        if (value is IEnumerable)
            return null;

        var props = GetReadableProperties(value.GetType());
        if (props.Length == 0 || props.Length > maxColumns)
            return null;

        if (!props.All(p => IsScalarType(p.PropertyType)))
            return null;

        return props.Select(p => p.Name).ToArray();
    }

    /// <summary>
    /// Flattens an object's scalar properties into a string dictionary.
    /// </summary>
    internal static Dictionary<string, string> FlattenToStringValues(object value, string[] propertyNames)
    {
        var result = new Dictionary<string, string>();
        var type = value.GetType();
        foreach (var name in propertyNames)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            result[name] = prop?.GetValue(value)?.ToString() ?? "";
        }
        return result;
    }

    /// <summary>
    /// Flattens an object's properties into a raw object dictionary.
    /// </summary>
    internal static Dictionary<string, object?> FlattenToRawValues(object value, string[] propertyNames)
    {
        var result = new Dictionary<string, object?>();
        var type = value.GetType();
        foreach (var name in propertyNames)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            result[name] = prop?.GetValue(value);
        }
        return result;
    }

    /// <summary>
    /// Renders a cell value as a sub-table (R3) for small complex objects.
    /// </summary>
    internal static void RenderSubTable(StringBuilder body, object value)
    {
        var props = GetReadableProperties(value.GetType());
        body.Append("<table class=\"cell-subtable\">");
        foreach (var prop in props)
        {
            var propValue = prop.GetValue(value)?.ToString() ?? "";
            body.Append($"<tr><th>{System.Net.WebUtility.HtmlEncode(prop.Name)}</th><td>{System.Net.WebUtility.HtmlEncode(propValue)}</td></tr>");
        }
        body.Append("</table>");
    }

    /// <summary>
    /// Renders a cell value as an expandable details/summary (R4) for deeply complex objects.
    /// </summary>
    internal static void RenderExpandable(StringBuilder body, object value)
    {
        var preview = GeneratePreview(value);
        var jsonBody = GenerateHighlightedJson(value, 0);

        body.Append("<details class=\"param-expand\">");
        body.Append($"<summary>{System.Net.WebUtility.HtmlEncode(preview)}</summary>");
        body.Append($"<div class=\"expand-body\">{jsonBody}</div>");
        body.Append("</details>");
    }

    /// <summary>
    /// Generates a short preview string for the summary line.
    /// Format: "TypeName { Prop1: val1, Prop2: val2, ... }"
    /// </summary>
    internal static string GeneratePreview(object value)
    {
        var type = value.GetType();
        var typeName = type.Name;
        var props = GetReadableProperties(type);

        if (props.Length == 0)
            return typeName;

        // Show up to 3 properties in the preview
        var previewParts = new List<string>();
        foreach (var prop in props.Take(3))
        {
            try
            {
                var propValue = prop.GetValue(value);
                string formatted;
                if (propValue is ICollection col)
                    formatted = $"{col.Count}";
                else if (propValue is IEnumerable and not string)
                    formatted = "[...]";
                else
                    formatted = FormatPreviewValue(propValue);
                previewParts.Add($"{prop.Name}: {formatted}");
            }
            catch
            {
                previewParts.Add($"{prop.Name}: ?");
            }
        }

        var suffix = props.Length > 3 ? ", ..." : "";
        return $"{typeName} {{ {string.Join(", ", previewParts)}{suffix} }}";
    }

    private static string FormatPreviewValue(object? value)
    {
        if (value is null) return "null";
        if (value is string s) return $"\"{(s.Length > 30 ? s[..27] + "..." : s)}\"";
        if (value is bool b) return b ? "true" : "false";
        return value.ToString() ?? "null";
    }

    /// <summary>
    /// Generates JSON-like representation with HTML span highlights for prop-key and prop-val.
    /// </summary>
    internal static string GenerateHighlightedJson(object? value, int indent)
    {
        if (value is null)
            return "<span class=\"prop-val\">null</span>";

        if (IsScalarType(value.GetType()))
            return FormatHighlightedScalar(value);

        if (value is IEnumerable enumerable and not string)
            return GenerateHighlightedArray(enumerable, indent);

        return GenerateHighlightedObject(value, indent);
    }

    private static string FormatHighlightedScalar(object value)
    {
        if (value is string s)
            return $"<span class=\"prop-val\">\"{System.Net.WebUtility.HtmlEncode(s)}\"</span>";
        if (value is bool b)
            return $"<span class=\"prop-val\">{(b ? "true" : "false")}</span>";
        if (value is DateTime dt)
            return $"<span class=\"prop-val\">\"{dt:O}\"</span>";
        if (value is DateTimeOffset dto)
            return $"<span class=\"prop-val\">\"{dto:O}\"</span>";
        if (value is DateOnly d)
            return $"<span class=\"prop-val\">\"{d:O}\"</span>";
        if (value is TimeOnly t)
            return $"<span class=\"prop-val\">\"{t:O}\"</span>";
        if (value is TimeSpan ts)
            return $"<span class=\"prop-val\">\"{ts}\"</span>";
        if (value is Guid g)
            return $"<span class=\"prop-val\">\"{g}\"</span>";
        return $"<span class=\"prop-val\">{System.Net.WebUtility.HtmlEncode(value.ToString() ?? "null")}</span>";
    }

    private static string GenerateHighlightedArray(IEnumerable enumerable, int indent)
    {
        var sb = new StringBuilder();
        var indentStr = new string(' ', indent * 2);
        var innerIndent = new string(' ', (indent + 1) * 2);

        sb.Append('[');
        var items = new List<string>();
        foreach (var item in enumerable)
            items.Add(GenerateHighlightedJson(item, indent + 1));

        if (items.Count == 0)
        {
            sb.Append(']');
        }
        else
        {
            sb.Append('\n');
            for (var i = 0; i < items.Count; i++)
            {
                sb.Append(innerIndent);
                sb.Append(items[i]);
                if (i < items.Count - 1) sb.Append(',');
                sb.Append('\n');
            }
            sb.Append(indentStr);
            sb.Append(']');
        }

        return sb.ToString();
    }

    private static string GenerateHighlightedObject(object value, int indent)
    {
        var sb = new StringBuilder();
        var indentStr = new string(' ', indent * 2);
        var innerIndent = new string(' ', (indent + 1) * 2);
        var props = GetReadableProperties(value.GetType());

        sb.Append('{');
        if (props.Length == 0)
        {
            sb.Append('}');
        }
        else
        {
            sb.Append('\n');
            for (var i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                object? propValue;
                try { propValue = prop.GetValue(value); }
                catch { propValue = null; }

                sb.Append(innerIndent);
                sb.Append($"<span class=\"prop-key\">\"{System.Net.WebUtility.HtmlEncode(prop.Name)}\"</span>: ");
                sb.Append(GenerateHighlightedJson(propValue, indent + 1));
                if (i < props.Length - 1) sb.Append(',');
                sb.Append('\n');
            }
            sb.Append(indentStr);
            sb.Append('}');
        }

        return sb.ToString();
    }
}
