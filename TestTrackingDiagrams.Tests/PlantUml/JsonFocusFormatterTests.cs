using System.Text.Json.Nodes;
using TestTrackingDiagrams.PlantUml;

namespace TestTrackingDiagrams.Tests.PlantUml;

public class JsonFocusFormatterTests
{
    private static string PrettyPrint(string json) => JsonNode.Parse(json)!.ToString();

    // ─── Bold emphasis ──────────────────────────────────────────

    [Fact]
    public void Bold_emphasis_wraps_focused_field_lines_in_bold_tags()
    {
        var json = PrettyPrint("""{"age":30,"name":"Alice"}""");
        // "name" is the last property so no trailing comma

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.Bold, FocusDeEmphasis.None);

        Assert.Contains("<b>\"name\": \"Alice\"</b>", result);
    }

    [Fact]
    public void Bold_emphasis_does_not_wrap_non_focused_fields()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.Bold, FocusDeEmphasis.None);

        Assert.DoesNotContain("<b>\"age\"", result);
        Assert.Contains("\"age\": 30", result);
    }

    [Fact]
    public void Bold_emphasis_on_multiple_focused_fields()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30,"email":"a@b.com"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name", "email"], FocusEmphasis.Bold, FocusDeEmphasis.None);

        Assert.Contains("<b>\"name\": \"Alice\",</b>", result);
        Assert.Contains("<b>\"email\": \"a@b.com\"</b>", result);
        Assert.DoesNotContain("<b>\"age\"", result);
    }

    // ─── Colored emphasis ───────────────────────────────────────

    [Fact]
    public void Colored_emphasis_wraps_focused_field_lines_in_color_tags()
    {
        var json = PrettyPrint("""{"age":30,"name":"Alice"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.Colored, FocusDeEmphasis.None);

        Assert.Contains("<color:blue>\"name\": \"Alice\"</color>", result);
    }

    // ─── Combined emphasis ──────────────────────────────────────

    [Fact]
    public void Bold_and_colored_emphasis_applies_both_tags()
    {
        var json = PrettyPrint("""{"age":30,"name":"Alice"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.Bold | FocusEmphasis.Colored, FocusDeEmphasis.None);

        Assert.Contains("<b><color:blue>\"name\": \"Alice\"</color></b>", result);
    }

    // ─── No emphasis ────────────────────────────────────────────

    [Fact]
    public void No_emphasis_leaves_focused_fields_unchanged()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.None);

        Assert.Contains("\"name\": \"Alice\"", result);
        Assert.DoesNotContain("<b>", result);
        Assert.DoesNotContain("<color:", result);
    }

    // ─── LightGray de-emphasis ──────────────────────────────────

    [Fact]
    public void LightGray_deemphasis_wraps_non_focused_field_lines()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.LightGray);

        Assert.Contains("<color:lightgray>\"age\": 30</color>", result);
        Assert.DoesNotContain("<color:lightgray>\"name\"", result);
    }

    // ─── SmallerText de-emphasis ────────────────────────────────

    [Fact]
    public void SmallerText_deemphasis_wraps_non_focused_field_lines()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.SmallerText);

        Assert.Contains("<size:9>\"age\": 30</size>", result);
    }

    // ─── Combined de-emphasis ───────────────────────────────────

    [Fact]
    public void LightGray_and_SmallerText_deemphasis_applies_both_tags()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.LightGray | FocusDeEmphasis.SmallerText);

        Assert.Contains("<color:lightgray><size:9>\"age\": 30</size></color>", result);
    }

    // ─── Hidden de-emphasis ─────────────────────────────────────

    [Fact]
    public void Hidden_deemphasis_replaces_non_focused_fields_with_ellipsis()
    {
        var json = PrettyPrint("""{"a":"1","name":"Alice","b":"2"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.Hidden);

        Assert.Contains("\"name\": \"Alice\"", result);
        Assert.DoesNotContain("\"a\"", result);
        Assert.DoesNotContain("\"b\"", result);
        Assert.Contains("...", result);
    }

    [Fact]
    public void Hidden_deemphasis_collapses_consecutive_non_focused_fields_into_single_ellipsis()
    {
        var json = PrettyPrint("""{"a":"1","b":"2","name":"Alice","c":"3","d":"4"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.Hidden);

        var lines = result.Split(Environment.NewLine);
        var ellipsisCount = lines.Count(l => l.TrimStart().StartsWith("..."));
        Assert.Equal(2, ellipsisCount); // one before "name", one after
    }

    [Fact]
    public void Hidden_deemphasis_with_focused_field_at_start()
    {
        var json = PrettyPrint("""{"name":"Alice","a":"1","b":"2"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.Hidden);

        Assert.Contains("\"name\": \"Alice\"", result);
        var lines = result.Split(Environment.NewLine);
        var ellipsisCount = lines.Count(l => l.TrimStart().StartsWith("..."));
        Assert.Equal(1, ellipsisCount); // only after "name"
    }

    [Fact]
    public void Hidden_deemphasis_with_focused_field_at_end()
    {
        var json = PrettyPrint("""{"a":"1","b":"2","name":"Alice"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.Hidden);

        Assert.Contains("\"name\": \"Alice\"", result);
        var lines = result.Split(Environment.NewLine);
        var ellipsisCount = lines.Count(l => l.TrimStart().StartsWith("..."));
        Assert.Equal(1, ellipsisCount); // only before "name"
    }

    // ─── Combined emphasis and de-emphasis ──────────────────────

    [Fact]
    public void Bold_emphasis_and_LightGray_deemphasis_together()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.Bold, FocusDeEmphasis.LightGray);

        Assert.Contains("<b>\"name\": \"Alice\"", result);
        Assert.Contains("<color:lightgray>\"age\": 30</color>", result);
    }

    [Fact]
    public void Bold_emphasis_and_Hidden_deemphasis_together()
    {
        var json = PrettyPrint("""{"a":"1","name":"Alice","b":"2"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.Bold, FocusDeEmphasis.Hidden);

        Assert.Contains("<b>\"name\": \"Alice\"</b>", result);
        Assert.DoesNotContain("\"a\"", result);
        Assert.DoesNotContain("\"b\"", result);
    }

    // ─── Edge cases ─────────────────────────────────────────────

    [Fact]
    public void All_fields_focused_means_no_deemphasis_applied()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name", "age"], FocusEmphasis.Bold, FocusDeEmphasis.LightGray);

        Assert.Contains("<b>\"name\": \"Alice\"", result);
        Assert.Contains("<b>\"age\": 30</b>", result);
        Assert.DoesNotContain("<color:lightgray>", result);
    }

    [Fact]
    public void No_matching_fields_returns_json_unchanged()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["nonexistent"], FocusEmphasis.Bold, FocusDeEmphasis.LightGray);

        Assert.Equal(json, result);
    }

    [Fact]
    public void Focused_field_with_nested_object_value_emphasises_all_lines()
    {
        var json = PrettyPrint("""{"name":"Alice","address":{"street":"123 Main","city":"Springfield"}}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["address"], FocusEmphasis.Bold, FocusDeEmphasis.None);

        Assert.Contains("<b>\"address\":", result);
        Assert.Contains("<b>\"street\": \"123 Main\"", result);
        Assert.Contains("<b>\"city\": \"Springfield\"</b>", result);
    }

    [Fact]
    public void Focused_field_with_array_value_emphasises_all_lines()
    {
        var json = PrettyPrint("""{"name":"Alice","tags":["admin","user"]}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["tags"], FocusEmphasis.Bold, FocusDeEmphasis.None);

        Assert.Contains("<b>\"tags\":", result);
        Assert.Contains("<b>\"admin\"", result);
        Assert.Contains("<b>\"user\"</b>", result);
    }

    [Fact]
    public void Case_insensitive_matching_handles_camelCase_property_names()
    {
        var json = PrettyPrint("""{"age":30,"name":"Alice"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["Name"], FocusEmphasis.Bold, FocusDeEmphasis.None);

        Assert.Contains("<b>\"name\": \"Alice\"</b>", result);
    }

    [Fact]
    public void Null_focus_fields_returns_json_unchanged()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, null, FocusEmphasis.Bold, FocusDeEmphasis.LightGray);

        Assert.Equal(json, result);
    }

    [Fact]
    public void Empty_focus_fields_returns_json_unchanged()
    {
        var json = PrettyPrint("""{"name":"Alice","age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, [], FocusEmphasis.Bold, FocusDeEmphasis.LightGray);

        Assert.Equal(json, result);
    }

    [Fact]
    public void Opening_and_closing_braces_are_not_formatted()
    {
        var json = PrettyPrint("""{"name":"Alice"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.Bold, FocusDeEmphasis.LightGray);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal("{", lines[0]);
        Assert.Equal("}", lines[^1]);
    }

    [Fact]
    public void Hidden_deemphasis_preserves_opening_and_closing_braces()
    {
        var json = PrettyPrint("""{"a":"1","name":"Alice","b":"2"}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.Hidden);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal("{", lines[0]);
        Assert.Equal("}", lines[^1]);
    }

    // ─── Hidden with nested objects ─────────────────────────────

    [Fact]
    public void Hidden_deemphasis_hides_nested_object_field_entirely()
    {
        var json = PrettyPrint("""{"name":"Alice","address":{"street":"123","city":"NYC"},"age":30}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["name"], FocusEmphasis.None, FocusDeEmphasis.Hidden);

        Assert.Contains("\"name\": \"Alice\"", result);
        Assert.DoesNotContain("\"address\"", result);
        Assert.DoesNotContain("\"street\"", result);
        Assert.DoesNotContain("\"age\"", result);
    }

    [Fact]
    public void Hidden_deemphasis_shows_focused_nested_object()
    {
        var json = PrettyPrint("""{"name":"Alice","address":{"street":"123","city":"NYC"}}""");

        var result = JsonFocusFormatter.FormatWithFocus(json, ["address"], FocusEmphasis.None, FocusDeEmphasis.Hidden);

        Assert.Contains("\"address\":", result);
        Assert.Contains("\"street\": \"123\"", result);
        Assert.Contains("\"city\": \"NYC\"", result);
        Assert.DoesNotContain("\"name\"", result);
    }
}
