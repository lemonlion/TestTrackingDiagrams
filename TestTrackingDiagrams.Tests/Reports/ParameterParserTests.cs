using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ParameterParserTests
{
    [Fact]
    public void Parse_named_params_from_display_name()
    {
        var result = ParameterParser.Parse("Namespace.Class.Method(region: \"UK\", amount: 100)");
        Assert.NotNull(result);
        Assert.Equal("UK", result!["region"]);
        Assert.Equal("100", result["amount"]);
    }

    [Fact]
    public void Parse_positional_params_from_display_name()
    {
        var result = ParameterParser.Parse("Namespace.Class.Method(\"UK\", 100)");
        Assert.NotNull(result);
        Assert.Equal("UK", result!["arg0"]);
        Assert.Equal("100", result["arg1"]);
    }

    [Fact]
    public void Parse_bracketed_params_from_display_name()
    {
        var result = ParameterParser.Parse("Given request [region: UK, amount: 100]");
        Assert.NotNull(result);
        Assert.Equal("UK", result!["region"]);
        Assert.Equal("100", result["amount"]);
    }

    [Fact]
    public void Returns_null_for_no_params()
    {
        var result = ParameterParser.Parse("SomeTest");
        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_for_custom_display_name_no_parens()
    {
        var result = ParameterParser.Parse("Happy path UK merchant");
        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_for_null_input()
    {
        var result = ParameterParser.Parse(null!);
        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_for_empty_string()
    {
        var result = ParameterParser.Parse("");
        Assert.Null(result);
    }

    [Fact]
    public void Handles_empty_parens()
    {
        var result = ParameterParser.Parse("Namespace.Class.Method()");
        Assert.Null(result);
    }

    [Fact]
    public void Handles_single_named_param()
    {
        var result = ParameterParser.Parse("Test(x: 42)");
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("42", result["x"]);
    }

    [Fact]
    public void Handles_single_positional_param()
    {
        var result = ParameterParser.Parse("Test(42)");
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("42", result["arg0"]);
    }

    [Fact]
    public void Handles_string_with_commas_inside_quotes()
    {
        var result = ParameterParser.Parse("Test(name: \"Smith, John\", age: 30)");
        Assert.NotNull(result);
        Assert.Equal("Smith, John", result!["name"]);
        Assert.Equal("30", result["age"]);
    }

    [Fact]
    public void Handles_nested_parens_as_single_value()
    {
        var result = ParameterParser.Parse("Test(obj: MyType(1, 2), flag: true)");
        Assert.NotNull(result);
        Assert.Equal("MyType(1, 2)", result!["obj"]);
        Assert.Equal("true", result["flag"]);
    }

    [Fact]
    public void Handles_null_literal_value()
    {
        var result = ParameterParser.Parse("Test(x: null, y: \"hello\")");
        Assert.NotNull(result);
        Assert.Equal("null", result!["x"]);
        Assert.Equal("hello", result["y"]);
    }

    [Fact]
    public void Handles_boolean_values()
    {
        var result = ParameterParser.Parse("Test(enabled: True, verbose: False)");
        Assert.NotNull(result);
        Assert.Equal("True", result!["enabled"]);
        Assert.Equal("False", result["verbose"]);
    }

    [Fact]
    public void Extracts_base_name_from_parens()
    {
        var result = ParameterParser.ExtractBaseName("Namespace.Class.Method(region: \"UK\", amount: 100)");
        Assert.Equal("Namespace.Class.Method", result);
    }

    [Fact]
    public void Extracts_base_name_from_brackets()
    {
        var result = ParameterParser.ExtractBaseName("Given request [region: UK]");
        Assert.Equal("Given request", result);
    }

    [Fact]
    public void Extracts_base_name_returns_full_string_when_no_params()
    {
        var result = ParameterParser.ExtractBaseName("SomeTest");
        Assert.Equal("SomeTest", result);
    }

    [Fact]
    public void Extracts_base_name_returns_null_for_null()
    {
        var result = ParameterParser.ExtractBaseName(null!);
        Assert.Null(result);
    }

    [Fact]
    public void Handles_mixed_positional_and_named_treats_as_named()
    {
        // xUnit2 always uses named format, xUnit3/TUnit always positional
        // But if we get a mix, treat all as named when first entry has colon
        var result = ParameterParser.Parse("Test(x: 1, 2)");
        Assert.NotNull(result);
        Assert.Equal("1", result!["x"]);
        Assert.Equal("2", result["arg1"]);
    }

    [Fact]
    public void Parse_trims_whitespace_from_values()
    {
        var result = ParameterParser.Parse("Test(x:  hello , y:  world )");
        Assert.NotNull(result);
        Assert.Equal("hello", result!["x"]);
        Assert.Equal("world", result["y"]);
    }

    [Fact]
    public void Parse_handles_bracketed_single_param()
    {
        var result = ParameterParser.Parse("Scenario name [count: 5]");
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("5", result["count"]);
    }
}
