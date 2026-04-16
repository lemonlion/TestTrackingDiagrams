using TestTrackingDiagrams.xUnit3;

namespace TestTrackingDiagrams.Tests.xUnit3;

public class DisplayNameFormatterTests
{
    public class FormatFeatureNameTests
    {
        [Theory]
        [InlineData("GetTests", "Get Tests")]
        [InlineData("Get_Tests", "Get Tests")]
        [InlineData("SimpleTest", "Simple Test")]
        [InlineData("ABCTests", "ABC Tests")]
        [InlineData("Already Spaced", "Already Spaced")]
        public void ShouldTitleizePascalCaseClassName(string input, string expected)
        {
            var result = DisplayNameFormatter.FormatFeatureName(input);

            Assert.Equal(expected, result);
        }
    }

    public class FormatScenarioDisplayNameTests
    {
        [Fact]
        public void ShouldRemoveNamespacePrefixAndHumanizeWithParameters()
        {
            var input =
                "NewDay.Digital.OpsCockpit.NewApply.Api.Tests.Component.Case.GetTests.GivenRequest_WhenGetEndpointIsCalled_ThenItWillReturnOverrideValues(overrideInput: \"1\", limitInput: \"-1\", overrideOutput: True, limitOutput: null)";

            var result = DisplayNameFormatter.FormatScenarioDisplayName(input);

            Assert.Equal(
                "Given request when get endpoint is called then it will return override values [overrideInput: \"1\", limitInput: \"-1\", overrideOutput: True, limitOutput: null]",
                result);
        }

        [Fact]
        public void ShouldHandleSimpleNameWithoutParameters()
        {
            var result = DisplayNameFormatter.FormatScenarioDisplayName("Namespace.Class.SimpleTest");

            Assert.Equal("Simple test", result);
        }

        [Fact]
        public void ShouldHandleEmptyParameters()
        {
            var result = DisplayNameFormatter.FormatScenarioDisplayName("Namespace.Class.SimpleTest()");

            Assert.Equal("Simple test", result);
        }

        [Fact]
        public void ShouldHandleMethodNameWithoutNamespace()
        {
            var result = DisplayNameFormatter.FormatScenarioDisplayName("JustMethodName");

            Assert.Equal("Just method name", result);
        }

        [Fact]
        public void ShouldHandleUnderscoreSeparatedMethodName()
        {
            var result = DisplayNameFormatter.FormatScenarioDisplayName("Ns.Class.Given_A_Request_When_Called_Then_Returns");

            Assert.Equal("Given a request when called then returns", result);
        }

        [Fact]
        public void ShouldHandleMixedPascalCaseAndUnderscores()
        {
            var result = DisplayNameFormatter.FormatScenarioDisplayName("Ns.Class.GivenRequest_WhenCalled_ThenReturns");

            Assert.Equal("Given request when called then returns", result);
        }

        [Fact]
        public void ShouldHandleParametersWithSpecialCharacters()
        {
            var result = DisplayNameFormatter.FormatScenarioDisplayName(
                "Ns.Class.TestMethod(url: \"http://example.com\", count: 5)");

            Assert.Equal("Test method [url: \"http://example.com\", count: 5]", result);
        }

        [Fact]
        public void ShouldTruncateVeryLongParameters()
        {
            var longValue = new string('x', 300);
            var result = DisplayNameFormatter.FormatScenarioDisplayName(
                $"Ns.Class.TestMethod(account: \"{longValue}\")");

            Assert.EndsWith("...]", result);
            Assert.StartsWith("Test method [", result);
            Assert.True(result.Length < 300, "Result should be significantly shorter than the raw parameter content");
        }

        [Fact]
        public void ShouldNotTruncateShortParameters()
        {
            var result = DisplayNameFormatter.FormatScenarioDisplayName(
                "Ns.Class.TestMethod(x: \"short\")");

            Assert.Equal("Test method [x: \"short\"]", result);
            Assert.DoesNotContain("...", result);
        }
    }
}
