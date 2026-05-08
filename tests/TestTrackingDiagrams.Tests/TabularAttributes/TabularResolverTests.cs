using TestTrackingDiagrams.TabularAttributes;

namespace TestTrackingDiagrams.Tests.TabularAttributes;

public class TabularResolverTests
{
    private class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class Greeting
    {
        public string Message { get; set; } = "";
    }

    // --- Sample methods for reflection ---

    private class SampleMethods
    {
        [Inputs("Alice", 30)]
        [Inputs("Bob", 25)]
        public void InputsOnly(TabularInputs<Person> inputs) { }

        [HeadOut("Message")]
        [Outputs("Hello")]
        [Outputs("World")]
        public void OutputsOnly(TabularOutputs<Greeting> outputs) { }

        [Inputs("Alice", 30)]
        [Inputs("Bob", 25)]
        [HeadOut("Message")]
        [Outputs("Hello Alice")]
        [Outputs("Hello Bob")]
        public void InputsAndOutputs(
            TabularInputs<Person> inputs,
            TabularOutputs<Greeting> outputs) { }

        [Inputs("Alice", 30)]
        public void SingleRow(TabularInputs<Person> inputs) { }

        // No HeadOut — column names inferred from Greeting properties
        [Outputs("Hello")]
        public void InferredOutputColumns(TabularOutputs<Greeting> outputs) { }
    }

    private static System.Reflection.MethodInfo GetMethod(string name) =>
        typeof(SampleMethods).GetMethod(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!;

    [Fact]
    public void Resolves_TabularInputs_with_multiple_rows()
    {
        var method = GetMethod(nameof(SampleMethods.InputsOnly));

        var result = TabularResolver.Resolve(method, ["Name", "Age"]);

        Assert.Single(result);
        var inputs = Assert.IsType<TabularInputs<Person>>(result[0]);
        Assert.Equal(2, inputs.Count);
        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal(30, inputs[0].Age);
        Assert.Equal("Bob", inputs[1].Name);
        Assert.Equal(25, inputs[1].Age);
    }

    [Fact]
    public void Resolves_TabularOutputs_with_HeadOut()
    {
        var method = GetMethod(nameof(SampleMethods.OutputsOnly));

        var result = TabularResolver.Resolve(method, null);

        Assert.Single(result);
        var outputs = Assert.IsType<TabularOutputs<Greeting>>(result[0]);
        Assert.Equal(2, outputs.Count);
        Assert.Equal("Hello", outputs[0].Message);
        Assert.Equal("World", outputs[1].Message);
    }

    [Fact]
    public void Resolves_both_inputs_and_outputs()
    {
        var method = GetMethod(nameof(SampleMethods.InputsAndOutputs));

        var result = TabularResolver.Resolve(method, ["Name", "Age"]);

        Assert.Equal(2, result.Length);
        var inputs = Assert.IsType<TabularInputs<Person>>(result[0]);
        var outputs = Assert.IsType<TabularOutputs<Greeting>>(result[1]);

        Assert.Equal(2, inputs.Count);
        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal("Bob", inputs[1].Name);

        Assert.Equal(2, outputs.Count);
        Assert.Equal("Hello Alice", outputs[0].Message);
        Assert.Equal("Hello Bob", outputs[1].Message);
    }

    [Fact]
    public void Resolves_single_input_row()
    {
        var method = GetMethod(nameof(SampleMethods.SingleRow));

        var result = TabularResolver.Resolve(method, ["Name", "Age"]);

        var inputs = Assert.IsType<TabularInputs<Person>>(result[0]);
        Assert.Equal(1, inputs.Count);
        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal(30, inputs[0].Age);
    }

    [Fact]
    public void Infers_input_column_names_when_null()
    {
        var method = GetMethod(nameof(SampleMethods.InputsOnly));

        var result = TabularResolver.Resolve(method, null);

        var inputs = Assert.IsType<TabularInputs<Person>>(result[0]);
        Assert.Equal(2, inputs.Count);
        // Column names inferred from Person properties (Name, Age)
        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal(30, inputs[0].Age);
    }

    [Fact]
    public void Infers_output_column_names_when_no_HeadOut()
    {
        var method = GetMethod(nameof(SampleMethods.InferredOutputColumns));

        var result = TabularResolver.Resolve(method, null);

        var outputs = Assert.IsType<TabularOutputs<Greeting>>(result[0]);
        Assert.Equal(1, outputs.Count);
        Assert.Equal("Hello", outputs[0].Message);

        // Check that columns were inferred
        var columns = outputs.GetColumns();
        Assert.Single(columns);
        Assert.Equal("Message", columns[0].Name);
    }

    [Fact]
    public void GetColumns_on_resolved_inputs_matches_provided_names()
    {
        var method = GetMethod(nameof(SampleMethods.InputsOnly));

        var result = TabularResolver.Resolve(method, ["Name", "Age"]);

        var inputs = Assert.IsType<TabularInputs<Person>>(result[0]);
        var columns = inputs.GetColumns();
        Assert.Equal(2, columns.Length);
        Assert.Equal("Name", columns[0].Name);
        Assert.Equal("Age", columns[1].Name);
    }
}
