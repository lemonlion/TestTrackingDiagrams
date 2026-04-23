using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.TUnit;

public class StructuredParameterExtractionTests
{
    [Fact]
    public void ExtractStructured_is_accessible_from_adapter_test_project()
    {
        var result = ParameterParser.ExtractStructuredParameters(
            new object[] { "hello" },
            new[] { "greeting" });

        Assert.NotNull(result);
        Assert.Equal("hello", result!["greeting"]);
    }
}
