using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class DiagramContextMenuTests
{
    private readonly string _script = DiagramContextMenu.GetContextMenuScript();

    // ═══════════════════════════════════════════════════════════
    // getBackgroundColor — SVG inline style detection
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GetBackgroundColor_checks_svg_inline_style_for_background()
    {
        Assert.Contains("svg.getAttribute('style')", _script);
        Assert.Contains("/background\\s*:\\s*([^;]+)/", _script);
    }

    [Fact]
    public void GetBackgroundColor_checks_computed_style()
    {
        Assert.Contains("getComputedStyle(svg).backgroundColor", _script);
    }

    [Fact]
    public void GetBackgroundColor_falls_back_to_rect_fill()
    {
        Assert.Contains("svg.querySelectorAll('rect')", _script);
        Assert.Contains("rect.getAttribute('fill')", _script);
    }

    [Fact]
    public void GetBackgroundColor_skips_rects_with_zero_fill_opacity()
    {
        Assert.Contains("fill-opacity", _script);
        Assert.Contains("parseFloat(fo) === 0", _script);
    }

    [Fact]
    public void GetBackgroundColor_skips_rects_with_8digit_hex_zero_alpha()
    {
        // plantuml-js uses fill="#00000000" (8-digit hex, alpha=00)
        Assert.Contains("fill.slice(7)", _script);
        Assert.Contains("#[0-9a-fA-F]{8}", _script);
    }

    [Fact]
    public void GetBackgroundColor_defaults_to_white()
    {
        Assert.Contains("return '#ffffff'", _script);
    }

    // ═══════════════════════════════════════════════════════════
    // svgToCanvasWithBg — SVG clone + background rect injection
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SvgToCanvasWithBg_clones_svg_and_injects_background_rect()
    {
        // Should clone the SVG and inject a background rect
        Assert.Contains("svg.cloneNode(true)", _script);
        Assert.Contains("createElementNS", _script);
        Assert.Contains("clone.insertBefore(bgRect, clone.firstChild)", _script);
    }

    [Fact]
    public void SvgToCanvasWithBg_reads_viewBox_for_rect_dimensions()
    {
        // Should read viewBox to get rect dimensions
        Assert.Contains("clone.getAttribute('viewBox')", _script);
        Assert.Contains("bgRect.setAttribute('width', bw)", _script);
        Assert.Contains("bgRect.setAttribute('height', bh)", _script);
    }

    [Fact]
    public void SvgToCanvasWithBg_does_not_use_canvas_fillRect()
    {
        // The new approach uses SVG rect injection, not canvas fillRect for background
        var funcStart = _script.IndexOf("function svgToCanvasWithBg(");
        var funcEnd = _script.IndexOf("function ", funcStart + 1);
        var funcBody = _script.Substring(funcStart, funcEnd - funcStart);
        Assert.DoesNotContain("ctx.fillStyle", funcBody);
        Assert.DoesNotContain("ctx.fillRect", funcBody);
    }

    [Fact]
    public void SvgToCanvasWithBg_serializes_clone_not_original()
    {
        // Should serialize the clone (with injected rect), not the original SVG
        var funcStart = _script.IndexOf("function svgToCanvasWithBg(");
        var funcEnd = _script.IndexOf("function ", funcStart + 1);
        var funcBody = _script.Substring(funcStart, funcEnd - funcStart);
        Assert.Contains("serializeSvg(clone)", funcBody);
    }

    // ═══════════════════════════════════════════════════════════
    // svgToCanvas (transparent) — unchanged, no background fill
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SvgToCanvas_does_not_fill_background()
    {
        // The transparent variant should NOT have fillRect
        var svgToCanvasStart = _script.IndexOf("function svgToCanvas(");
        var svgToCanvasEnd = _script.IndexOf("function svgToCanvasWithBg(");
        var transparentSection = _script[svgToCanvasStart..svgToCanvasEnd];
        Assert.DoesNotContain("fillRect", transparentSection);
        Assert.DoesNotContain("fillStyle", transparentSection);
    }

    // ═══════════════════════════════════════════════════════════
    // Collapsible notes script
    // ═══════════════════════════════════════════════════════════

    private readonly string _notesScript = DiagramContextMenu.GetCollapsibleNotesScript();

    private string GetFunction(string name, string? source = null)
    {
        source ??= _notesScript;
        var start = source.IndexOf($"function {name}(");
        if (start < 0) start = source.IndexOf($"window.{name} = function(");
        Assert.True(start >= 0, $"Could not find function {name}");
        // Find the next top-level function or window. assignment after this one
        var searchFrom = start + 10;
        var funcEnd = source.IndexOf("\n    function ", searchFrom);
        var windowEnd = source.IndexOf("\n    window.", searchFrom);
        var ends = new[] { funcEnd, windowEnd }.Where(x => x > 0).ToArray();
        var end = ends.Length > 0 ? ends.Min() : source.Length;
        return source[start..end];
    }

    // ─── buildSourceWithNoteStates ───────────────────────────

    [Fact]
    public void BuildSourceWithNoteStates_expanded_with_hideHeaders_removes_color_gray_lines()
    {
        var funcBody = GetFunction("buildSourceWithNoteStates");
        // When hideHeaders is true and note is in normal (expanded) mode,
        // lines matching $color(gray) should be skipped (setting justSkippedGray)
        Assert.Contains(@"if (inNote && hideHeaders && /^\$color\(gray\)/.test(trimmed)) { justSkippedGray = true; continue; }", funcBody);
    }

    [Fact]
    public void BuildSourceWithNoteStates_expanded_without_hideHeaders_preserves_all_lines()
    {
        var funcBody = GetFunction("buildSourceWithNoteStates");
        // The $color(gray) skip only happens when hideHeaders is true
        // The normal mode path should only skip when hideHeaders is true
        Assert.Contains("hideHeaders", funcBody);
        // Should NOT unconditionally strip $color(gray) lines
        Assert.DoesNotContain("continue; // always strip gray", funcBody);
    }

    [Fact]
    public void BuildSourceWithNoteStates_uses_noteSteps_for_state_not_hideHeaders()
    {
        var funcBody = GetFunction("buildSourceWithNoteStates");
        // Note step determines collapsed/truncated/expanded — not hideHeaders
        Assert.Contains("var step = noteSteps[nIdx]", funcBody);
        Assert.Contains("noteStepState(step)", funcBody);
    }

    [Fact]
    public void BuildSourceWithNoteStates_truncated_mode_also_respects_hideHeaders()
    {
        var funcBody = GetFunction("buildSourceWithNoteStates");
        // In truncated mode, $color(gray) lines should be skippable when hideHeaders is true
        Assert.Contains(@"if (hideHeaders && /^\$color\(gray\)/.test(trimmed)) { justSkippedGray = true; continue; }", funcBody);
    }

    // ─── _preProcessSource ──────────────────────────────────

    [Fact]
    public void PreProcessSource_always_initializes_noteSteps()
    {
        // _preProcessSource must always set _noteSteps for every note,
        // even when detailsDefault is 'expanded'. Without this, buildHeadersQueue
        // would use undefined steps → noteSteps[i] || 0 → 0 (collapsed),
        // causing headers toggle to collapse all notes.
        var funcBody = GetFunction("_preProcessSource");
        Assert.Contains("el._noteSteps", funcBody);
        // Step initialization must happen BEFORE the conditional source transformation,
        // not inside it. Verify _noteSteps assignment is outside the if block.
        var stepsInit = funcBody.IndexOf("el._noteSteps[i] = targetStep");
        var conditional = funcBody.IndexOf("if (state !== 'expanded'");
        Assert.True(stepsInit >= 0, "Should set el._noteSteps[i] = targetStep");
        Assert.True(conditional >= 0, "Should have conditional for source transformation");
        Assert.True(stepsInit < conditional,
            "_noteSteps initialization must happen BEFORE the conditional source transformation");
    }

    [Fact]
    public void PreProcessSource_sets_expanded_step_to_2()
    {
        var funcBody = GetFunction("_preProcessSource");
        // Step 2 = expanded in noteStepState
        Assert.Contains("targetStep = 2", funcBody);
    }

    [Fact]
    public void PreProcessSource_stores_noteOriginalSource()
    {
        var funcBody = GetFunction("_preProcessSource");
        Assert.Contains("el._noteOriginalSource = source", funcBody);
    }

    // ─── buildHeadersQueue ──────────────────────────────────

    [Fact]
    public void BuildHeadersQueue_sets_headersHidden_on_container()
    {
        var funcBody = GetFunction("buildHeadersQueue");
        Assert.Contains("container._headersHidden = hiding", funcBody);
    }

    [Fact]
    public void BuildHeadersQueue_does_not_modify_noteSteps()
    {
        var funcBody = GetFunction("buildHeadersQueue");
        // buildHeadersQueue should NOT change noteSteps — it only changes header visibility
        Assert.DoesNotContain("container._noteSteps[", funcBody);
        Assert.DoesNotContain("_noteSteps =", funcBody.Replace("if (!container._noteSteps) container._noteSteps = {};", ""));
    }

    [Fact]
    public void BuildHeadersQueue_skips_container_when_state_unchanged()
    {
        var funcBody = GetFunction("buildHeadersQueue");
        // Should skip containers where the hiding state hasn't changed
        Assert.Contains("if (wasHidden === hiding) return", funcBody);
    }

    // ─── buildDetailsQueue ──────────────────────────────────

    [Fact]
    public void BuildDetailsQueue_does_not_change_headersHidden()
    {
        var funcBody = GetFunction("buildDetailsQueue");
        // Details queue should NOT change _headersHidden — it only changes note steps
        Assert.DoesNotContain("_headersHidden", funcBody);
    }

    [Fact]
    public void BuildDetailsQueue_sets_expanded_step_2_truncated_step_1_collapsed_step_0()
    {
        var funcBody = GetFunction("buildDetailsQueue");
        Assert.Contains("targetStep = 2", funcBody); // expanded
        Assert.Contains("targetStep = 0", funcBody); // collapsed
        Assert.Contains("isLongNote", funcBody); // truncated uses isLongNote check
    }

    // ─── _setReportDetails ──────────────────────────────────

    [Fact]
    public void SetReportDetails_stores_detailsDefault()
    {
        var funcBody = GetFunction("_setReportDetails");
        Assert.Contains("window._detailsDefault = targetState", funcBody);
    }

    // ─── _setReportHeaders ──────────────────────────────────

    [Fact]
    public void SetReportHeaders_stores_headersHidden()
    {
        var funcBody = GetFunction("_setReportHeaders");
        Assert.Contains("window._headersHidden = hiding", funcBody);
    }

    [Fact]
    public void SetReportHeaders_does_not_modify_detailsDefault()
    {
        var funcBody = GetFunction("_setReportHeaders");
        Assert.DoesNotContain("_detailsDefault", funcBody);
    }

    // ─── noteStepState ──────────────────────────────────────

    [Fact]
    public void NoteStepState_maps_0_to_collapsed_2_to_expanded()
    {
        var funcBody = GetFunction("noteStepState");
        Assert.Contains("if (step === 0) return 'collapsed'", funcBody);
        Assert.Contains("if (step === 2) return 'expanded'", funcBody);
        Assert.Contains("return 'truncated'", funcBody);
    }

    // ─── processRenderQueue ─────────────────────────────────

    [Fact]
    public void ProcessRenderQueue_passes_headersHidden_to_buildSourceWithNoteStates()
    {
        var funcBody = GetFunction("processRenderQueue");
        Assert.Contains("container._headersHidden", funcBody);
    }

    // ─── syncRadioButtons ───────────────────────────────────

    [Fact]
    public void SyncRadioButtons_scopes_to_data_state_only()
    {
        var funcBody = GetFunction("syncRadioButtons");
        // Must only target buttons with data-state attribute, not headers buttons (data-hstate)
        Assert.Contains("[data-state]", funcBody);
    }

    // ─── Globals ────────────────────────────────────────────

    [Fact]
    public void Globals_headersHidden_defaults_to_false()
    {
        Assert.Contains("window._headersHidden = false", _notesScript);
    }

    [Fact]
    public void Globals_truncateLines_defaults_to_20()
    {
        Assert.Contains("window._truncateLines = 20", _notesScript);
    }

    [Fact]
    public void Globals_detailsDefault_defaults_to_expanded()
    {
        Assert.Contains("window._detailsDefault = 'expanded'", _notesScript);
    }

    // ─── createNoteButtons ──────────────────────────────────

    [Fact]
    public void CreateNoteButtons_expand_button_is_3x_width()
    {
        var funcBody = GetFunction("createNoteButtons");
        Assert.Contains("size * 3", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_shows_contract_for_expanded_and_truncated()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Contract button shown for expanded or truncated
        Assert.Contains("state === 'expanded' || state === 'truncated'", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_shows_expand_for_collapsed_and_truncated()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Expand button shown for collapsed or truncated
        Assert.Contains("state === 'collapsed' || state === 'truncated'", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_all_buttons_are_hover_only()
    {
        var funcBody = GetFunction("createNoteButtons");
        // All buttons start with opacity 0 (hover only)
        Assert.DoesNotContain("opacity = '0.6'", funcBody);
        Assert.Contains("opacity = '0'", funcBody);
    }

    // ─── createNoteButtons — arrows and double-click ────────

    [Fact]
    public void CreateNoteButtons_expand_uses_large_downward_arrow()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Bottom expand button uses ▼ (large downward triangle)
        Assert.Contains("\\u25BC", funcBody);
        Assert.DoesNotContain("\\u25BE", funcBody); // not small ▾
        Assert.DoesNotContain("textContent = '+'", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_bottom_contract_uses_large_upward_arrow()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Bottom-center ▲ contract button shown when expanded and long note
        Assert.Contains("state === 'expanded' && longNote", funcBody);
        Assert.Contains("\\u25B2", funcBody); // ▲ large
    }

    [Fact]
    public void CreateNoteButtons_top_right_arrow_for_expanded_long_notes()
    {
        var funcBody = GetFunction("createNoteButtons");
        // For expanded long notes: ▴ arrow appears to the left of −
        // The ▴ block uses offset: size * 2 + pad * 2
        Assert.Contains("bbox.x + bbox.width - size * 2 - pad * 2", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_double_click_cycles_state()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Hover rect has dblclick handler
        Assert.Contains("dblclick", funcBody);
        Assert.Contains("onCycle", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_accepts_onTruncate_and_onCycle_parameters()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Function signature includes onTruncate and onCycle parameters
        Assert.Contains("onExpand, onContract, onTruncate, onCycle, contentLines", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_top_right_arrow_calls_onTruncate()
    {
        var funcBody = GetFunction("createNoteButtons");
        // The top-right ▴ arrow on expanded long notes calls onTruncate, not onContract
        // Find the block for the ▴ arrow (uses bgA click handler)
        Assert.Contains("onTruncate()", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_bottom_contract_calls_onTruncate()
    {
        var funcBody = GetFunction("createNoteButtons");
        // The bottom ▲ on expanded long notes calls onTruncate, not onContract
        // bgBC click handler should call onTruncate
        var bottomBlock = funcBody.Substring(funcBody.IndexOf("\\u25B2"));
        Assert.Contains("onTruncate()", bottomBlock);
    }

    [Fact]
    public void CreateNoteButtons_minus_button_calls_onContract()
    {
        var funcBody = GetFunction("createNoteButtons");
        // The − (minus) button calls onContract for full collapse
        Assert.Contains("onContract()", funcBody);
    }

    // ─── makeNotesCollapsible — double-click cycle logic ────

    [Fact]
    public void MakeNotesCollapsible_passes_cycle_callback()
    {
        var funcBody = GetFunction("makeNotesCollapsible");
        // The cycle callback calculates next step based on current step
        Assert.Contains("curStep === 2", funcBody);
        Assert.Contains("curStep === 1", funcBody);
        Assert.Contains("long ? 1 : 0", funcBody);
    }

    [Fact]
    public void MakeNotesCollapsible_passes_onTruncate_to_state_1()
    {
        var funcBody = GetFunction("makeNotesCollapsible");
        // onTruncate callback sets state to 1 (truncated)
        Assert.Contains("setNoteState(container, idx, 1)", funcBody);
    }

    // ─── buildSourceWithNoteStates — trailing space fix ─────

    [Fact]
    public void BuildSourceWithNoteStates_skips_blank_lines_after_hidden_headers()
    {
        var funcBody = GetFunction("buildSourceWithNoteStates");
        // justSkippedGray tracking removes blank lines left after hiding $color(gray) lines
        Assert.Contains("justSkippedGray", funcBody);
        Assert.Contains("justSkippedGray && trimmed === ''", funcBody);
    }

    // ─── setNoteState — performance ─────────────────────────

    [Fact]
    public void SetNoteState_short_circuits_unchanged_step()
    {
        var funcBody = GetFunction("setNoteState");
        // Should return early if step hasn't changed
        Assert.Contains("container._noteSteps[noteIdx] === targetStep", funcBody);
    }

    [Fact]
    public void SetNoteState_uses_svg_cache()
    {
        var funcBody = GetFunction("setNoteState");
        Assert.Contains("_svgCache[newSource]", funcBody);
    }

    [Fact]
    public void SetNoteState_caches_rendered_svg()
    {
        var funcBody = GetFunction("setNoteState");
        Assert.Contains("_svgCache[newSource] = container.innerHTML", funcBody);
    }

    // ─── processRenderQueue — performance ───────────────────

    [Fact]
    public void ProcessRenderQueue_uses_svg_cache()
    {
        var funcBody = GetFunction("processRenderQueue");
        Assert.Contains("_svgCache[newSource]", funcBody);
    }

    [Fact]
    public void ProcessRenderQueue_caches_rendered_svg()
    {
        var funcBody = GetFunction("processRenderQueue");
        Assert.Contains("_svgCache[newSource] = container.innerHTML", funcBody);
    }

    // ─── buildDetailsQueue — force parameter ────────────────

    [Fact]
    public void BuildDetailsQueue_accepts_force_parameter()
    {
        var funcBody = GetFunction("buildDetailsQueue");
        Assert.Contains("force", funcBody);
        Assert.Contains("needsUpdate || force", funcBody);
    }

    // ─── _setTruncateLines ──────────────────────────────────

    [Fact]
    public void SetTruncateLines_passes_force_true()
    {
        var funcBody = GetFunction("_setTruncateLines");
        Assert.Contains("'truncated', true", funcBody);
    }

    // ─── _setScenarioTruncateLines ──────────────────────────

    [Fact]
    public void SetScenarioTruncateLines_exists_and_scopes_to_scenario()
    {
        var funcBody = GetFunction("_setScenarioTruncateLines");
        Assert.Contains("sel.closest('details.scenario')", funcBody);
        Assert.Contains("window._truncateLines", funcBody);
    }

    [Fact]
    public void SetScenarioTruncateLines_syncs_all_dropdowns()
    {
        var funcBody = GetFunction("_setScenarioTruncateLines");
        Assert.Contains(".truncate-lines-select", funcBody);
    }

    [Fact]
    public void SetScenarioTruncateLines_passes_force_true()
    {
        var funcBody = GetFunction("_setScenarioTruncateLines");
        Assert.Contains("'truncated', true", funcBody);
    }

    // ─── SVG cache global ───────────────────────────────────

    [Fact]
    public void SvgCache_is_declared()
    {
        Assert.Contains("var _svgCache = {}", _notesScript);
    }

    // ─── GetCollapsibleNotesStyles ──────────────────────────

    [Fact]
    public void CollapsibleNotesStyles_contains_radio_button_css()
    {
        var css = DiagramContextMenu.GetCollapsibleNotesStyles();
        Assert.Contains(".details-radio-btn", css);
        Assert.Contains(".details-active", css);
        Assert.Contains(".headers-radio", css);
        Assert.Contains(".truncate-lines-select", css);
    }
}
