namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// Declares one row of input data for a tabular test.
/// Apply multiple times to define multiple input rows.
/// Column names are provided by the framework-specific <c>[HeadIn]</c> attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InputsAttribute : Attribute
{
    public object?[] Values { get; }

    public InputsAttribute(params object?[] values)
    {
        Values = values;
    }
}
