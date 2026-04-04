using System.Diagnostics;
using System.Text;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Renders internal flow segments as PlantUML activity diagrams
/// with swimlanes grouped by source (Activity.Source).
/// </summary>
public static class InternalFlowRenderer
{
    public static string RenderActivityDiagram(InternalFlowSegment segment)
    {
        if (segment.Spans.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("skinparam ActivityBackgroundColor #f0f4ff");
        sb.AppendLine("skinparam ActivityBorderColor #666");
        sb.AppendLine("skinparam SwimlaneBorderColor #ccc");

        var spansBySource = segment.Spans
            .GroupBy(s => string.IsNullOrEmpty(s.Source.Name) ? "Unknown" : s.Source.Name)
            .OrderBy(g => g.Min(s => s.StartTimeUtc));

        foreach (var group in spansBySource)
        {
            sb.AppendLine($"|{EscapePlantUml(group.Key)}|");

            foreach (var span in group.OrderBy(s => s.StartTimeUtc))
            {
                var label = EscapePlantUml(span.DisplayName ?? span.OperationName);
                var duration = span.Duration.TotalMilliseconds;
                sb.AppendLine(duration >= 1
                    ? $":{label} ({duration:F0}ms);"
                    : $":{label};");
            }
        }

        sb.AppendLine("@enduml");
        return sb.ToString();
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

    private static List<SpanNode> BuildSpanTree(Activity[] spans)
    {
        var nodesById = spans.ToDictionary(s => s.SpanId.ToString(), s => new SpanNode(s));
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

    private record SpanNode(Activity Span)
    {
        public List<SpanNode> Children { get; } = [];
    }
}
