using Kronikol.xUnit2;
using Xunit;

// Use the custom reporting test framework so reports are generated after all tests complete.
// This also captures test results (pass/fail/skip) automatically.
[assembly: Xunit.TestFramework("Kronikol.xUnit2.ReportingTestFramework", "Kronikol.xUnit2")]
