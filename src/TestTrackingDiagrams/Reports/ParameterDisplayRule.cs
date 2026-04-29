namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Determines how parameterized test case values are displayed in the HTML report table.
/// </summary>
public enum ParameterDisplayRule
{
    /// <summary>Fallback rendering — raw display name in a single cell (R0).</summary>
    Fallback,

    /// <summary>Each parameter occupies its own column with scalar values (R1).</summary>
    ScalarColumns,

    /// <summary>A single complex-object parameter is flattened into individual property columns (R2).</summary>
    FlattenedObject
}
