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
    [InlineData("() => users.Should().ContainSingle(x => x.IsAdmin)", "Users should contain single [x => x.IsAdmin]")]
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

    [Fact]
    public void Format_removes_null_forgiving_operators()
    {
        var result = AssertionExpressionFormatter.Format("() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK)");
        Assert.Equal("Audit log response status code should be OK", result);
    }

    [Fact]
    public void Format_strips_underscore_prefix_from_subject()
    {
        var result = AssertionExpressionFormatter.Format("() => _pancakeSteps.ResponseMessage.Should().NotBeNull()");
        Assert.Equal("Pancake steps response message should not be null", result);
    }

    [Theory]
    [InlineData("() => myVariable.Should().OnlyContain(x => x.Foo == bar)", "My variable should only contain [x => x.Foo == bar]")]
    [InlineData("() => items.Should().AllSatisfy(x => x.IsValid)", "Items should all satisfy [x => x.IsValid]")]
    [InlineData("() => _auditLogs.Should().BeInDescendingOrder(l => l.Timestamp)", "Audit logs should be in descending order [l => l.Timestamp]")]
    public void Format_wraps_lambda_args_in_square_brackets(string expression, string expected)
    {
        var result = AssertionExpressionFormatter.Format(expression);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_substitutes_resolved_variable_in_args()
    {
        var resolved = new Dictionary<string, string> { ["expected"] = "hello" };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().Be(expected)", resolved);
        Assert.Equal("X should be 'hello'", result);
    }

    [Fact]
    public void Format_substitutes_guid_variable()
    {
        var resolved = new Dictionary<string, string>
            { ["confirmationId"] = "12345678-1234-1234-1234-123456789abc" };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().Be(confirmationId)", resolved);
        Assert.Equal("X should be '12345678-1234-1234-1234-123456789abc'", result);
    }

    [Fact]
    public void Format_leaves_unresolved_variable_unchanged()
    {
        var resolved = new Dictionary<string, string>();
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().Be(expected)", resolved);
        Assert.Equal("X should be expected", result);
    }

    [Fact]
    public void Format_with_null_resolved_values_behaves_like_original()
    {
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().Be(expected)", null);
        Assert.Equal("X should be expected", result);
    }

    [Fact]
    public void Format_substitutes_only_in_args_not_subject()
    {
        var resolved = new Dictionary<string, string> { ["result"] = "some value" };
        var result = AssertionExpressionFormatter.Format(
            "() => result.Should().Be(expected)", resolved);
        // 'result' is the subject — should NOT be substituted; 'expected' is not resolved
        Assert.Equal("Result should be expected", result);
    }

    [Fact]
    public void Format_substitutes_multiple_resolved_values()
    {
        var resolved = new Dictionary<string, string>
        {
            ["min"] = "1",
            ["max"] = "100"
        };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().BeInRange(min, max)", resolved);
        Assert.Equal("X should be in range '1', '100'", result);
    }

    [Fact]
    public void Format_does_not_substitute_inside_quoted_strings()
    {
        var resolved = new Dictionary<string, string> { ["expected"] = "world" };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().Be(\"expected\")", resolved);
        // "expected" is a string literal, not a variable reference
        Assert.Equal("X should be \"expected\"", result);
    }

    [Fact]
    public void Format_does_not_substitute_partial_token_match()
    {
        var resolved = new Dictionary<string, string> { ["id"] = "123" };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().Be(orderId)", resolved);
        // 'id' should NOT match inside 'orderId'
        Assert.Equal("X should be orderId", result);
    }

    [Fact]
    public void Format_substitutes_null_resolved_value()
    {
        var resolved = new Dictionary<string, string> { ["expected"] = "null" };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().Be(expected)", resolved);
        Assert.Equal("X should be 'null'", result);
    }

    [Fact]
    public void Format_substitutes_dotted_property_chain_value()
    {
        var resolved = new Dictionary<string, string> { ["expected.ExpectedIngredientCount"] = "5" };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().HaveCount(expected.ExpectedIngredientCount)", resolved);
        Assert.Equal("X should have count '5'", result);
    }

    [Fact]
    public void Format_prefers_longer_key_over_shorter_key()
    {
        // Both keys present — longer dotted key must match first
        var resolved = new Dictionary<string, string>
        {
            ["expected"] = "MuffinBatchExpectation { ... }",
            ["expected.ExpectedIngredientCount"] = "5"
        };
        var result = AssertionExpressionFormatter.Format(
            "() => x.Should().HaveCount(expected.ExpectedIngredientCount)", resolved);
        Assert.Equal("X should have count '5'", result);
    }
}
