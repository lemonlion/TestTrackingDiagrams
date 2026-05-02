using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class AssertionExpressionFormatterTests
{
    [Theory]
    [InlineData("() => foo.Should().Be(\"blah\")", "Foo should be \"blah\"")]
    [InlineData("() => response.StatusCode.Should().Be(HttpStatusCode.OK)", "Response status code should be OK")]
    [InlineData("() => order.Items.Should().HaveCount(3)", "Order items should have count 3")]
    [InlineData("() => order.Total.Should().BeGreaterThan(0)", "Order total should be greater than 0")]
    [InlineData("() => items.Should().NotBeEmpty()", "Items should not be empty")]
    [InlineData("() => result.Name.Should().StartWith(\"John\")", "Result name should start with \"John\"")]
    [InlineData("() => users.Should().ContainSingle(x => x.IsAdmin)", "Users should contain single x => x.IsAdmin")]
    [InlineData("() => result.Should().BeNull()", "Result should be null")]
    [InlineData("() => count.Should().Be(42)", "Count should be 42")]
    [InlineData("() => list.Should().BeEmpty()", "List should be empty")]
    [InlineData("() => name.Should().NotBeNullOrWhiteSpace()", "Name should not be null or white space")]
    [InlineData("() => response.Headers.ContentType.Should().Be(\"application/json\")", "Response headers content type should be \"application/json\"")]
    public void Format_parses_Should_pattern_into_readable_sentence(string expression, string expected)
    {
        var result = AssertionExpressionFormatter.Format(expression);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_strips_lambda_prefix()
    {
        var result = AssertionExpressionFormatter.Format("() => x.Should().BeTrue()");
        Assert.Equal("X should be true", result);
    }

    [Fact]
    public void Format_returns_cleaned_expression_when_no_Should_pattern()
    {
        var result = AssertionExpressionFormatter.Format("() => Assert.True(value)");
        Assert.Equal("Assert.True(value)", result);
    }

    [Fact]
    public void Format_handles_expression_without_lambda_prefix()
    {
        var result = AssertionExpressionFormatter.Format("foo.Should().BeTrue()");
        Assert.Equal("Foo should be true", result);
    }

    [Fact]
    public void Format_handles_null_expression()
    {
        var result = AssertionExpressionFormatter.Format(null);
        Assert.Equal("", result);
    }

    [Fact]
    public void Format_handles_empty_expression()
    {
        var result = AssertionExpressionFormatter.Format("");
        Assert.Equal("", result);
    }

    [Theory]
    [InlineData("() => result.Should().BeOfType<string>()", "Result should be of type <string>")]
    [InlineData("() => items.Should().AllBeOfType<int>()", "Items should all be of type <int>")]
    public void Format_handles_generic_type_arguments(string expression, string expected)
    {
        var result = AssertionExpressionFormatter.Format(expression);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_handles_chained_And_by_taking_first_assertion()
    {
        var result = AssertionExpressionFormatter.Format("() => result.Should().NotBeNull().And.NotBeEmpty()");
        Assert.Equal("Result should not be null", result);
    }

    [Theory]
    [InlineData("() => dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5))", "Dto created at should be close to DateTime.UtcNow, TimeSpan.FromSeconds(5)")]
    [InlineData("() => list.Should().HaveCountGreaterThan(0)", "List should have count greater than 0")]
    public void Format_handles_multiple_arguments(string expression, string expected)
    {
        var result = AssertionExpressionFormatter.Format(expression);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_strips_enum_prefix()
    {
        var result = AssertionExpressionFormatter.Format("() => status.Should().Be(TaskStatus.RanToCompletion)");
        Assert.Equal("Status should be RanToCompletion", result);
    }
}
