using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class StepTextSegmentRenderingTests
{
    private static string GenerateReport(Feature[] features)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "TextSegments.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    private static Feature[] FeaturesWithStep(ScenarioStep step) =>
    [
        new Feature
        {
            DisplayName = "F1",
            Scenarios =
            [
                new Scenario
                {
                    Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed,
                    Steps = [step]
                }
            ]
        }
    ];

    [Fact]
    public void TextSegments_renders_inline_param_within_step_text()
    {
        var step = new ScenarioStep
        {
            Keyword = "Given",
            Text = "customer has \"105\" in account",
            Status = ExecutionResult.Passed,
            TextSegments =
            [
                StepTextSegment.Literal("customer has "),
                StepTextSegment.Param("amount", new InlineParameterValue("105", null, VerificationStatus.Success)),
                StepTextSegment.Literal(" in account")
            ]
        };

        var content = GenerateReport(FeaturesWithStep(step));

        // The parameter should be embedded WITHIN the step text span area, not as a separate badge
        Assert.Contains("customer has ", content);
        Assert.Contains("step-param-inline", content);
        Assert.Contains(">105<", content);
        Assert.Contains("param-success", content);
        Assert.Contains(" in account", content);
    }

    [Fact]
    public void TextSegments_renders_failed_verification_inline()
    {
        var step = new ScenarioStep
        {
            Keyword = "Then",
            Text = "price should be \"99/100\"",
            Status = ExecutionResult.Failed,
            TextSegments =
            [
                StepTextSegment.Literal("price should be "),
                StepTextSegment.Param("price", new InlineParameterValue("99", "100", VerificationStatus.Failure))
            ]
        };

        var content = GenerateReport(FeaturesWithStep(step));

        Assert.Contains("param-failure", content);
        Assert.Contains("99/100", content);
    }

    [Fact]
    public void TextSegments_renders_multiple_params_inline()
    {
        var step = new ScenarioStep
        {
            Keyword = "When",
            Text = "user \"Alice\" buys \"3\" items",
            Status = ExecutionResult.Passed,
            TextSegments =
            [
                StepTextSegment.Literal("user "),
                StepTextSegment.Param("name", new InlineParameterValue("Alice", null, VerificationStatus.NotApplicable)),
                StepTextSegment.Literal(" buys "),
                StepTextSegment.Param("count", new InlineParameterValue("3", null, VerificationStatus.NotApplicable)),
                StepTextSegment.Literal(" items")
            ]
        };

        var content = GenerateReport(FeaturesWithStep(step));

        // Both params should be inline
        Assert.Contains(">Alice<", content);
        Assert.Contains(">3<", content);
        // Should NOT contain the old-style step-text with quoted values
        Assert.DoesNotContain("&quot;Alice&quot;", content);
    }

    [Fact]
    public void TextSegments_param_has_name_tooltip()
    {
        var step = new ScenarioStep
        {
            Keyword = "Given",
            Text = "balance is \"500\"",
            Status = ExecutionResult.Passed,
            TextSegments =
            [
                StepTextSegment.Literal("balance is "),
                StepTextSegment.Param("amount", new InlineParameterValue("500", null, VerificationStatus.Success))
            ]
        };

        var content = GenerateReport(FeaturesWithStep(step));

        Assert.Contains("title=\"amount\"", content);
    }

    [Fact]
    public void TextSegments_null_falls_back_to_plain_text()
    {
        var step = new ScenarioStep
        {
            Keyword = "Given",
            Text = "a plain step",
            Status = ExecutionResult.Passed,
            TextSegments = null
        };

        var content = GenerateReport(FeaturesWithStep(step));

        Assert.Contains("a plain step", content);
    }

    [Fact]
    public void TextSegments_html_encodes_literal_text()
    {
        var step = new ScenarioStep
        {
            Keyword = "Given",
            Text = "value <script>xss</script>",
            Status = ExecutionResult.Passed,
            TextSegments =
            [
                StepTextSegment.Literal("value <script>xss</script>")
            ]
        };

        var content = GenerateReport(FeaturesWithStep(step));

        Assert.Contains("&lt;script&gt;xss&lt;/script&gt;", content);
        Assert.DoesNotContain("<script>xss</script>", content);
    }

    [Fact]
    public void TextSegments_html_encodes_param_value()
    {
        var step = new ScenarioStep
        {
            Keyword = "Given",
            Text = "input is \"<b>bold</b>\"",
            Status = ExecutionResult.Passed,
            TextSegments =
            [
                StepTextSegment.Literal("input is "),
                StepTextSegment.Param("val", new InlineParameterValue("<b>bold</b>", null, VerificationStatus.NotApplicable))
            ]
        };

        var content = GenerateReport(FeaturesWithStep(step));

        Assert.Contains("&lt;b&gt;bold&lt;/b&gt;", content);
        Assert.DoesNotContain("<b>bold</b>", content);
    }

    [Fact]
    public void TextSegments_does_not_render_separate_inline_param_badges()
    {
        // When TextSegments is used, inline parameters should NOT also appear as separate badges
        var step = new ScenarioStep
        {
            Keyword = "Given",
            Text = "customer has \"105\" in account",
            Status = ExecutionResult.Passed,
            TextSegments =
            [
                StepTextSegment.Literal("customer has "),
                StepTextSegment.Param("amount", new InlineParameterValue("105", null, VerificationStatus.Success)),
                StepTextSegment.Literal(" in account")
            ],
            Parameters =
            [
                new StepParameter
                {
                    Name = "amount",
                    Kind = StepParameterKind.Inline,
                    InlineValue = new InlineParameterValue("105", null, VerificationStatus.Success)
                }
            ]
        };

        var content = GenerateReport(FeaturesWithStep(step));

        // Count occurrences of rendered param spans - should be exactly 1 (inline in text), not 2
        var paramCount = content.Split("<span class=\"step-param-inline").Length - 1;
        Assert.Equal(1, paramCount);
    }
}
