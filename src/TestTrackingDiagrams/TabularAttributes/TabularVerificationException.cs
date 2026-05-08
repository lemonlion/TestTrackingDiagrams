namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// Thrown by <see cref="TabularOutputs{T}.Verify"/> when actual output rows
/// do not match the expected rows declared via <see cref="OutputsAttribute"/>.
/// </summary>
public class TabularVerificationException : Exception
{
    public TabularVerificationException(string message) : base(message) { }
}
