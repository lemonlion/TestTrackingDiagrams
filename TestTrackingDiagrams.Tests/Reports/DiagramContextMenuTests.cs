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
    // svgToCanvasWithBg — canvas-level background fill
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SvgToCanvasWithBg_uses_canvas_fillRect_not_svg_clone()
    {
        // Should use canvas fillRect for background
        Assert.Contains("ctx.fillStyle = bg", _script);
        Assert.Contains("ctx.fillRect(0, 0,", _script);
    }

    [Fact]
    public void SvgToCanvasWithBg_fills_full_canvas_dimensions()
    {
        // fillRect should use canvas.width/height (which includes scale), not img.naturalWidth
        Assert.Contains("ctx.fillRect(0, 0, canvas.width, canvas.height)", _script);
    }

    [Fact]
    public void SvgToCanvasWithBg_does_not_clone_svg_or_inject_rect()
    {
        // The old approach cloned the SVG and inserted a <rect> — this should be gone
        Assert.DoesNotContain("clone.insertBefore", _script);
        Assert.DoesNotContain("cloneNode", _script);
        Assert.DoesNotContain("createElementNS", _script);
    }

    [Fact]
    public void SvgToCanvasWithBg_draws_image_after_background()
    {
        // fillRect must come before drawImage in svgToCanvasWithBg
        var fillRectIndex = _script.IndexOf("ctx.fillRect(0, 0,");
        var drawImageIndex = _script.IndexOf("ctx.drawImage(img, 0, 0)", fillRectIndex);
        Assert.True(fillRectIndex > 0);
        Assert.True(drawImageIndex > fillRectIndex, "drawImage should come after fillRect");
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
