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

    [Fact]
    public void Parse_handles_multiple_separate_brackets()
    {
        // LightBDD appends each unmatched parameter in its own bracket: [param1: val] [param2: val]
        var result = ParameterParser.Parse("Scenario name [version: \"V1\"] [claimName: \"LivePersonSdes\"]");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("V1", result["version"]);
        Assert.Equal("LivePersonSdes", result["claimName"]);
    }

    [Fact]
    public void Parse_handles_three_separate_brackets()
    {
        var result = ParameterParser.Parse("Name [a: 1] [b: 2] [c: 3]");
        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal("1", result["a"]);
        Assert.Equal("2", result["b"]);
        Assert.Equal("3", result["c"]);
    }

    [Fact]
    public void ExtractBaseName_strips_all_trailing_brackets()
    {
        var result = ParameterParser.ExtractBaseName("Scenario name [version: \"V1\"] [claimName: \"LivePersonSdes\"]");
        Assert.Equal("Scenario name", result);
    }

    [Fact]
    public void ExtractBaseName_strips_three_trailing_brackets()
    {
        var result = ParameterParser.ExtractBaseName("Name [a: 1] [b: 2] [c: 3]");
        Assert.Equal("Name", result);
    }

    [Fact]
    public void Parse_handles_record_ToString_in_brackets()
    {
        var result = ParameterParser.Parse(
            "Test [testCase: MyRecord { Name = Test 0, Value = 42 }]");
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("MyRecord { Name = Test 0, Value = 42 }", result["testCase"]);
    }

    [Fact]
    public void Parse_handles_nested_record_ToString_in_brackets()
    {
        var result = ParameterParser.Parse(
            "Test [testCase: RefundUpfrontPaymentTestCase { Name = Test 0, Input = RefundUpfrontPaymentTestCaseInputs { CreditOrder = upfrontpayment, CreditAmount = 90, UpfrontPaymentAmount = 10, RefundAmount = 60 }, Expected = RefundUpfrontPaymentTestCaseExpectation { Amount = 60 } }]");
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.StartsWith("RefundUpfrontPaymentTestCase {", result["testCase"]);
        Assert.EndsWith("}", result["testCase"]);
    }

    [Fact]
    public void Parse_handles_braces_with_multiple_params_in_brackets()
    {
        var result = ParameterParser.Parse(
            "Test [a: Rec { X = 1, Y = 2 }, b: 99]");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("Rec { X = 1, Y = 2 }", result["a"]);
        Assert.Equal("99", result["b"]);
    }

    [Fact]
    public void Parse_handles_braces_in_parens()
    {
        var result = ParameterParser.Parse(
            "Test(obj: MyRecord { A = 1, B = 2 }, flag: true)");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("MyRecord { A = 1, B = 2 }", result["obj"]);
        Assert.Equal("true", result["flag"]);
    }

    [Fact]
    public void Parse_handles_deeply_nested_braces()
    {
        var result = ParameterParser.Parse(
            "Test [x: Outer { Inner = Mid { Deep = 1, Val = 2 }, Other = 3 }]");
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("Outer { Inner = Mid { Deep = 1, Val = 2 }, Other = 3 }", result["x"]);
    }

    [Fact]
    public void ExtractStructured_should_map_named_parameters()
    {
        var args = new object[] { "hello", 42 };
        var paramNames = new[] { "message", "count" };

        var result = ParameterParser.ExtractStructuredParameters(args, paramNames);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("hello", result["message"]);
        Assert.Equal("42", result["count"]);
    }

    [Fact]
    public void ExtractStructured_returns_null_when_args_null()
    {
        Assert.Null(ParameterParser.ExtractStructuredParameters(null, new[] { "a" }));
    }

    [Fact]
    public void ExtractStructured_returns_null_when_args_empty()
    {
        Assert.Null(ParameterParser.ExtractStructuredParameters(Array.Empty<object>(), new[] { "a" }));
    }

    [Fact]
    public void ExtractStructured_returns_null_when_param_names_null()
    {
        Assert.Null(ParameterParser.ExtractStructuredParameters(new object[] { 1 }, null));
    }

    [Fact]
    public void ExtractStructured_returns_null_when_param_names_empty()
    {
        Assert.Null(ParameterParser.ExtractStructuredParameters(new object[] { 1 }, Array.Empty<string>()));
    }

    [Fact]
    public void ExtractStructured_returns_null_when_lengths_mismatch()
    {
        Assert.Null(ParameterParser.ExtractStructuredParameters(new object[] { "a", "b" }, new[] { "x" }));
    }

    [Fact]
    public void ExtractStructured_handles_null_arg_value()
    {
        var result = ParameterParser.ExtractStructuredParameters(new object?[] { null, "value" }!, new[] { "first", "second" });

        Assert.NotNull(result);
        Assert.Equal("", result!["first"]);
        Assert.Equal("value", result["second"]);
    }

    [Fact]
    public void ExtractStructured_uses_toString_on_complex_objects()
    {
        var result = ParameterParser.ExtractStructuredParameters(
            new object[] { new StructuredTestRecord("hello", 42) },
            new[] { "testCase" });

        Assert.NotNull(result);
        Assert.Equal("StructuredTestRecord { Name = hello, Value = 42 }", result!["testCase"]);
    }

    [Fact]
    public void ExtractStructured_uses_positional_fallback_for_null_param_name()
    {
        var result = ParameterParser.ExtractStructuredParameters(
            new object[] { "val" },
            new string?[] { null });

        Assert.NotNull(result);
        Assert.Equal("val", result!["param0"]);
    }

    [Fact]
    public void ExtractStructuredParametersWithRaw_returns_both_string_and_raw_values()
    {
        var obj = new StructuredTestRecord("UK", 100);
        object[] args = [obj, "extra"];
        string[] names = ["request", "tag"];

        var result = ParameterParser.ExtractStructuredParametersWithRaw(args, names);
        Assert.NotNull(result);
        var (stringValues, rawValues) = result!.Value;

        Assert.Equal(obj.ToString(), stringValues["request"]);
        Assert.Equal("extra", stringValues["tag"]);
        Assert.Same(obj, rawValues["request"]);
        Assert.Equal("extra", rawValues["tag"]);
    }

    [Fact]
    public void ExtractStructuredParametersWithRaw_returns_null_for_null_args()
    {
        Assert.Null(ParameterParser.ExtractStructuredParametersWithRaw(null, new[] { "a" }));
    }

    [Fact]
    public void ExtractStructuredParametersWithRaw_returns_null_for_mismatched_lengths()
    {
        Assert.Null(ParameterParser.ExtractStructuredParametersWithRaw(new object[] { 1, 2 }, new[] { "a" }));
    }

    [Fact]
    public void ExtractStructuredParametersWithRaw_preserves_null_raw_values()
    {
        var result = ParameterParser.ExtractStructuredParametersWithRaw(new object?[] { null }!, new[] { "x" });
        Assert.NotNull(result);
        Assert.Null(result!.Value.RawValues["x"]);
        Assert.Equal("", result.Value.StringValues["x"]);
    }

    private record StructuredTestRecord(string Name, int Value);
}
