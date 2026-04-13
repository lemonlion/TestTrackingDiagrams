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
    public void Globals_truncateLines_defaults_to_40()
    {
        Assert.Contains("window._truncateLines = 40", _notesScript);
    }

    [Fact]
    public void Globals_detailsDefault_defaults_to_truncated()
    {
        Assert.Contains("window._detailsDefault = 'truncated'", _notesScript);
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
        // The ▴ block uses offset: topSize * 2 + pad * 2
        Assert.Contains("bbox.x + bbox.width - topSize * 2 - pad * 2", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_double_click_cycles_state()
    {
        var funcBody = GetFunction("createNoteButtons");
        // dblclick on note background paths cycles state
        Assert.Contains("dblclick", funcBody);
        Assert.Contains("onCycle", funcBody);
        // Uses note group paths for hover detection and dblclick
        Assert.Contains("grp.paths.forEach", funcBody);
        Assert.Contains("grp.texts.forEach", funcBody);
        // Delayed hide for smooth hover across gaps between note sub-elements
        Assert.Contains("_noteScheduleHide", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_accepts_onTruncate_and_onCycle_parameters()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Function signature includes onTruncate, onCycle, and grp parameters
        Assert.Contains("onExpand, onContract, onTruncate, onCycle, contentLines, grp", funcBody);
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

    [Fact]
    public void CreateNoteButtons_minus_button_uses_svg_line()
    {
        var funcBody = GetFunction("createNoteButtons");
        // The minus button uses an SVG line element instead of text
        Assert.Contains("createElementNS(SVGNS, 'line')", funcBody);
        Assert.Contains("stroke-linecap", funcBody);
    }

    // ─── createNoteButtons — hover detection rect ───────────

    [Fact]
    public void CreateNoteButtons_adds_transparent_hover_rect()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Transparent rect covers the note bounding box for reliable hover detection
        Assert.Contains("note-hover-rect", funcBody);
        Assert.Contains("fill', 'transparent'", funcBody);
        Assert.Contains("stroke', 'none'", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_hover_rect_uses_note_bbox()
    {
        var funcBody = GetFunction("createNoteButtons");
        // hoverRect positioned using the note's bounding box
        Assert.Contains("hoverRect.setAttribute('x', bbox.x)", funcBody);
        Assert.Contains("hoverRect.setAttribute('y', bbox.y)", funcBody);
        Assert.Contains("hoverRect.setAttribute('width', bbox.width)", funcBody);
        Assert.Contains("hoverRect.setAttribute('height', bbox.height)", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_hover_rect_captures_all_pointer_events()
    {
        var funcBody = GetFunction("createNoteButtons");
        // pointer-events: all ensures the rect captures hover even with overlapping SVG elements
        Assert.Contains("hoverRect.style.pointerEvents = 'all'", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_hover_rect_has_mouseenter_and_mouseleave()
    {
        var funcBody = GetFunction("createNoteButtons");
        // hoverRect wired to the same show/hide functions as path/text elements
        Assert.Contains("hoverRect.addEventListener('mouseenter', _noteShowButtons)", funcBody);
        Assert.Contains("hoverRect.addEventListener('mouseleave', _noteScheduleHide)", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_hover_rect_supports_dblclick_cycle()
    {
        var funcBody = GetFunction("createNoteButtons");
        // hoverRect also handles dblclick for toggle-cycling
        Assert.Contains("hoverRect.addEventListener('dblclick'", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_hover_rect_inserted_inside_mainG_before_note_paths()
    {
        var funcBody = GetFunction("createNoteButtons");
        // hoverRect must be inserted inside mainG before the note's first path element.
        // This keeps text elements (which follow paths in SVG order) on top of the
        // hoverRect, allowing native text selection. If the hoverRect were appended
        // to the SVG root it would sit above mainG and block all pointer events to text.
        Assert.Contains("grp.paths[0].parentNode.insertBefore(hoverRect, grp.paths[0])", funcBody);
        Assert.DoesNotContain("svg.appendChild(hoverRect)", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_buttons_appended_to_svg_root()
    {
        var funcBody = GetFunction("createNoteButtons");
        // Buttons must be appended to the SVG root so they render on top of everything
        Assert.Contains("svg.appendChild(b)", funcBody);
    }

    [Fact]
    public void CreateNoteButtons_hide_timeout_is_300ms()
    {
        var funcBody = GetFunction("createNoteButtons");
        // 300ms delay gives enough time for mouse to cross element boundaries
        Assert.Contains("}, 300)", funcBody);
        Assert.DoesNotContain("}, 100)", funcBody);
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
    public void SetScenarioTruncateLines_does_not_sync_all_dropdowns()
    {
        var funcBody = GetFunction("_setScenarioTruncateLines");
        // Scenario-level should NOT sync all dropdowns — only report-level does
        Assert.DoesNotContain(".truncate-lines-select", funcBody);
    }

    [Fact]
    public void SetScenarioTruncateLines_restores_global_truncate_lines()
    {
        var funcBody = GetFunction("_setScenarioTruncateLines");
        // Should save and restore window._truncateLines after rendering
        Assert.Contains("var prev = window._truncateLines", funcBody);
        Assert.Contains("window._truncateLines = prev", funcBody);
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

    [Fact]
    public void CollapsibleNotesStyles_note_toggle_icon_user_select_none()
    {
        var css = DiagramContextMenu.GetCollapsibleNotesStyles();
        Assert.Contains(".note-toggle-icon", css);
        Assert.Contains("user-select: none", css);
    }

    // ─── getNotePreview — empty note handling ───────────────

    [Fact]
    public void GetNotePreview_returns_empty_string_for_empty_content()
    {
        var funcBody = GetFunction("getNotePreview");
        // When all content lines are empty (or only gray), the preview should be
        // empty — not '...' — so collapsed notes aren't bigger than expanded ones
        Assert.Contains("if (!raw) return '';", funcBody);
        Assert.DoesNotContain("if (!raw) return '...';", funcBody);
    }

    [Fact]
    public void BuildSourceWithNoteStates_skips_empty_preview_for_collapsed_notes()
    {
        var funcBody = GetFunction("buildSourceWithNoteStates");
        // When a collapsed note has an empty preview, don't push it into the output
        // (avoids an empty line between "note left" and "end note")
        Assert.Contains("if (preview)", funcBody);
    }

    // ═══════════════════════════════════════════════════════════
    // Context menu styles — submenu hover uses direct child selectors
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ContextMenu_styles_use_direct_child_selectors_for_hover()
    {
        var css = DiagramContextMenu.GetStyles();
        // Must use > div:hover to avoid highlighting the submenu container itself
        Assert.Contains("> div:hover", css);
        Assert.DoesNotContain(".diagram-ctx-menu div:hover", css);
    }

    [Fact]
    public void ContextMenu_submenu_styles_use_direct_child_selectors_for_hover()
    {
        var css = DiagramContextMenu.GetStyles();
        Assert.Contains(".submenu > div:hover", css);
        Assert.DoesNotContain(".submenu div:hover", css);
    }

    // ═══════════════════════════════════════════════════════════
    // Inline SVG styles — floating zoom button visibility
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ZoomButton_hidden_by_default_shown_on_container_hover()
    {
        var css = DiagramContextMenu.GetInlineSvgStyles();
        // Button should be invisible by default
        Assert.Contains("opacity: 0", css);
        Assert.Contains("pointer-events: none", css);
        // Container hover reveals the button
        Assert.Contains("[data-diagram-type]:hover > .diagram-zoom-toggle", css);
    }

    // ═══════════════════════════════════════════════════════════
    // Zoom toggle — drag-to-pan and max-height
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ZoomToggle_sets_overflow_auto_and_max_height_when_zoomed()
    {
        Assert.Contains("container.style.overflow = 'auto'", _script);
        Assert.Contains("container.style.maxHeight = '80vh'", _script);
        Assert.Contains("container.style.cursor = 'grab'", _script);
    }

    [Fact]
    public void ZoomToggle_clears_overflow_and_max_height_when_unzoomed()
    {
        Assert.Contains("container.style.overflow = ''", _script);
        Assert.Contains("container.style.maxHeight = ''", _script);
        Assert.Contains("container.style.cursor = ''", _script);
    }

    [Fact]
    public void ZoomToggle_has_drag_to_pan_handlers()
    {
        // Must have mousedown/mousemove/mouseup for drag panning
        Assert.Contains("cursor = 'grabbing'", _script);
        Assert.Contains("scrollLeft = scrollL - (e.pageX - startX)", _script);
        Assert.Contains("scrollTop = scrollT - (e.pageY - startY)", _script);
    }

    [Fact]
    public void ZoomButton_is_prepended_not_appended()
    {
        Assert.Contains("container.prepend(btn)", _script);
        Assert.DoesNotContain("container.appendChild(btn)", _script);
    }

    [Fact]
    public void ZoomObserver_setup_deferred_to_DOMContentLoaded()
    {
        // The MutationObserver for zoom buttons must be set up inside the
        // DOMContentLoaded callback (or when readyState is not 'loading').
        // When the script runs in <head>, document.body is null and
        // observe(null, ...) silently fails, so lazily-rendered BrowserJs
        // diagrams never get their zoom buttons added.
        //
        // The fix wraps the observer setup inside the same DOMContentLoaded
        // handler that calls addZoomButtons(), using a named function.
        Assert.Contains("function initZoom()", _script);
        Assert.Contains("addEventListener('DOMContentLoaded', initZoom)", _script);
    }

    // ═══════════════════════════════════════════════════════════
    // Open in new tab — uses blob URLs not data URIs
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void OpenInNewTab_uses_blob_url_not_data_uri()
    {
        // PNG open should use createObjectURL, not FileReader + readAsDataURL
        Assert.DoesNotContain("readAsDataURL", _script);
        // SVG "Open in new tab" should not use window.open('data:image/svg+xml...')
        // (data:image/svg+xml;base64 still exists in svgToCanvas for img.src, which is fine)
        Assert.DoesNotContain("window.open('data:image/svg+xml", _script);
    }

    [Fact]
    public void OpenInNewTab_uses_createObjectURL_for_images()
    {
        // The "Open image in new tab" submenu items should use URL.createObjectURL
        Assert.Contains("window.open(URL.createObjectURL(blob))", _script);
    }
}
