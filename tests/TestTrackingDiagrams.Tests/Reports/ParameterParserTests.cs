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

    // ── TryParseRecordToString tests ──

    [Fact]
    public void TryParseRecordToString_parses_simple_record()
    {
        var result = ParameterParser.TryParseRecordToString("TypeName { Prop1 = 89, Prop2 = hello }");
        Assert.NotNull(result);
        Assert.Equal("89", result!["Prop1"]);
        Assert.Equal("hello", result["Prop2"]);
    }

    [Fact]
    public void TryParseRecordToString_parses_null_values()
    {
        var result = ParameterParser.TryParseRecordToString("MyRecord { X = null, Y = 42 }");
        Assert.NotNull(result);
        Assert.Equal("null", result!["X"]);
        Assert.Equal("42", result["Y"]);
    }

    [Fact]
    public void TryParseRecordToString_parses_quoted_string_values()
    {
        var result = ParameterParser.TryParseRecordToString("""MyRecord { Name = "hello world", Count = 5 }""");
        Assert.NotNull(result);
        Assert.Equal("hello world", result!["Name"]);
        Assert.Equal("5", result["Count"]);
    }

    [Fact]
    public void TryParseRecordToString_parses_real_account_risk_scenario()
    {
        var input = """AccountRiskScoreScenario { AccountAgeInDays = 89, AccountRiskScore = 320, ApplicationRiskScore = null, ExpectedRiskband = "E", Reason = "Pre 90. No application score present, account score in E band" }""";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Equal("89", result!["AccountAgeInDays"]);
        Assert.Equal("320", result["AccountRiskScore"]);
        Assert.Equal("null", result["ApplicationRiskScore"]);
        Assert.Equal("E", result["ExpectedRiskband"]);
        Assert.Equal("Pre 90. No application score present, account score in E band", result["Reason"]);
    }

    [Fact]
    public void TryParseRecordToString_returns_null_for_plain_string()
    {
        Assert.Null(ParameterParser.TryParseRecordToString("just a plain string"));
    }

    [Fact]
    public void TryParseRecordToString_returns_null_for_null_input()
    {
        Assert.Null(ParameterParser.TryParseRecordToString(null));
    }

    [Fact]
    public void TryParseRecordToString_returns_null_for_empty_braces()
    {
        Assert.Null(ParameterParser.TryParseRecordToString("TypeName { }"));
    }

    [Fact]
    public void TryParseRecordToString_handles_truncated_values_with_ellipsis()
    {
        var input = """MyRecord { Name = "Pre 90. No application score present, account scor"··..., Age = 42 }""";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Contains("Pre 90", result!["Name"]);
        Assert.Equal("42", result["Age"]);
    }

    [Fact]
    public void TryParseRecordToString_handles_commas_inside_quoted_values()
    {
        var input = """MyRecord { Desc = "hello, world", Count = 3 }""";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Equal("hello, world", result!["Desc"]);
        Assert.Equal("3", result["Count"]);
    }

    // ── TryParseRecordToString: truncated record tests (xUnit display name truncation) ──

    [Fact]
    public void TryParseRecordToString_handles_truncated_record_ending_with_middot_ellipsis()
    {
        // xUnit truncates the entire record ToString() — no closing " }"
        var input = "AccountRiskScoreScenario { AccountAgeInDays = 89, AccountRiskScore = 320, ApplicationRiskScore = null, ExpectedRiskband = \"E\", Reason = \"Pre 90. No application score present, account scor\"\u00B7\u00B7...";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Equal("89", result!["AccountAgeInDays"]);
        Assert.Equal("320", result["AccountRiskScore"]);
        Assert.Equal("null", result["ApplicationRiskScore"]);
        Assert.Equal("E", result["ExpectedRiskband"]);
        Assert.Contains("Pre 90", result["Reason"]);
    }

    [Fact]
    public void TryParseRecordToString_handles_truncated_record_mid_unquoted_value()
    {
        // Truncation in the middle of a numeric value
        var input = "MyRecord { Count = 42, LongName = \"hello\", Score = 12\u00B7\u00B7...";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Equal("42", result!["Count"]);
        Assert.Equal("hello", result["LongName"]);
        // Score may be partially parsed or omitted — at least Count and LongName are present
        Assert.True(result.Count >= 2);
    }

    [Fact]
    public void TryParseRecordToString_handles_truncated_record_mid_property_name()
    {
        // Truncation in the middle of a property name — incomplete property discarded
        var input = "MyRecord { Count = 42, LongProp\u00B7\u00B7...";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Equal("42", result!["Count"]);
        Assert.Single(result); // Only Count parsed, LongProp discarded
    }

    [Fact]
    public void TryParseRecordToString_handles_truncated_record_mid_quoted_value()
    {
        // Truncation inside a quoted string (unclosed quote)
        var input = "MyRecord { Id = 5, Desc = \"some long descri\u00B7\u00B7...";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Equal("5", result!["Id"]);
        Assert.True(result.Count >= 1);
    }

    [Fact]
    public void TryParseRecordToString_handles_truncated_record_with_plain_ellipsis()
    {
        // Some formatters use plain "..." without middle dots
        var input = "MyRecord { X = 1, Y = 2, Z = 3...";
        var result = ParameterParser.TryParseRecordToString(input);
        Assert.NotNull(result);
        Assert.Equal("1", result!["X"]);
        Assert.Equal("2", result["Y"]);
        Assert.True(result.Count >= 2);
    }

    [Fact]
    public void Parse_handles_square_brackets_in_quoted_param_values_parens_format()
    {
        var result = ParameterParser.Parse(
            "Order_with_invalid_field_should_return_bad_request(field: \"Items[0].BatchId\", value: null, reason: \"Batch ID is required\", expectedError: \"'Batch Id' is required.\", expectedStatus: \"Bad Request\")");
        Assert.NotNull(result);
        Assert.Equal(5, result!.Count);
        Assert.Equal("Items[0].BatchId", result["field"]);
        Assert.Equal("null", result["value"]);
        Assert.Equal("Batch ID is required", result["reason"]);
        Assert.Equal("'Batch Id' is required.", result["expectedError"]);
        Assert.Equal("Bad Request", result["expectedStatus"]);
    }

    [Fact]
    public void Parse_handles_square_brackets_in_bracketed_format()
    {
        var result = ParameterParser.Parse(
            "Order validation [field: Items[0].BatchId, value: null, reason: Batch ID is required]");
        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal("Items[0].BatchId", result["field"]);
        Assert.Equal("null", result["value"]);
        Assert.Equal("Batch ID is required", result["reason"]);
    }

    [Fact]
    public void Parse_handles_square_brackets_in_quoted_bracketed_format()
    {
        var result = ParameterParser.Parse(
            "Order validation [field: \"Items[0].BatchId\", value: null]");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("Items[0].BatchId", result["field"]);
        Assert.Equal("null", result["value"]);
    }

    [Fact]
    public void ExtractBaseName_handles_square_brackets_in_param_values()
    {
        var result = ParameterParser.ExtractBaseName(
            "Order validation [field: Items[0].BatchId, value: null]");
        Assert.Equal("Order validation", result);
    }
}
