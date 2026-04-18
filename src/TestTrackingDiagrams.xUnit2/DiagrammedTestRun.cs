namespace TestTrackingDiagrams.xUnit2;

/// <summary>
/// Base class for a collection fixture that captures the test run start time.
/// Create a subclass and register it with <c>ICollectionFixture&lt;YourTestRun&gt;</c>.
/// Generate reports in your subclass's <c>Dispose()</c> method using
/// <see cref="XUnit2ReportGenerator.CreateStandardReportsWithDiagrams"/>.
/// <para>
/// <b>Note:</b> When using the <see cref="ReportingTestFramework"/> (recommended),
/// reports are generated automatically after all tests complete, and this class
/// is not strictly required. It remains useful for starting/stopping HTTP fakes
/// and other shared test resources.
/// </para>
/// </summary>
public class DiagrammedTestRun
{
    protected static DateTime StartRunTime { get; private set; }
    protected static DateTime EndRunTime { get; set; }

    public DiagrammedTestRun()
    {
        StartRunTime = DateTime.UtcNow;
    }
}
