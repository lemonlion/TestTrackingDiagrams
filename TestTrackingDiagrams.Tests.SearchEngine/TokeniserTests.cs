namespace TestTrackingDiagrams.Tests.SearchEngine;

public class TokeniserTests : JintTestBase
{
    #region Basic tokenisation

    [Fact]
    public void Empty_string_returns_empty_token_list()
    {
        var tokens = CallTokenise("");
        Assert.Empty(tokens);
    }

    [Fact]
    public void Whitespace_only_returns_empty_token_list()
    {
        var tokens = CallTokenise("   ");
        Assert.Empty(tokens);
    }

    [Fact]
    public void Single_word_returns_single_text_token()
    {
        var tokens = CallTokenise("chocolate");
        Assert.Single(tokens);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("chocolate", tokens[0].Value);
    }

    [Fact]
    public void Multiple_words_return_text_tokens_with_implicit_AND()
    {
        var tokens = CallTokenise("chocolate cake");
        Assert.Equal(3, tokens.Count); // text AND text
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("chocolate", tokens[0].Value);
        Assert.Equal("and", tokens[1].Type);
        Assert.Equal("text", tokens[2].Type);
        Assert.Equal("cake", tokens[2].Value);
    }

    [Fact]
    public void Quoted_phrase_returns_phrase_token()
    {
        var tokens = CallTokenise("\"chocolate cake\"");
        Assert.Single(tokens);
        Assert.Equal("phrase", tokens[0].Type);
        Assert.Equal("chocolate cake", tokens[0].Value);
    }

    [Fact]
    public void Mixed_quotes_and_words_returns_correct_tokens()
    {
        var tokens = CallTokenise("\"chocolate cake\" vanilla");
        // phrase AND text
        Assert.Equal(3, tokens.Count);
        Assert.Equal("phrase", tokens[0].Type);
        Assert.Equal("chocolate cake", tokens[0].Value);
        Assert.Equal("and", tokens[1].Type);
        Assert.Equal("text", tokens[2].Type);
        Assert.Equal("vanilla", tokens[2].Value);
    }

    [Fact]
    public void Tag_returns_tag_token()
    {
        var tokens = CallTokenise("@smoke");
        Assert.Single(tokens);
        Assert.Equal("tag", tokens[0].Type);
        Assert.Equal("smoke", tokens[0].Value);
    }

    [Fact]
    public void Status_returns_status_token()
    {
        var tokens = CallTokenise("$failed");
        Assert.Single(tokens);
        Assert.Equal("status", tokens[0].Type);
        Assert.Equal("failed", tokens[0].Value);
    }

    [Fact]
    public void And_operator_returns_and_token()
    {
        var tokens = CallTokenise("a && b");
        Assert.Equal(3, tokens.Count);
        Assert.Equal("and", tokens[1].Type);
    }

    [Fact]
    public void Or_operator_returns_or_token()
    {
        var tokens = CallTokenise("a || b");
        Assert.Equal(3, tokens.Count);
        Assert.Equal("or", tokens[1].Type);
    }

    [Fact]
    public void Not_operator_returns_not_token()
    {
        var tokens = CallTokenise("!!a");
        Assert.Equal(2, tokens.Count);
        Assert.Equal("not", tokens[0].Type);
        Assert.Equal("text", tokens[1].Type);
        Assert.Equal("a", tokens[1].Value);
    }

    [Fact]
    public void Lparen_returns_lparen_token()
    {
        var tokens = CallTokenise("(a)");
        Assert.Equal("lparen", tokens[0].Type);
    }

    [Fact]
    public void Rparen_returns_rparen_token()
    {
        var tokens = CallTokenise("(a)");
        Assert.Equal("rparen", tokens[2].Type);
    }

    #endregion

    #region Compound tokenisation

    [Fact]
    public void A_and_b_tokenises_correctly()
    {
        var tokens = CallTokenise("a && b");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(new TokenResult("text", "a"), tokens[0]);
        Assert.Equal(new TokenResult("and", null), tokens[1]);
        Assert.Equal(new TokenResult("text", "b"), tokens[2]);
    }

    [Fact]
    public void A_or_b_tokenises_correctly()
    {
        var tokens = CallTokenise("a || b");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(new TokenResult("text", "a"), tokens[0]);
        Assert.Equal(new TokenResult("or", null), tokens[1]);
        Assert.Equal(new TokenResult("text", "b"), tokens[2]);
    }

