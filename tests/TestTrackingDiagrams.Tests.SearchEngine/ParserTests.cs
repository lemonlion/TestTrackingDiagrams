namespace TestTrackingDiagrams.Tests.SearchEngine;

public class ParserTests : JintTestBase
{
    #region Simple expressions

    [Fact]
    public void Single_word_parses_to_text_node()
    {
        var ast = CallParse("word");
        Assert.Equal(new AstText("word"), ast);
    }

    [Fact]
    public void Quoted_phrase_parses_to_phrase_node()
    {
        var ast = CallParse("\"hello world\"");
        Assert.Equal(new AstPhrase("hello world"), ast);
    }

    [Fact]
    public void Tag_parses_to_tag_node()
    {
        var ast = CallParse("@smoke");
        Assert.Equal(new AstTag("smoke"), ast);
    }

    [Fact]
    public void Status_parses_to_status_node()
    {
        var ast = CallParse("$failed");
        Assert.Equal(new AstStatus("failed"), ast);
    }

    #endregion

    #region Binary operators

    [Fact]
    public void A_and_b_parses_to_and_node()
    {
        var ast = CallParse("a && b");
        Assert.Equal(new AstAnd(new AstText("a"), new AstText("b")), ast);
    }

    [Fact]
    public void A_or_b_parses_to_or_node()
    {
        var ast = CallParse("a || b");
        Assert.Equal(new AstOr(new AstText("a"), new AstText("b")), ast);
    }

    #endregion

    #region NOT operator

    [Fact]
    public void Not_a_parses_to_not_node()
    {
        var ast = CallParse("!!a");
        Assert.Equal(new AstNot(new AstText("a")), ast);
    }

    [Fact]
    public void Not_tag_parses_to_not_tag_node()
    {
        var ast = CallParse("!!@smoke");
        Assert.Equal(new AstNot(new AstTag("smoke")), ast);
    }

    [Fact]
    public void Not_status_parses_to_not_status_node()
    {
        var ast = CallParse("!!$failed");
        Assert.Equal(new AstNot(new AstStatus("failed")), ast);
    }

    #endregion

    #region Precedence

    [Fact]
    public void Or_has_lower_precedence_than_and()
    {
        // a || b && c → or(a, and(b, c))
        var ast = CallParse("a || b && c");
        Assert.Equal(
            new AstOr(new AstText("a"), new AstAnd(new AstText("b"), new AstText("c"))),
            ast);
    }

    [Fact]
    public void And_chains_left_to_right_then_or()
    {
        // a && b || c → or(and(a, b), c)
        var ast = CallParse("a && b || c");
        Assert.Equal(
            new AstOr(new AstAnd(new AstText("a"), new AstText("b")), new AstText("c")),
            ast);
    }

    [Fact]
    public void Not_binds_tighter_than_and()
    {
        // !!a && b → and(not(a), b)
        var ast = CallParse("!!a && b");
        Assert.Equal(
            new AstAnd(new AstNot(new AstText("a")), new AstText("b")),
            ast);
    }

    [Fact]
    public void Not_binds_tighter_than_or()
    {
        // !!a || b → or(not(a), b)
        var ast = CallParse("!!a || b");
        Assert.Equal(
            new AstOr(new AstNot(new AstText("a")), new AstText("b")),
            ast);
    }

    #endregion

    #region Grouping

    [Fact]
    public void Parentheses_override_precedence()
    {
        // (a || b) && c → and(or(a, b), c)
        var ast = CallParse("(a || b) && c");
        Assert.Equal(
            new AstAnd(new AstOr(new AstText("a"), new AstText("b")), new AstText("c")),
            ast);
    }

    [Fact]
    public void Right_grouped_or()
    {
        // a && (b || c) → and(a, or(b, c))
        var ast = CallParse("a && (b || c)");
        Assert.Equal(
            new AstAnd(new AstText("a"), new AstOr(new AstText("b"), new AstText("c"))),
            ast);
    }

