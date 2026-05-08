namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// Declares one row of expected output data for a tabular test.
/// Apply multiple times to define multiple expected output rows.
/// Column names are provided by the <see cref="HeadOutAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class OutputsAttribute : Attribute
{
    public object?[] Values { get; }

    public OutputsAttribute(params object?[] values)
    {
        Values = values;
    }
}