    [Fact]
    public void Not_a_tokenises_correctly()
    {
        var tokens = CallTokenise("!!a");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(new TokenResult("not", null), tokens[0]);
        Assert.Equal(new TokenResult("text", "a"), tokens[1]);
    }

    [Fact]
    public void Grouped_expression_tokenises_correctly()
    {
        var tokens = CallTokenise("(a || b) && c");
        Assert.Equal(7, tokens.Count);
        Assert.Equal("lparen", tokens[0].Type);
        Assert.Equal("text", tokens[1].Type);
        Assert.Equal("a", tokens[1].Value);
        Assert.Equal("or", tokens[2].Type);
        Assert.Equal("text", tokens[3].Type);
        Assert.Equal("b", tokens[3].Value);
        Assert.Equal("rparen", tokens[4].Type);
        Assert.Equal("and", tokens[5].Type);
        Assert.Equal("text", tokens[6].Type);
        Assert.Equal("c", tokens[6].Value);
    }

    [Fact]
    public void Phrase_and_tag_tokenises_correctly()
    {
        var tokens = CallTokenise("\"hello world\" && @smoke");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(new TokenResult("phrase", "hello world"), tokens[0]);
        Assert.Equal(new TokenResult("and", null), tokens[1]);
        Assert.Equal(new TokenResult("tag", "smoke"), tokens[2]);
    }

    [Fact]
    public void Status_or_status_tokenises_correctly()
    {
        var tokens = CallTokenise("$failed || $skipped");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(new TokenResult("status", "failed"), tokens[0]);
        Assert.Equal(new TokenResult("or", null), tokens[1]);
        Assert.Equal(new TokenResult("status", "skipped"), tokens[2]);
    }

    #endregion

    #region Whitespace handling

    [Fact]
    public void Leading_and_trailing_whitespace_is_ignored()
    {
        var tokens = CallTokenise("  a && b  ");
        Assert.Equal(3, tokens.Count);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("a", tokens[0].Value);
    }

    [Fact]
    public void Multiple_spaces_between_tokens_are_collapsed()
    {
        var tokens = CallTokenise("a    &&    b");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(new TokenResult("text", "a"), tokens[0]);
        Assert.Equal(new TokenResult("and", null), tokens[1]);
        Assert.Equal(new TokenResult("text", "b"), tokens[2]);
    }

    [Fact]
    public void Tabs_and_mixed_whitespace_handled()
    {
        var tokens = CallTokenise("a\t&&\tb");
        Assert.Equal(3, tokens.Count);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("and", tokens[1].Type);
        Assert.Equal("text", tokens[2].Type);
    }

    #endregion

    #region Case handling

    [Fact]
    public void Text_tokens_are_lowercased()
    {
        var tokens = CallTokenise("CHOCOLATE");
        Assert.Single(tokens);
        Assert.Equal("chocolate", tokens[0].Value);
    }

    [Fact]
    public void Tag_tokens_are_lowercased()
    {
        var tokens = CallTokenise("@SMOKE");
        Assert.Single(tokens);
        Assert.Equal("smoke", tokens[0].Value);
    }

    [Fact]
    public void Status_tokens_are_lowercased()
    {
        var tokens = CallTokenise("$FAILED");
        Assert.Single(tokens);
        Assert.Equal("failed", tokens[0].Value);
    }

