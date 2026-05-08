using TestTrackingDiagrams.TabularAttributes;

namespace TestTrackingDiagrams.Tests.xUnit3.TabularAttributes;

public class HeadInAttributeTests
{
    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public class Greeting
    {
        public string Message { get; set; } = "";
    }

    [Theory]
    [HeadIn("Name", "Age")]
    [Inputs("Alice", 30)]
    [Inputs("Bob", 25)]
    public void Inputs_are_injected_correctly(TabularInputs<Person> inputs)
    {
        Assert.Equal(2, inputs.Count);
        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal(30, inputs[0].Age);
        Assert.Equal("Bob", inputs[1].Name);
        Assert.Equal(25, inputs[1].Age);
    }

    [Theory]
    [HeadIn("Name", "Age"), HeadOut("Message")]
    [Inputs("Alice", 30), Outputs("Hello Alice")]
    [Inputs("Bob",   25), Outputs("Hello Bob")]
    public void Inputs_and_outputs_injected_together(
        TabularInputs<Person> inputs, TabularOutputs<Greeting> outputs)
    {
        Assert.Equal(2, inputs.Count);
        Assert.Equal(2, outputs.Count);
        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal("Hello Alice", outputs[0].Message);
        Assert.Equal("Bob", inputs[1].Name);
        Assert.Equal("Hello Bob", outputs[1].Message);
    }

    [Theory]
    [HeadIn("Name", "Age")]
    [Inputs("Alice", 30)]
    public void Single_input_row(TabularInputs<Person> inputs)
    {
        Assert.Equal(1, inputs.Count);
        Assert.Equal("Alice", inputs[0].Name);
        Assert.Equal(30, inputs[0].Age);
    }

    [Theory]
    [HeadIn] // infer column names from T
    [Inputs("Charlie", 35)]
    public void Inferred_column_names(TabularInputs<Person> inputs)
    {
        Assert.Equal(1, inputs.Count);
        Assert.Equal("Charlie", inputs[0].Name);
        Assert.Equal(35, inputs[0].Age);
    }

    [Theory]
    [HeadIn("Name", "Age"), HeadOut("Message")]
    [Inputs("Alice", 30), Outputs("Hello Alice")]
    public void Output_verification_passes(
        TabularInputs<Person> inputs, TabularOutputs<Greeting> outputs)
    {
        foreach (var person in inputs)
        {
            outputs.AddActual(new Greeting { Message = $"Hello {person.Name}" });
        }
        outputs.Verify(); // Should not throw
    }

    [Theory]
    [HeadIn("Name", "Age"), HeadOut("Message")]
    [Inputs("Alice", 30), Outputs("Hello Alice")]
    public void Output_verification_fails_on_mismatch(
        TabularInputs<Person> inputs, TabularOutputs<Greeting> outputs)
    {
        foreach (var person in inputs)
        {
            outputs.AddActual(new Greeting { Message = "Wrong" });
        }
        Assert.Throws<TabularVerificationException>(() => outputs.Verify());
    }

    [Theory]
    [HeadIn("Name", "Age")]
    [Inputs("Alice", 30)]
    [Inputs("Bob", 25)]
    public void Foreach_iterates_all_rows(TabularInputs<Person> inputs)
    {
        var names = new List<string>();
        foreach (var person in inputs)
            names.Add(person.Name);

        Assert.Equal(["Alice", "Bob"], names);
    }
}
