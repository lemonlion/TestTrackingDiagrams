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
}
