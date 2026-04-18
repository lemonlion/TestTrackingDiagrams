using System.Linq.Expressions;

namespace TestTrackingDiagrams;

public static class DiagramFocus
{
    private static readonly AsyncLocal<FocusHolder> PendingRequestFocus = new();
    private static readonly AsyncLocal<FocusHolder> PendingResponseFocus = new();

    public static void Request<T>(params Expression<Func<T, object?>>[] fields)
    {
        PendingRequestFocus.Value = new FocusHolder { Fields = fields.Select(ExtractPropertyName).ToArray() };
    }

    public static void Request(params string[] fieldNames)
    {
        PendingRequestFocus.Value = new FocusHolder { Fields = fieldNames };
    }

    public static void Response<T>(params Expression<Func<T, object?>>[] fields)
    {
        PendingResponseFocus.Value = new FocusHolder { Fields = fields.Select(ExtractPropertyName).ToArray() };
    }

    public static void Response(params string[] fieldNames)
    {
        PendingResponseFocus.Value = new FocusHolder { Fields = fieldNames };
    }

    internal static string[]? ConsumePendingRequestFocus()
    {
        var holder = PendingRequestFocus.Value;
        if (holder is null) return null;
        var value = holder.Fields;
        holder.Fields = null;
        return value;
    }

    internal static string[]? ConsumePendingResponseFocus()
    {
        var holder = PendingResponseFocus.Value;
        if (holder is null) return null;
        var value = holder.Fields;
        holder.Fields = null;
        return value;
    }

    internal static void ClearAll()
    {
        PendingRequestFocus.Value = null!;
        PendingResponseFocus.Value = null!;
    }

    internal static string ExtractPropertyName<T>(Expression<Func<T, object?>> expression)
    {
        return expression.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression member } => member.Member.Name, // boxing (int/bool → object)
            _ => throw new ArgumentException($"Expression must be a simple property access, e.g. x => x.PropertyName. Got: {expression.Body.NodeType}")
        };
    }

    private sealed class FocusHolder
    {
        public string[]? Fields;
    }
}
