namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// Declares the column names for expected output data in a tabular test.
/// Each column name maps to a property of the output type <c>T</c>
/// in <see cref="TabularOutputs{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HeadOutAttribute : Attribute
{
    public string[] ColumnNames { get; }

    public HeadOutAttribute(params string[] columnNames)
    {
        ColumnNames = columnNames;
    }
}
