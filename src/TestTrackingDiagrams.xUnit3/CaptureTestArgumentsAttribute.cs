using System.Reflection;
using Xunit.v3;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// Captures TestMethodArguments before the test method runs.
/// xUnit3 clears these arguments after test execution, so they must be captured early
/// for report generation which happens after all tests complete.
/// </summary>
internal sealed class CaptureTestArgumentsAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        // For delay-enumerated theories ([MemberData]), args are on the Test (XunitTest).
        object[]? args = null;
        if (test is XunitTest xunitTest)
            args = xunitTest.TestMethodArguments;

        // Fall back to TestCase for [InlineData] theories.
        if (args is null or { Length: 0 } && test.TestCase is XunitTestCase testCase)
            args = testCase.TestMethodArguments;

        if (args is { Length: > 0 })
            DiagrammedComponentTest.CapturedTestMethodArguments[test.UniqueID] = args.ToArray();
    }
}
