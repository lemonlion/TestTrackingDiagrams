using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.TabularAttributes;

namespace TestTrackingDiagrams.Tests.TabularAttributes;

public class TabularInputsTests
{
    private class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [Fact]
    public void Indexer_returns_correct_item()
    {
        var items = new[] { new Person { Name = "Alice", Age = 30 } };
        var inputs = new TabularInputs<Person>(items, ["Name", "Age"]);

        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal(30, inputs[0].Age);
    }

    [Fact]
    public void Count_returns_number_of_items()
    {
        var items = new[]
        {
            new Person { Name = "Alice", Age = 30 },
            new Person { Name = "Bob", Age = 25 }
        };
        var inputs = new TabularInputs<Person>(items, ["Name", "Age"]);

        Assert.Equal(2, inputs.Count);
    }

    [Fact]
    public void GetColumns_returns_column_definitions()
    {
        var inputs = new TabularInputs<Person>([], ["Name", "Age"]);

        var columns = inputs.GetColumns();

        Assert.Equal(2, columns.Length);
        Assert.Equal("Name", columns[0].Name);
        Assert.False(columns[0].IsKey);
        Assert.Equal("Age", columns[1].Name);
        Assert.False(columns[1].IsKey);
    }

    [Fact]
    public void GetRows_returns_row_data()
    {
        var items = new[]
        {
            new Person { Name = "Alice", Age = 30 },
            new Person { Name = "Bob", Age = 25 }
        };
        var inputs = new TabularInputs<Person>(items, ["Name", "Age"]);

        var rows = inputs.GetRows();

        Assert.Equal(2, rows.Length);
        Assert.Equal(TableRowType.Matching, rows[0].Type);
        Assert.Equal("Alice", rows[0].Values[0].Value);
        Assert.Equal("30", rows[0].Values[1].Value);
        Assert.Equal(VerificationStatus.NotApplicable, rows[0].Values[0].Status);
        Assert.Equal("Bob", rows[1].Values[0].Value);
        Assert.Equal("25", rows[1].Values[1].Value);
    }

    [Fact]
    public void GetRows_returns_empty_for_no_items()
    {
        var inputs = new TabularInputs<Person>([], ["Name", "Age"]);

        var rows = inputs.GetRows();

        Assert.Empty(rows);
    }

    [Fact]
    public void Enumerator_iterates_all_items()
    {
        var items = new[]
        {
            new Person { Name = "Alice", Age = 30 },
            new Person { Name = "Bob", Age = 25 }
        };
        var inputs = new TabularInputs<Person>(items, ["Name", "Age"]);

        var enumerated = new List<Person>();
        foreach (var item in inputs)
            enumerated.Add(item);

        Assert.Equal(2, enumerated.Count);
        Assert.Equal("Alice", enumerated[0].Name);
        Assert.Equal("Bob", enumerated[1].Name);
    }

    [Fact]
    public void Enumerator_works_with_single_item()
    {
        var items = new[] { new Person { Name = "Alice", Age = 30 } };
        var inputs = new TabularInputs<Person>(items, ["Name", "Age"]);

        var count = 0;
        foreach (var _ in inputs)
            count++;

        Assert.Equal(1, count);
    }

    [Fact]
    public void Enumerator_handles_empty_collection()
    {
        var inputs = new TabularInputs<Person>([], ["Name", "Age"]);

        var count = 0;
        foreach (var _ in inputs)
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public void GetRows_handles_null_property_value()
    {
        var items = new[] { new Person { Name = null!, Age = 30 } };
        var inputs = new TabularInputs<Person>(items, ["Name", "Age"]);

        var rows = inputs.GetRows();

        Assert.Equal("null", rows[0].Values[0].Value);
    }
}
