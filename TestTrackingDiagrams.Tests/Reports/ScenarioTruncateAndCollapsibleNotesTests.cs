using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ScenarioTruncateAndCollapsibleNotesTests
{
    private readonly string _script = DiagramContextMenu.GetCollapsibleNotesScript();

    // ═══════════════════════════════════════════════════════════
    // Fix: _setScenarioTruncateLines stores per-container
    //       truncateLines instead of temporarily overriding global
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SetScenarioTruncateLines_stores_per_container_truncate_lines()
    {
        // The truncation line count must be stored per-container so that
        // headers toggle and details toggle preserve the scenario's value
        // instead of reverting to the global default.
        // Old approach: temporarily override window._truncateLines, restore in callback
        // New approach: set container._truncateLines on each container

        var fnBody = ExtractFunctionBody(_script, "_setScenarioTruncateLines");

        // Should store per-container
        Assert.Contains("_truncateLines = scenarioLines", fnBody);

        // Should NOT use the old save/restore pattern
        Assert.DoesNotContain("var prev = window._truncateLines", fnBody);
        Assert.DoesNotContain("_truncateLines = prev", fnBody);
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
    // Per-container truncation: buildSourceWithNoteStates accepts
    // truncateLines parameter; buildDetailsQueue/buildHeadersQueue
    // use container._truncateLines
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BuildSourceWithNoteStates_accepts_truncateLines_parameter()
    {
        var fnBody = ExtractFunctionBody(_script, "buildSourceWithNoteStates");
        // Should have truncateLines as 5th parameter
        Assert.Matches(@"function buildSourceWithNoteStates\(origSource,\s*noteSteps,\s*noteBlocks,\s*hideHeaders,\s*truncateLines\)", _script);
        // Should use local limit instead of global
        Assert.Contains("var limit = truncateLines || window._truncateLines", fnBody);
    }

    [Fact]
    public void BuildDetailsQueue_uses_per_container_truncateLines()
    {
        var fnBody = ExtractFunctionBody(_script, "buildDetailsQueue");
        Assert.Contains("container._truncateLines", fnBody);
        Assert.Contains("isLongNote(noteBlocks[i].contentLines, containerLines)", fnBody);
    }

    [Fact]
    public void BuildHeadersQueue_uses_per_container_truncateLines()
    {
        var fnBody = ExtractFunctionBody(_script, "buildHeadersQueue");
        Assert.Contains("container._truncateLines", fnBody);
    }

    [Fact]
    public void ProcessRenderQueue_passes_container_truncateLines()
    {
        var fnBody = ExtractFunctionBody(_script, "processRenderQueue");
        Assert.Contains("container._truncateLines", fnBody);
    }

    [Fact]
    public void IsLongNote_accepts_truncateLines_parameter()
    {
        var fnBody = ExtractFunctionBody(_script, "isLongNote");
        Assert.Contains("truncateLines", fnBody);
    }

    [Fact]
    public void SetAllNotes_reads_dropdown_when_switching_to_truncated()
    {
        var fnBody = ExtractFunctionBody(_script, "_setAllNotes");
        // When switching to truncated, should read the scenario's dropdown value
        Assert.Contains(".truncate-lines-select", fnBody);
        Assert.Contains("c._truncateLines = scenarioLines", fnBody);
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
