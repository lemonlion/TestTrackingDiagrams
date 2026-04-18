using TestTrackingDiagrams.xUnit2;
using Xunit;

// Use the custom reporting test framework so reports are generated after all tests complete.
// This also captures test results (pass/fail/skip) automatically.
[assembly: Xunit.TestFramework("TestTrackingDiagrams.xUnit2.ReportingTestFramework", "TestTrackingDiagrams.xUnit2")]
