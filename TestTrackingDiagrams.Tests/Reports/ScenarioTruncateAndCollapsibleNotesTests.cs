using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ScenarioTruncateAndCollapsibleNotesTests
{
    private readonly string _script = DiagramContextMenu.GetCollapsibleNotesScript();

    // ═══════════════════════════════════════════════════════════
    // Bug 1: _setScenarioTruncateLines must defer _truncateLines
    //         restore until processRenderQueue completes
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SetScenarioTruncateLines_restores_truncateLines_in_onAllDone_callback()
    {
        // The restore of window._truncateLines must happen inside the
        // processRenderQueue callback, not synchronously after the call.
        // Bad:  processRenderQueue(...); window._truncateLines = prev;
        // Good: processRenderQueue(..., function() { window._truncateLines = prev; });

        var fnBody = ExtractFunctionBody(_script, "_setScenarioTruncateLines");

        // Should NOT restore _truncateLines synchronously after processRenderQueue
        Assert.DoesNotContain("processRenderQueue(buildDetailsQueue(containers, 'truncated', true));\n                syncRadioButtons(scenario, 'truncated');\n                window._truncateLines = prev;", fnBody);

        // Should restore in callback
        Assert.Contains("_truncateLines = prev", fnBody);
        // The restore should be inside a function passed to processRenderQueue
        Assert.Matches(@"processRenderQueue\(.*,\s*function\s*\(\)\s*\{[^}]*_truncateLines\s*=\s*prev", _script);
    }

    // ═══════════════════════════════════════════════════════════
    // Bug 2: makeNotesCollapsible must remove existing toggle
    //         icons and hover rects before adding new ones
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void MakeNotesCollapsible_removes_existing_toggle_icons_before_adding_new_ones()
    {
        var fnBody = ExtractFunctionBody(_script, "makeNotesCollapsible");
        // Should remove old .note-toggle-icon elements
        Assert.Contains(".note-toggle-icon", fnBody);
        Assert.Matches(@"querySelectorAll\(['""]\.note-toggle-icon['""].*forEach.*remove", fnBody);
    }

    [Fact]
    public void MakeNotesCollapsible_removes_existing_hover_rects_before_adding_new_ones()
    {
        var fnBody = ExtractFunctionBody(_script, "makeNotesCollapsible");
        // Should remove old .note-hover-rect elements
        Assert.Contains(".note-hover-rect", fnBody);
        Assert.Matches(@"querySelectorAll\(['""]\.note-hover-rect['""].*forEach.*remove", fnBody);
    }

    // ═══════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════

    private static string ExtractFunctionBody(string content, string functionName)
    {
        // Handle both regular functions and window.X = function(...)
        var idx = content.IndexOf($"function {functionName}(", StringComparison.Ordinal);
        if (idx < 0) idx = content.IndexOf($".{functionName} = function(", StringComparison.Ordinal);
        if (idx < 0) idx = content.IndexOf($"_{functionName} = function(", StringComparison.Ordinal);
        Assert.True(idx >= 0, $"Function '{functionName}' not found in content");

        var braceStart = content.IndexOf('{', idx);
        Assert.True(braceStart >= 0);

        var depth = 0;
        for (var i = braceStart; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}') depth--;
            if (depth == 0) return content[braceStart..(i + 1)];
        }
        throw new Exception($"Unmatched braces in function '{functionName}'");
    }
}
