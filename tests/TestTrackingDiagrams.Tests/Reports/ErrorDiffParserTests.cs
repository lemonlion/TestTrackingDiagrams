using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ErrorDiffParserTests
{
    [Fact]
    public void Returns_null_when_error_message_is_null()
    {
        var result = ErrorDiffParser.TryParseExpectedActual(null);
        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_when_error_message_is_empty()
    {
        var result = ErrorDiffParser.TryParseExpectedActual("");
        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_when_error_message_has_no_diff_pattern()
    {
        var result = ErrorDiffParser.TryParseExpectedActual("Something went wrong: NullReferenceException");
        Assert.Null(result);
    }

    [Fact]
    public void Parses_xUnit_Assert_Equal_failure()
    {
        var message = """
            Assert.Equal() Failure: Values differ
            Expected: Hello World
            Actual:   Hello Mars
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("Hello World", result.Expected);
        Assert.Equal("Hello Mars", result.Actual);
    }

    [Fact]
    public void Parses_xUnit_Assert_Equal_with_strings()
    {
        var message = """
            Assert.Equal() Failure: Strings differ
                           ↓ (pos 6)
            Expected: "Hello World"
            Actual:   "Hello Mars"
                           ↑ (pos 6)
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("Hello World", result.Expected);
        Assert.Equal("Hello Mars", result.Actual);
    }

    [Fact]
    public void Parses_NUnit_expected_but_was()
    {
        var message = """
              Expected: "Hello World"
              But was:  "Hello Mars"
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("Hello World", result.Expected);
        Assert.Equal("Hello Mars", result.Actual);
    }

    [Fact]
    public void Parses_FluentAssertions_expected_to_be()
    {
        var message = """
            Expected string to be "Hello World" with a length of 11, but "Hello Mars" has a length of 10.
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("Hello World", result.Expected);
        Assert.Equal("Hello Mars", result.Actual);
    }

    [Fact]
    public void Parses_Shouldly_expected_but_was()
    {
        var message = """
            should be
                "Hello World"
            but was
                "Hello Mars"
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("Hello World", result.Expected);
        Assert.Equal("Hello Mars", result.Actual);
    }

    [Fact]
    public void Parses_xUnit_Assert_Equal_numeric()
    {
        var message = """
            Assert.Equal() Failure: Values differ
            Expected: 42
            Actual:   99
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("42", result.Expected);
        Assert.Equal("99", result.Actual);
    }

    [Fact]
    public void Parses_Expected_Actual_multiline_with_whitespace_variations()
    {
        var message = """
            Expected:    foo bar
            Actual:      foo baz
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("foo bar", result.Expected);
        Assert.Equal("foo baz", result.Actual);
    }

    [Fact]
    public void GenerateDiffHtml_highlights_differing_characters()
    {
        var html = ErrorDiffParser.GenerateDiffHtml("abc", "axc");
        Assert.Contains("diff-expected", html);
        Assert.Contains("diff-actual", html);
        Assert.Contains("diff-del", html);
        Assert.Contains("diff-ins", html);
    }

    [Fact]
    public void GenerateDiffHtml_marks_identical_text_without_diff_markers()
    {
        var html = ErrorDiffParser.GenerateDiffHtml("same", "same");
        Assert.DoesNotContain("diff-del", html);
        Assert.DoesNotContain("diff-ins", html);
    }

    [Fact]
    public void GenerateDiffHtml_handles_different_lengths()
    {
        var html = ErrorDiffParser.GenerateDiffHtml("Hello World", "Hello");
        Assert.Contains("diff-del", html);
    }

    [Fact]
    public void Parses_FluentAssertions_expected_to_be_equivalent()
    {
        var message = """
            Expected string to be equivalent to "HELLO WORLD" with a length of 11, but "hello mars" has a length of 10.
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("HELLO WORLD", result.Expected);
        Assert.Equal("hello mars", result.Actual);
    }

    [Fact]
    public void Strips_quotes_from_expected_and_actual()
    {
        var message = """
            Expected: "Hello World"
            Actual:   "Hello Mars"
            """;
        var result = ErrorDiffParser.TryParseExpectedActual(message);
        Assert.NotNull(result);
        Assert.Equal("Hello World", result.Expected);
        Assert.Equal("Hello Mars", result.Actual);
    }
}
