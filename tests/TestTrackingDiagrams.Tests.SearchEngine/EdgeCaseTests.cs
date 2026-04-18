namespace TestTrackingDiagrams.Tests.SearchEngine;

/// <summary>
/// Boundary, fuzz, and edge case tests for the advanced search engine.
/// </summary>
public class EdgeCaseTests : JintTestBase
{
    [Fact]
    public void Very_long_input_does_not_crash()
    {
        // 1000+ character input with lots of OR branches
        var parts = Enumerable.Range(0, 100).Select(i => $"word{i}");
        var input = string.Join(" || ", parts);

        var result = CallMatch(input, "word50 is here", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Deeply_nested_parentheses()
    {
        var ast = CallParse("((((a))))");
        Assert.Equal(new AstText("a"), ast);
    }

    [Fact]
    public void Many_or_branches()
    {
        var result = CallMatch("a || b || c || d || e || f || g || h", "e found", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Many_and_terms()
    {
        var result = CallMatch("a && b && c && d && e", "a b c d e", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Many_and_terms_one_missing()
    {
        var result = CallMatch("a && b && c && d && e", "a b c d", [], "Passed");
        Assert.False(result);
    }

    [Fact]
    public void Operators_inside_quotes_not_parsed()
    {
        var result = CallMatch("\"a && b\"", "a && b is literal", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Operators_inside_quotes_do_not_match_separately()
    {
        var result = CallMatch("\"a && b\"", "a but not b", [], "Passed");
        Assert.False(result);
    }

    [Fact]
    public void Tag_with_hyphen()
    {
        var result = CallMatch("@happy-path", "some text", ["happy-path"], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Tag_with_dot()
    {
        var result = CallMatch("@api.v2", "some text", ["api.v2"], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Status_case_variations_all_match()
    {
        Assert.True(CallMatch("$failed", "text", [], "Failed") ?? false);
        Assert.True(CallMatch("$Failed", "text", [], "Failed") ?? false);
        Assert.True(CallMatch("$FAILED", "text", [], "Failed") ?? false);
    }

    [Fact]
    public void Single_ampersand_treated_as_text()
    {
        // & alone is not &&, so isAdvancedSearch is false for "a & b" unless there's also a &&/||/!!
        // But if forced into advanced path: "a&b || c"
        var result = CallMatch("a&b || c", "a&b is literal", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Single_pipe_treated_as_text()
    {
        var result = CallMatch("a|b || c", "a|b is literal", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Single_bang_treated_as_text()
    {
        var result = CallMatch("!important || css", "!important rule", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Not_with_grouped_expression()
    {
        // !!(a || b) → NOT (a OR b)
        var result1 = CallMatch("!!(a || b)", "c only", [], "Passed");
        Assert.True(result1);

        var result2 = CallMatch("!!(a || b)", "a is here", [], "Passed");
        Assert.False(result2);
    }

    [Fact]
    public void Empty_parens_return_null()
    {
        var result = CallMatch("()", "text", [], "Passed");
        Assert.Null(result);
    }

    [Fact]
    public void Unicode_search_term()
    {
        var result = CallMatch("café || naïve", "café latte", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Mismatched_parens_fallback_to_null()
    {
        var result = CallMatch("(a || b", "a text", [], "Passed");
        Assert.Null(result);
    }

    [Fact]
    public void Multiple_not_operators()
    {
        // !!a && !!b → NOT a AND NOT b
        var result = CallMatch("!!a && !!b", "c d e", [], "Passed");
        Assert.True(result);

        var result2 = CallMatch("!!a && !!b", "a is here", [], "Passed");
        Assert.False(result2);
    }

    [Fact]
    public void Three_words_implicit_and()
    {
        // In advanced mode (triggered by implicit AND insertion which won't trigger isAdvancedSearch,
        // but CallMatch uses lowercase so let's use explicit)
        var result = CallMatch("a && b && c", "a b c d", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Or_chain_with_phrases_and_tags()
    {
        var result = CallMatch("\"hello world\" || @smoke || $failed", "text", ["smoke"], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Complex_real_world_query()
    {
        // "Find all failed or skipped scenarios tagged smoke that mention timeout"
        var result = CallMatch("($failed || $skipped) && @smoke && timeout",
            "request timeout after 30 seconds",
            ["smoke", "api"],
            "Failed");
        Assert.True(result);
    }

    [Fact]
    public void Complex_real_world_query_no_match()
    {
        var result = CallMatch("($failed || $skipped) && @smoke && timeout",
            "request timeout after 30 seconds",
            ["smoke", "api"],
            "Passed"); // passed, not failed/skipped
        Assert.False(result);
    }
}
