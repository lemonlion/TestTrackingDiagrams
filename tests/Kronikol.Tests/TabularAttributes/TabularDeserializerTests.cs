using Kronikol.TabularAttributes;

namespace Kronikol.Tests.TabularAttributes;

public class TabularDeserializerTests
{
    private class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class NullablePerson
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    }

    private enum Color { Red, Green, Blue }

    private class ColorHolder
    {
        public Color FavoriteColor { get; set; }
    }

    private class SpacedProperties
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    private class AmpersandProperties
    {
        public string NameAndAge { get; set; } = "";
    }

    private class MixedCaseProps
    {
        public string USERNAME { get; set; } = "";
        public int Score { get; set; }
    }

    private class WithDouble
    {
        public double Price { get; set; }
    }

    private class WithBool
    {
        public bool IsActive { get; set; }
    }

    [Fact]
    public void Deserializes_basic_string_and_int()
    {
        var result = TabularDeserializer.Deserialize<Person>(
            ["Name", "Age"], ["Alice", 30]);

        Assert.Equal("Alice", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void Handles_null_string_value()
    {
        var result = TabularDeserializer.Deserialize<NullablePerson>(
            ["Name", "Age"], [null, null]);

        Assert.Null(result.Name);
        Assert.Null(result.Age);
    }

    [Fact]
    public void Handles_null_value_for_value_type_defaults_to_zero()
    {
        var result = TabularDeserializer.Deserialize<Person>(
            ["Name", "Age"], ["Bob", null]);

        Assert.Equal("Bob", result.Name);
        Assert.Equal(0, result.Age);
    }

    [Fact]
    public void SanitizeName_removes_spaces()
    {
        Assert.Equal("firstname", TabularDeserializer.SanitizeName("First Name"));
    }

    [Fact]
    public void SanitizeName_replaces_ampersand_with_And()
    {
        Assert.Equal("nameandage", TabularDeserializer.SanitizeName("Name & Age"));
    }

    [Fact]
    public void SanitizeName_lowercases()
    {
        Assert.Equal("username", TabularDeserializer.SanitizeName("UserName"));
    }

    [Fact]
    public void Matches_columns_with_spaces_to_PascalCase_properties()
    {
        var result = TabularDeserializer.Deserialize<SpacedProperties>(
            ["First Name", "Last Name"], ["Jane", "Doe"]);

        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public void Matches_columns_with_ampersand_to_And_properties()
    {
        var result = TabularDeserializer.Deserialize<AmpersandProperties>(
            ["Name & Age"], ["Alice30"]);

        Assert.Equal("Alice30", result.NameAndAge);
    }

    [Fact]
    public void Matching_is_case_insensitive()
    {
        var result = TabularDeserializer.Deserialize<MixedCaseProps>(
            ["username", "score"], ["Admin", 100]);

        Assert.Equal("Admin", result.USERNAME);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Handles_enum_string_value()
    {
        var result = TabularDeserializer.Deserialize<ColorHolder>(
            ["FavoriteColor"], ["Green"]);

        Assert.Equal(Color.Green, result.FavoriteColor);
    }

    [Fact]
    public void Handles_enum_case_insensitive()
    {
        var result = TabularDeserializer.Deserialize<ColorHolder>(
            ["FavoriteColor"], ["blue"]);

        Assert.Equal(Color.Blue, result.FavoriteColor);
    }

    [Fact]
    public void Extra_columns_beyond_values_are_ignored()
    {
        var result = TabularDeserializer.Deserialize<Person>(
            ["Name", "Age"], ["OnlyName"]);

        Assert.Equal("OnlyName", result.Name);
        Assert.Equal(0, result.Age); // default
    }

    [Fact]
    public void Extra_values_beyond_columns_are_ignored()
    {
        var result = TabularDeserializer.Deserialize<Person>(
            ["Name"], ["Alice", 30]);

        Assert.Equal("Alice", result.Name);
        Assert.Equal(0, result.Age); // not set
    }

    [Fact]
    public void Unknown_column_is_ignored()
    {
        var result = TabularDeserializer.Deserialize<Person>(
            ["Name", "NonExistent"], ["Alice", "ignored"]);

        Assert.Equal("Alice", result.Name);
        Assert.Equal(0, result.Age);
    }

    [Fact]
    public void Handles_double_property()
    {
        var result = TabularDeserializer.Deserialize<WithDouble>(
            ["Price"], [19.99]);

        Assert.Equal(19.99, result.Price);
    }

    [Fact]
    public void Handles_int_to_double_conversion()
    {
        var result = TabularDeserializer.Deserialize<WithDouble>(
            ["Price"], [20]);

        Assert.Equal(20.0, result.Price);
    }

    [Fact]
    public void Handles_bool_property()
    {
        var result = TabularDeserializer.Deserialize<WithBool>(
            ["IsActive"], [true]);

        Assert.True(result.IsActive);
    }

    [Fact]
    public void Int_value_converted_to_string_via_ToString()
    {
        var result = TabularDeserializer.Deserialize<Person>(
            ["Name", "Age"], [42, 42]);

        Assert.Equal("42", result.Name); // int → string via ToString()
        Assert.Equal(42, result.Age);
    }

    // ── Record support ──────────────────────────────────────────────

    private record SimpleRecord(string Name, int Age);
    private record NullableRecord(string? Name, int? Age);
    private record RecordWithEnum(Color FavoriteColor);
    private record RecordWithSpacedNames(string FirstName, string LastName);

    [Fact]
    public void Deserializes_record_with_primary_constructor()
    {
        var result = TabularDeserializer.Deserialize<SimpleRecord>(
            ["Name", "Age"], ["Alice", 30]);

        Assert.Equal("Alice", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void Deserializes_record_with_nullable_params()
    {
        var result = TabularDeserializer.Deserialize<NullableRecord>(
            ["Name", "Age"], [null, null]);

        Assert.Null(result.Name);
        Assert.Null(result.Age);
    }

    [Fact]
    public void Deserializes_record_with_enum_param()
    {
        var result = TabularDeserializer.Deserialize<RecordWithEnum>(
            ["FavoriteColor"], ["Green"]);

        Assert.Equal(Color.Green, result.FavoriteColor);
    }

    [Fact]
    public void Deserializes_record_with_spaced_column_names()
    {
        var result = TabularDeserializer.Deserialize<RecordWithSpacedNames>(
            ["First Name", "Last Name"], ["Jane", "Doe"]);

        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public void Deserializes_record_with_missing_columns_uses_defaults()
    {
        var result = TabularDeserializer.Deserialize<SimpleRecord>(
            ["Name"], ["Alice"]);

        Assert.Equal("Alice", result.Name);
        Assert.Equal(0, result.Age); // default for int
    }
}
