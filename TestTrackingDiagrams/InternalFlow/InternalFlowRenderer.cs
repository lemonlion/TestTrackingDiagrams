using System.Diagnostics;
using System.Text;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Renders internal flow segments as PlantUML activity diagrams,
/// call trees, or flame charts.
/// </summary>
public static class InternalFlowRenderer
{
    public static string RenderActivityDiagram(InternalFlowSegment segment)
    {
        if (segment.Spans.Length == 0)
            return string.Empty;

        var roots = BuildSpanTree(segment.Spans);
        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("skinparam ActivityBackgroundColor #f0f4ff");
        sb.AppendLine("skinparam ActivityBorderColor #666");
        sb.AppendLine("skinparam SwimlaneBorderColor #ccc");

        var currentSwimlane = "";
        foreach (var root in roots)
            RenderActivityNode(sb, root, ref currentSwimlane, 0);

        sb.AppendLine("@enduml");
        return sb.ToString();
    }

    private static void RenderActivityNode(StringBuilder sb, SpanNode node, ref string currentSwimlane, int depth)
    {
        var source = string.IsNullOrEmpty(node.Span.Source.Name) ? "Unknown" : node.Span.Source.Name;
        if (source != currentSwimlane)
        {
            sb.AppendLine($"|{EscapePlantUml(source)}|");
            currentSwimlane = source;
        }

        var label = EscapePlantUml(node.Span.DisplayName ?? node.Span.OperationName);
        var duration = node.Span.Duration.TotalMilliseconds;
        sb.AppendLine(duration >= 1
            ? $":{label} ({duration:F0}ms);"
            : $":{label};");

        foreach (var child in node.Children)
            RenderActivityNode(sb, child, ref currentSwimlane, depth + 1);
    }

    public static string RenderCallTree(InternalFlowSegment segment)
    {
        if (segment.Spans.Length == 0)
            return string.Empty;

        var roots = BuildSpanTree(segment.Spans);
        var sb = new StringBuilder();
        sb.AppendLine("<ul class=\"iflow-call-tree\">");
        foreach (var root in roots)
            RenderTreeNode(sb, root);
        sb.AppendLine("</ul>");
        return sb.ToString();
    }

    private static void RenderTreeNode(StringBuilder sb, SpanNode node)
    {
        var duration = node.Span.Duration.TotalMilliseconds;
        var durationText = duration >= 1 ? $" <span class=\"iflow-duration\">({duration:F0}ms)</span>" : "";
        var source = string.IsNullOrEmpty(node.Span.Source.Name) ? "" : $"<span class=\"iflow-source\">[{System.Net.WebUtility.HtmlEncode(node.Span.Source.Name)}]</span> ";
        var name = System.Net.WebUtility.HtmlEncode(node.Span.DisplayName ?? node.Span.OperationName);

        sb.AppendLine($"<li>{source}{name}{durationText}");
        if (node.Children.Count > 0)
        {
            sb.AppendLine("<ul>");
            foreach (var child in node.Children)
                RenderTreeNode(sb, child);
            sb.AppendLine("</ul>");
        }
        sb.AppendLine("</li>");
    }

    internal static List<SpanNode> BuildSpanTree(Activity[] spans)
    {
        var nodesById = new Dictionary<string, SpanNode>(spans.Length);
        foreach (var span in spans)
            nodesById.TryAdd(span.SpanId.ToString(), new SpanNode(span));

        var roots = new List<SpanNode>();

        foreach (var node in nodesById.Values)
        {
            var parentId = node.Span.ParentSpanId.ToString();
            if (parentId != null && nodesById.TryGetValue(parentId, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        roots.Sort((a, b) => a.Span.StartTimeUtc.CompareTo(b.Span.StartTimeUtc));
        foreach (var node in nodesById.Values)
            node.Children.Sort((a, b) => a.Span.StartTimeUtc.CompareTo(b.Span.StartTimeUtc));

        return roots;
    }

    private static string EscapePlantUml(string text)
    {
        return text.Replace("|", "\\|").Replace(";", "\\;");
    }

    /// <summary>
    /// Renders a flame chart as HTML horizontal bars showing relative duration.
    /// Each bar's width is proportional to the span's duration relative to the
    /// total segment time window.
    /// </summary>
    public static string RenderFlameChart(InternalFlowSegment segment)
    {
        if (segment.Spans.Length == 0)
            return string.Empty;

        var roots = BuildSpanTree(segment.Spans);
        var earliest = segment.Spans.Min(s => s.StartTimeUtc);
        var latest = segment.Spans.Max(s => s.StartTimeUtc + s.Duration);
        var totalMs = (latest - earliest).TotalMilliseconds;
        if (totalMs <= 0) totalMs = 1;

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"iflow-flame\">");
        foreach (var root in roots)
            RenderFlameNode(sb, root, earliest, totalMs, 0);
        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static void RenderFlameNode(StringBuilder sb, SpanNode node, DateTime earliest, double totalMs, int depth)
    {
        var offsetMs = (node.Span.StartTimeUtc - earliest).TotalMilliseconds;
        var durationMs = node.Span.Duration.TotalMilliseconds;
        var leftPct = (offsetMs / totalMs) * 100;
        var widthPct = Math.Max((durationMs / totalMs) * 100, 0.5); // min 0.5% so it's visible

        var source = System.Net.WebUtility.HtmlEncode(
            string.IsNullOrEmpty(node.Span.Source.Name) ? "Unknown" : node.Span.Source.Name);
        var name = System.Net.WebUtility.HtmlEncode(node.Span.DisplayName ?? node.Span.OperationName);
        var durationText = durationMs >= 1 ? $" ({durationMs:F0}ms)" : "";

        var hue = Math.Abs(source.GetHashCode()) % 360;
        var color = $"hsl({hue}, 60%, {70 + Math.Min(depth * 5, 20)}%)";

        sb.Append($"<div class=\"iflow-flame-bar\" style=\"margin-left:{leftPct:F2}%;width:{widthPct:F2}%;background:{color}\" ");
        sb.Append($"title=\"[{source}] {name}{durationText}\">");
        sb.Append($"<span class=\"iflow-flame-label\">{name}{durationText}</span>");
        sb.AppendLine("</div>");

        foreach (var child in node.Children)
            RenderFlameNode(sb, child, earliest, totalMs, depth + 1);
    }

    internal record SpanNode(Activity Span)
    {
        public List<SpanNode> Children { get; } = [];
    }
}
