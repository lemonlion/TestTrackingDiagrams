using System.Text;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ParameterValueRendererTests
{
    #region Test types

    private record SimpleScalarRecord(string Region, int Amount, string Currency);

    private record SmallComplexRecord(string Name, string SortCode, string AccountNo);

    private record LargeComplexRecord(string A, string B, string C, string D, string E, string F);

    private record NestedRecord(string Name, SmallComplexRecord Address);

    private record RecordWithArray(string Name, string[] Items);

    private record RecordWithCollection(string Name, List<int> Scores);

    private enum TestEnum { ValueA, ValueB }

    private record RecordWithEnum(string Name, TestEnum Status);

    private record RecordWithNullable(string Name, int? Value);

    private record EmptyRecord;

    #endregion

    #region IsScalarType

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(double))]
    [InlineData(typeof(long))]
    public void IsScalarType_returns_true_for_scalar_types(Type type)
    {
        Assert.True(ParameterValueRenderer.IsScalarType(type));
    }

    [Fact]
    public void IsScalarType_returns_true_for_enum()
    {
        Assert.True(ParameterValueRenderer.IsScalarType(typeof(TestEnum)));
    }

    [Fact]
    public void IsScalarType_returns_true_for_nullable_int()
    {
        Assert.True(ParameterValueRenderer.IsScalarType(typeof(int?)));
    }

    [Fact]
    public void IsScalarType_returns_false_for_complex_type()
    {
        Assert.False(ParameterValueRenderer.IsScalarType(typeof(SimpleScalarRecord)));
    }

    [Fact]
    public void IsScalarType_returns_false_for_array()
    {
        Assert.False(ParameterValueRenderer.IsScalarType(typeof(string[])));
    }

    #endregion

    #region IsSmallComplexObject

    [Fact]
    public void IsSmallComplexObject_returns_true_for_record_with_3_scalar_props()
    {
        var obj = new SmallComplexRecord("Acme", "12-34-56", "12345678");
        Assert.True(ParameterValueRenderer.IsSmallComplexObject(obj));
    }

    [Fact]
    public void IsSmallComplexObject_returns_false_for_null()
    {
        Assert.False(ParameterValueRenderer.IsSmallComplexObject(null));
    }

    [Fact]
    public void IsSmallComplexObject_returns_false_for_scalar()
    {
        Assert.False(ParameterValueRenderer.IsSmallComplexObject("hello"));
    }

    [Fact]
    public void IsSmallComplexObject_returns_false_for_too_many_properties()
    {
        var obj = new LargeComplexRecord("a", "b", "c", "d", "e", "f");
        Assert.False(ParameterValueRenderer.IsSmallComplexObject(obj));
    }

    [Fact]
    public void IsSmallComplexObject_returns_false_for_nested_object()
    {
        var obj = new NestedRecord("Test", new SmallComplexRecord("Acme", "12-34-56", "12345678"));
        Assert.False(ParameterValueRenderer.IsSmallComplexObject(obj));
    }

    [Fact]
    public void IsSmallComplexObject_returns_false_for_object_with_array()
    {
        var obj = new RecordWithArray("Test", ["a", "b"]);
        Assert.False(ParameterValueRenderer.IsSmallComplexObject(obj));
    }

    [Fact]
    public void IsSmallComplexObject_returns_false_for_collection()
    {
        var obj = new RecordWithCollection("Test", [1, 2, 3]);
        Assert.False(ParameterValueRenderer.IsSmallComplexObject(obj));
    }

    [Fact]
    public void IsSmallComplexObject_returns_false_for_empty_record()
    {
        Assert.False(ParameterValueRenderer.IsSmallComplexObject(new EmptyRecord()));
    }

    #endregion

    #region TryGetFlattenableProperties

    [Fact]
    public void TryGetFlattenableProperties_returns_names_for_simple_record()
    {
        var obj = new SimpleScalarRecord("UK", 100, "GBP");
        var result = ParameterValueRenderer.TryGetFlattenableProperties(obj, 10);
        Assert.NotNull(result);
        Assert.Contains("Region", result!);
        Assert.Contains("Amount", result);
        Assert.Contains("Currency", result);
    }

    [Fact]
    public void TryGetFlattenableProperties_returns_null_for_scalar()
    {
        Assert.Null(ParameterValueRenderer.TryGetFlattenableProperties("hello", 10));
    }

    [Fact]
    public void TryGetFlattenableProperties_returns_null_for_null()
    {
        Assert.Null(ParameterValueRenderer.TryGetFlattenableProperties(null, 10));
    }

    [Fact]
    public void TryGetFlattenableProperties_returns_null_for_nested_properties()
    {
        var obj = new NestedRecord("Test", new SmallComplexRecord("Acme", "12-34-56", "12345678"));
        Assert.Null(ParameterValueRenderer.TryGetFlattenableProperties(obj, 10));
    }

    [Fact]
    public void TryGetFlattenableProperties_returns_null_when_exceeds_maxColumns()
    {
        var obj = new SimpleScalarRecord("UK", 100, "GBP");
        Assert.Null(ParameterValueRenderer.TryGetFlattenableProperties(obj, 2));
    }

    [Fact]
    public void TryGetFlattenableProperties_returns_null_for_array()
    {
        Assert.Null(ParameterValueRenderer.TryGetFlattenableProperties(new[] { 1, 2, 3 }, 10));
    }

    [Fact]
    public void TryGetFlattenableProperties_works_with_enum_properties()
    {
        var obj = new RecordWithEnum("Test", TestEnum.ValueA);
        var result = ParameterValueRenderer.TryGetFlattenableProperties(obj, 10);
        Assert.NotNull(result);
        Assert.Contains("Name", result!);
        Assert.Contains("Status", result);
    }

    [Fact]
    public void TryGetFlattenableProperties_works_with_nullable_properties()
    {
        var obj = new RecordWithNullable("Test", 42);
        var result = ParameterValueRenderer.TryGetFlattenableProperties(obj, 10);
        Assert.NotNull(result);
    }

    #endregion

    #region FlattenToStringValues

    [Fact]
    public void FlattenToStringValues_extracts_property_values()
    {
        var obj = new SimpleScalarRecord("UK", 100, "GBP");
        var result = ParameterValueRenderer.FlattenToStringValues(obj, ["Region", "Amount", "Currency"]);
        Assert.Equal("UK", result["Region"]);
        Assert.Equal("100", result["Amount"]);
        Assert.Equal("GBP", result["Currency"]);
    }

    #endregion

    #region RenderSubTable

    [Fact]
    public void RenderSubTable_renders_correct_html()
    {
        var obj = new SmallComplexRecord("Acme Ltd", "12-34-56", "12345678");
        var sb = new StringBuilder();
        ParameterValueRenderer.RenderSubTable(sb, obj);
        var html = sb.ToString();

        Assert.Contains("<table class=\"cell-subtable\">", html);
        Assert.Contains("<th>Name</th><td>Acme Ltd</td>", html);
        Assert.Contains("<th>SortCode</th><td>12-34-56</td>", html);
        Assert.Contains("<th>AccountNo</th><td>12345678</td>", html);
        Assert.Contains("</table>", html);
    }

    [Fact]
    public void RenderSubTable_html_encodes_values()
    {
        var obj = new SmallComplexRecord("<script>", "a&b", "\"test\"");
        var sb = new StringBuilder();
        ParameterValueRenderer.RenderSubTable(sb, obj);
        var html = sb.ToString();

        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("a&amp;b", html);
        Assert.Contains("&quot;test&quot;", html);
    }

    #endregion

    #region RenderExpandable

    [Fact]
    public void RenderExpandable_renders_details_summary()
    {
        var obj = new NestedRecord("Test Corp", new SmallComplexRecord("Acme", "12-34-56", "12345678"));
        var sb = new StringBuilder();
        ParameterValueRenderer.RenderExpandable(sb, obj);
        var html = sb.ToString();

        Assert.Contains("<details class=\"param-expand\">", html);
        Assert.Contains("<summary>", html);
        Assert.Contains("NestedRecord", html);
        Assert.Contains("<div class=\"expand-body\">", html);
        Assert.Contains("</details>", html);
    }

    [Fact]
    public void RenderExpandable_contains_highlighted_json()
    {
        var obj = new SmallComplexRecord("Acme", "12-34-56", "12345678");
        var sb = new StringBuilder();
        ParameterValueRenderer.RenderExpandable(sb, obj);
        var html = sb.ToString();

        Assert.Contains("<span class=\"prop-key\">", html);
        Assert.Contains("<span class=\"prop-val\">", html);
    }

    #endregion

    #region GeneratePreview

    [Fact]
    public void GeneratePreview_shows_type_name_and_properties()
    {
        var obj = new SimpleScalarRecord("UK", 100, "GBP");
        var preview = ParameterValueRenderer.GeneratePreview(obj);
        Assert.Contains("SimpleScalarRecord", preview);
        Assert.Contains("Region: \"UK\"", preview);
        Assert.Contains("Amount: 100", preview);
        Assert.Contains("Currency: \"GBP\"", preview);
    }

    [Fact]
    public void GeneratePreview_truncates_after_3_properties()
    {
        var obj = new LargeComplexRecord("a", "b", "c", "d", "e", "f");
        var preview = ParameterValueRenderer.GeneratePreview(obj);
        Assert.Contains("...", preview);
    }

    #endregion

    #region GenerateHighlightedJson

    [Fact]
    public void GenerateHighlightedJson_null_renders_null()
    {
        var result = ParameterValueRenderer.GenerateHighlightedJson(null, 0);
        Assert.Equal("<span class=\"prop-val\">null</span>", result);
    }

    [Fact]
    public void GenerateHighlightedJson_string_renders_quoted()
    {
        var result = ParameterValueRenderer.GenerateHighlightedJson("hello", 0);
        Assert.Contains("\"hello\"", result);
        Assert.Contains("prop-val", result);
    }

    [Fact]
    public void GenerateHighlightedJson_object_renders_properties()
    {
        var obj = new SmallComplexRecord("Acme", "12-34-56", "12345678");
        var result = ParameterValueRenderer.GenerateHighlightedJson(obj, 0);
        Assert.Contains("prop-key", result);
        Assert.Contains("\"Name\"", result);
        Assert.Contains("\"Acme\"", result);
    }

    [Fact]
    public void GenerateHighlightedJson_array_renders_items()
    {
        var arr = new[] { "a", "b", "c" };
        var result = ParameterValueRenderer.GenerateHighlightedJson(arr, 0);
        Assert.Contains("[", result);
        Assert.Contains("]", result);
        Assert.Contains("\"a\"", result);
    }

    [Fact]
    public void GenerateHighlightedJson_nested_object_renders_recursively()
    {
        var obj = new NestedRecord("Test", new SmallComplexRecord("Acme", "12-34-56", "12345678"));
        var result = ParameterValueRenderer.GenerateHighlightedJson(obj, 0);
        Assert.Contains("\"Name\"", result);
        Assert.Contains("\"Address\"", result);
        // Should contain nested object's properties
        Assert.Contains("\"SortCode\"", result);
    }

    #endregion
}