    [Fact]
    public void Phrase_tokens_are_lowercased()
    {
        var tokens = CallTokenise("\"Hello World\"");
        Assert.Single(tokens);
        Assert.Equal("hello world", tokens[0].Value);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Unclosed_quote_treats_rest_as_phrase()
    {
        var tokens = CallTokenise("\"hello world");
        Assert.Single(tokens);
        Assert.Equal("phrase", tokens[0].Type);
        Assert.Equal("hello world", tokens[0].Value);
    }

    [Fact]
    public void Empty_quoted_phrase_produces_no_token()
    {
        var tokens = CallTokenise("\"\"");
        Assert.Empty(tokens);
    }

    [Fact]
    public void At_sign_alone_produces_no_token()
    {
        var tokens = CallTokenise("@");
        Assert.Empty(tokens);
    }

    [Fact]
    public void Dollar_sign_alone_produces_no_token()
    {
        var tokens = CallTokenise("$");
        Assert.Empty(tokens);
    }

    [Fact]
    public void Not_before_tag_produces_not_and_tag()
    {
        var tokens = CallTokenise("!!@smoke");
        Assert.Equal(2, tokens.Count);
        Assert.Equal("not", tokens[0].Type);
        Assert.Equal("tag", tokens[1].Type);
        Assert.Equal("smoke", tokens[1].Value);
    }

    [Fact]
    public void Not_before_status_produces_not_and_status()
    {
        var tokens = CallTokenise("!!$failed");
        Assert.Equal(2, tokens.Count);
        Assert.Equal("not", tokens[0].Type);
        Assert.Equal("status", tokens[1].Type);
        Assert.Equal("failed", tokens[1].Value);
    }

    [Fact]
    public void Single_bang_is_part_of_word()
    {
        var tokens = CallTokenise("!important");
        Assert.Single(tokens);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("!important", tokens[0].Value);
    }

    [Fact]
    public void Single_ampersand_is_part_of_word()
    {
        var tokens = CallTokenise("a&b");
        Assert.Single(tokens);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("a&b", tokens[0].Value);
    }

    [Fact]
    public void Single_pipe_is_part_of_word()
    {
        var tokens = CallTokenise("a|b");
        Assert.Single(tokens);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("a|b", tokens[0].Value);
    }

    [Fact]
    public void Tag_with_hyphen_is_preserved()
    {
        var tokens = CallTokenise("@happy-path");
        Assert.Single(tokens);
        Assert.Equal("tag", tokens[0].Type);
        Assert.Equal("happy-path", tokens[0].Value);
    }

    [Fact]
    public void Tag_with_dot_is_preserved()
    {
        var tokens = CallTokenise("@api.v2");
        Assert.Single(tokens);
        Assert.Equal("tag", tokens[0].Type);
        Assert.Equal("api.v2", tokens[0].Value);
    }

    [Fact]
    public void Implicit_AND_inserted_between_adjacent_operands()
    {
        // login success && @smoke  →  login [AND] success [AND] @smoke
        var tokens = CallTokenise("login success && @smoke");
        Assert.Equal(5, tokens.Count);
        Assert.Equal(new TokenResult("text", "login"), tokens[0]);
        Assert.Equal(new TokenResult("and", null), tokens[1]); // implicit
        Assert.Equal(new TokenResult("text", "success"), tokens[2]);
        Assert.Equal(new TokenResult("and", null), tokens[3]); // explicit
        Assert.Equal(new TokenResult("tag", "smoke"), tokens[4]);
    }

    [Fact]
    public void Implicit_AND_inserted_between_rparen_and_operand()
    {
        // (a) b → (a) [AND] b
        var tokens = CallTokenise("(a) b");
        Assert.Equal(5, tokens.Count);
        Assert.Equal("lparen", tokens[0].Type);
        Assert.Equal("text", tokens[1].Type);
        Assert.Equal("rparen", tokens[2].Type);
        Assert.Equal("and", tokens[3].Type);
        Assert.Equal("text", tokens[4].Type);
    }

    [Fact]
    public void Implicit_AND_inserted_between_operand_and_lparen()
    {
        // a (b) → a [AND] (b)
        var tokens = CallTokenise("a (b)");
        Assert.Equal(5, tokens.Count);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("and", tokens[1].Type);
        Assert.Equal("lparen", tokens[2].Type);
        Assert.Equal("text", tokens[3].Type);
        Assert.Equal("rparen", tokens[4].Type);
    }

    [Fact]
    public void Implicit_AND_inserted_before_not()
    {
        // a !!b → a [AND] !!b
        var tokens = CallTokenise("a !!b");
        Assert.Equal(4, tokens.Count);
        Assert.Equal("text", tokens[0].Type);
        Assert.Equal("and", tokens[1].Type);
        Assert.Equal("not", tokens[2].Type);
        Assert.Equal("text", tokens[3].Type);
    }

    [Fact]
    public void Operators_inside_quotes_are_not_parsed()
    {
        var tokens = CallTokenise("\"a && b\"");
        Assert.Single(tokens);
        Assert.Equal("phrase", tokens[0].Type);
        Assert.Equal("a && b", tokens[0].Value);
    }

    #endregion
}
