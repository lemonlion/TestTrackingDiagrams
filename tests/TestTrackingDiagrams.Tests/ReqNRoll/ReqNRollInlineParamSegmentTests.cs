using Reqnroll;
using TestTrackingDiagrams.ReqNRoll;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.ReqNRoll;

public class ReqNRollInlineParamSegmentTests
{
    private static ScenarioStep MapSingleStep(ReqNRollStepInfo step)
    {
        var scenario = new ReqNRollScenarioInfo
        {
            ScenarioId = "s1",
            ScenarioTitle = "Test",
            FeatureTitle = "Feature",
            ScenarioTags = [],
            CombinedTags = [],
            Steps = [step],
            ExecutionStatus = ScenarioExecutionStatus.OK
        };
        return new[] { scenario }.ToFeatures()[0].Scenarios[0].Steps![0];
    }

    [Fact]
    public void Step_with_inline_params_populates_TextSegments()
    {
        // "a muffin recipe "Classic" with 5 ingredients"
        // Params: "Classic" at offset 16, "5" at offset 31
        var step = new ReqNRollStepInfo("Given",
            "a muffin recipe Classic with 5 ingredients",
            InlineParams:
            [
                new InlineParamCapture(16, 7, "Classic", "recipeName"),
                new InlineParamCapture(29, 1, "5", "ingredientCount")
            ]);

        var mapped = MapSingleStep(step);

        Assert.NotNull(mapped.TextSegments);
        Assert.Equal(5, mapped.TextSegments!.Length);
        Assert.Equal("a muffin recipe ", mapped.TextSegments[0].Text);
        Assert.Equal("Classic", mapped.TextSegments[1].Parameter!.Value);
        Assert.Equal("recipeName", mapped.TextSegments[1].ParameterName);
        Assert.Equal(" with ", mapped.TextSegments[2].Text);
        Assert.Equal("5", mapped.TextSegments[3].Parameter!.Value);
        Assert.Equal("ingredientCount", mapped.TextSegments[3].ParameterName);
        Assert.Equal(" ingredients", mapped.TextSegments[4].Text);
    }

    [Fact]
    public void Step_with_no_inline_params_has_null_TextSegments()
    {
        var step = new ReqNRollStepInfo("Given", "a valid request");
        var mapped = MapSingleStep(step);
        Assert.Null(mapped.TextSegments);
    }

    [Fact]
    public void Step_with_param_at_start_produces_correct_segments()
    {
        // "42 is the answer"
        var step = new ReqNRollStepInfo("Then", "42 is the answer",
            InlineParams: [new InlineParamCapture(0, 2, "42", "number")]);

        var mapped = MapSingleStep(step);

        Assert.NotNull(mapped.TextSegments);
        Assert.Equal(2, mapped.TextSegments!.Length);
        Assert.Equal("42", mapped.TextSegments[0].Parameter!.Value);
        Assert.Equal(" is the answer", mapped.TextSegments[1].Text);
    }

    [Fact]
    public void Step_with_param_at_end_produces_correct_segments()
    {
        // "the response status is 200"
        var step = new ReqNRollStepInfo("Then", "the response status is 200",
            InlineParams: [new InlineParamCapture(23, 3, "200", "statusCode")]);

        var mapped = MapSingleStep(step);

        Assert.NotNull(mapped.TextSegments);
        Assert.Equal(2, mapped.TextSegments!.Length);
        Assert.Equal("the response status is ", mapped.TextSegments[0].Text);
        Assert.Equal("200", mapped.TextSegments[1].Parameter!.Value);
    }

    [Fact]
    public void Inline_params_have_NotApplicable_verification_status()
    {
        var step = new ReqNRollStepInfo("Given", "user is Alice",
            InlineParams: [new InlineParamCapture(8, 5, "Alice", "name")]);

        var mapped = MapSingleStep(step);

        Assert.Equal(VerificationStatus.NotApplicable, mapped.TextSegments![1].Parameter!.Status);
    }

    [Fact]
    public void Params_without_names_produce_null_ParameterName()
    {
        var step = new ReqNRollStepInfo("When", "I wait 5 seconds",
            InlineParams: [new InlineParamCapture(7, 1, "5", null)]);

        var mapped = MapSingleStep(step);

        Assert.Null(mapped.TextSegments![1].ParameterName);
    }
}