    [Fact]
    public void Two_grouped_ands_or()
    {
        // (a && b) || (c && d)
        var ast = CallParse("(a && b) || (c && d)");
        Assert.Equal(
            new AstOr(
                new AstAnd(new AstText("a"), new AstText("b")),
                new AstAnd(new AstText("c"), new AstText("d"))),
            ast);
    }

    [Fact]
    public void Double_nested_parens()
    {
        // ((a || b)) → or(a, b)
        var ast = CallParse("((a || b))");
        Assert.Equal(new AstOr(new AstText("a"), new AstText("b")), ast);
    }

    [Fact]
    public void Nested_group_inside_or()
    {
        // (a || (b && c)) → or(a, and(b, c))
        var ast = CallParse("(a || (b && c))");
        Assert.Equal(
            new AstOr(new AstText("a"), new AstAnd(new AstText("b"), new AstText("c"))),
            ast);
    }

    #endregion

    #region Complex mixed expressions

    [Fact]
    public void Tag_and_phrase_or_text()
    {
        // @smoke && "error message" || timeout → or(and(tag, phrase), text)
        var ast = CallParse("@smoke && \"error message\" || timeout");
        Assert.Equal(
            new AstOr(
                new AstAnd(new AstTag("smoke"), new AstPhrase("error message")),
                new AstText("timeout")),
            ast);
    }

    [Fact]
    public void Grouped_statuses_and_tag()
    {
        // ($failed || $skipped) && @regression
        var ast = CallParse("($failed || $skipped) && @regression");
        Assert.Equal(
            new AstAnd(
                new AstOr(new AstStatus("failed"), new AstStatus("skipped")),
                new AstTag("regression")),
            ast);
    }

    [Fact]
    public void Three_way_and_with_not_and_group()
    {
        // !!@slow && (@smoke || @regression) && timeout
        var ast = CallParse("!!@slow && (@smoke || @regression) && timeout");
        Assert.Equal(
            new AstAnd(
                new AstAnd(
                    new AstNot(new AstTag("slow")),
                    new AstOr(new AstTag("smoke"), new AstTag("regression"))),
                new AstText("timeout")),
            ast);
    }

    [Fact]
    public void Implicit_and_between_words_with_explicit_operators()
    {
        // login success && @smoke → and(and(login, success), @smoke) via implicit AND
        var ast = CallParse("login success && @smoke");
        Assert.Equal(
            new AstAnd(
                new AstAnd(new AstText("login"), new AstText("success")),
                new AstTag("smoke")),
            ast);
    }

    #endregion

    #region Error recovery (returns null)

    [Fact]
    public void Mismatched_open_paren_returns_null()
    {
        var ast = CallParse("(a || b");
        Assert.Null(ast);
    }

    [Fact]
    public void Mismatched_close_paren_returns_null()
    {
        var ast = CallParse("a || b)");
        Assert.Null(ast);
    }

    [Fact]
    public void Dangling_and_at_end_returns_null()
    {
        var ast = CallParse("a &&");
        Assert.Null(ast);
    }

    [Fact]
    public void Dangling_or_at_start_returns_null()
    {
        var ast = CallParse("|| a");
        Assert.Null(ast);
    }

    [Fact]
    public void Adjacent_operators_returns_null()
    {
        var ast = CallParse("a && || b");
        Assert.Null(ast);
    }

    [Fact]
    public void Empty_parens_returns_null()
    {
        var ast = CallParse("()");
        Assert.Null(ast);
    }

    [Fact]
    public void Empty_input_returns_null()
    {
        var ast = CallParse("");
        Assert.Null(ast);
    }

    #endregion

    #region Not with grouping

    [Fact]
    public void Not_grouped_expression()
    {
        // !!(a || b) → not(or(a, b))
        var ast = CallParse("!!(a || b)");
        Assert.Equal(
            new AstNot(new AstOr(new AstText("a"), new AstText("b"))),
            ast);
    }

    #endregion
}
