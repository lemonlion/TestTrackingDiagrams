using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class StepRenderingReportTests
{
    private static Feature[] MakeFeatures(Scenario scenario) =>
    [
        new Feature
        {
            DisplayName = "Test Feature",
            Scenarios = [scenario]
        }
    ];

    private static string GenerateReport(Feature[] features, string fileName)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName, "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Report_renders_steps_when_present()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test scenario",
            Steps =
            [
                new ScenarioStep { Keyword = "Given", Text = "a valid request" },
                new ScenarioStep { Keyword = "When", Text = "the request is sent" },
                new ScenarioStep { Keyword = "Then", Text = "the response is successful" }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepRender.html");
        Assert.Contains("scenario-steps", content);
        Assert.Contains("step-keyword", content);
        Assert.Contains("Given", content);
        Assert.Contains("a valid request", content);
    }

    [Fact]
    public void Report_does_not_render_steps_section_when_no_steps()
    {
        var scenario = new Scenario { Id = "s1", DisplayName = "No steps" };
        var content = GenerateReport(MakeFeatures(scenario), "NoSteps.html");
        Assert.DoesNotContain("<details class=\"scenario-steps\"", content);
    }

    [Fact]
    public void Report_wraps_steps_in_collapsible_details_element()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test scenario",
            Steps =
            [
                new ScenarioStep { Keyword = "Given", Text = "a valid request" },
                new ScenarioStep { Keyword = "Then", Text = "the response is successful" }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "CollapsibleSteps.html");
        Assert.Contains("<details class=\"scenario-steps\"", content);
        Assert.Contains("<summary class=\"h4\">Steps</summary>", content);
    }

    [Fact]
    public void Report_steps_details_element_is_open_by_default()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test scenario",
            Steps =
            [
                new ScenarioStep { Keyword = "Given", Text = "something" }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepsOpen.html");
        Assert.Contains("<details class=\"scenario-steps\" open>", content);
    }

    [Fact]
    public void Report_scenario_steps_css_has_no_border_left()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps = [new ScenarioStep { Keyword = "Given", Text = "something" }]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepNoBorder.html");
        // The .scenario-steps CSS should not include a border-left
        var cssStart = content.IndexOf(".scenario-steps {");
        Assert.True(cssStart >= 0, ".scenario-steps CSS class should exist");
        var cssEnd = content.IndexOf("}", cssStart);
        var cssBlock = content[cssStart..cssEnd];
        Assert.DoesNotContain("border-left", cssBlock);
    }

    [Fact]
    public void Report_renders_step_status_icon()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps =
            [
                new ScenarioStep { Keyword = "Given", Text = "something", Status = ExecutionResult.Passed },
                new ScenarioStep { Keyword = "When", Text = "action", Status = ExecutionResult.Failed }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepStatus.html");
        Assert.Contains("step-status passed", content);
        Assert.Contains("step-status failed", content);
    }

    [Fact]
    public void Report_renders_step_duration()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps =
            [
                new ScenarioStep { Keyword = "Given", Text = "something", Duration = TimeSpan.FromMilliseconds(1234) }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepDuration.html");
        Assert.Contains("step-duration", content);
        Assert.Contains("1.2s", content);
    }

    [Fact]
    public void Report_renders_nested_substeps()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "a valid request body",
                    SubSteps =
                    [
                        new ScenarioStep { Keyword = "And", Text = "the body specifies milk" },
                        new ScenarioStep { Keyword = "And", Text = "the body specifies eggs" }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "SubSteps.html");
        Assert.Contains("sub-steps", content);
        Assert.Contains("step-collapsible", content);
        Assert.Contains("<summary>", content);
        Assert.Contains("the body specifies milk", content);
    }

    [Fact]
    public void Report_renders_step_comments()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Then", Text = "the result is valid",
                    Comments = ["Verifying business rule XYZ"]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepComments.html");
        Assert.Contains("step-comment", content);
        Assert.Contains("Verifying business rule XYZ", content);
    }

    [Fact]
    public void Report_renders_file_attachments()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Then", Text = "the page looks right",
                    Attachments = [new FileAttachment("screenshot.png", "Reports/screenshot.png")]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "Attachments.html");
        Assert.Contains("step-attachment", content);
        Assert.Contains("screenshot.png", content);
    }

    [Fact]
    public void Report_renders_feature_description()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Cake",
                Description = "As a dessert provider I want to create cakes",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "Test" }]
            }
        };
        var content = GenerateReport(features, "FeatureDesc.html");
        Assert.Contains("feature-description", content);
        Assert.Contains("As a dessert provider I want to create cakes", content);
    }

    [Fact]
    public void Report_renders_scenario_labels()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Labels = ["smoke", "regression"]
        };
        var content = GenerateReport(MakeFeatures(scenario), "ScenarioLabels.html");
        Assert.Contains("smoke", content);
        Assert.Contains("regression", content);
    }

    [Fact]
    public void Report_renders_feature_labels()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Cake",
                Labels = ["api", "v2"],
                Scenarios = [new Scenario { Id = "s1", DisplayName = "Test" }]
            }
        };
        var content = GenerateReport(features, "FeatureLabels.html");
        Assert.Contains("api", content);
        Assert.Contains("v2", content);
    }

    [Fact]
    public void Report_renders_bypassed_status()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Bypassed test",
            Result = ExecutionResult.Bypassed
        };
        var content = GenerateReport(MakeFeatures(scenario), "Bypassed.html");
        Assert.Contains("data-status=\"Bypassed\"", content);
    }

    [Fact]
    public void Report_renders_skipped_after_failure_status()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "SkippedAfterFailure test",
            Result = ExecutionResult.SkippedAfterFailure
        };
        var content = GenerateReport(MakeFeatures(scenario), "SkippedAfterFailure.html");
        Assert.Contains("data-status=\"SkippedAfterFailure\"", content);
    }

    [Fact]
    public void Report_renders_passed_bypassed_gradient_for_parent_with_bypassed_substeps()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Gradient test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "a composite step",
                    Status = ExecutionResult.Passed,
                    SubSteps =
                    [
                        new ScenarioStep { Keyword = "And", Text = "passed child", Status = ExecutionResult.Passed },
                        new ScenarioStep { Keyword = "And", Text = "bypassed child", Status = ExecutionResult.Bypassed }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "PassedBypassed.html");
        Assert.Contains("step-status passed-bypassed", content);
    }

    [Fact]
    public void Report_does_not_render_gradient_when_parent_passed_without_bypassed_substeps()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "No gradient test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "a composite step",
                    Status = ExecutionResult.Passed,
                    SubSteps =
                    [
                        new ScenarioStep { Keyword = "And", Text = "passed child", Status = ExecutionResult.Passed },
                        new ScenarioStep { Keyword = "And", Text = "another passed child", Status = ExecutionResult.Passed }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "NoGradient.html");
        Assert.DoesNotContain("class=\"step-status passed-bypassed\"", content);
        Assert.Contains("class=\"step-status passed\"", content);
    }

    [Fact]
    public void Report_renders_passed_skipped_for_parent_with_skipped_substeps()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Passed-skipped test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "a composite step",
                    Status = ExecutionResult.Passed,
                    SubSteps =
                    [
                        new ScenarioStep { Keyword = "And", Text = "passed child", Status = ExecutionResult.Passed },
                        new ScenarioStep { Keyword = "And", Text = "skipped child", Status = ExecutionResult.Skipped }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "PassedSkipped.html");
        Assert.Contains("step-status passed-skipped", content);
        Assert.Contains("&#10003;", content); // tick icon
    }

    [Fact]
    public void Report_renders_passed_skipped_for_deeply_nested_skipped_descendant()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Deep skipped test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "outer step",
                    Status = ExecutionResult.Passed,
                    SubSteps =
                    [
                        new ScenarioStep
                        {
                            Keyword = "And", Text = "mid step",
                            Status = ExecutionResult.Passed,
                            SubSteps =
                            [
                                new ScenarioStep { Keyword = "And", Text = "deep skipped", Status = ExecutionResult.Skipped }
                            ]
                        }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "DeepPassedSkipped.html");
        Assert.Contains("step-status passed-skipped", content);
    }

    [Fact]
    public void Report_renders_passed_skipped_when_both_bypassed_and_skipped_substeps()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Mixed bypassed+skipped test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "a composite step",
                    Status = ExecutionResult.Passed,
                    SubSteps =
                    [
                        new ScenarioStep { Keyword = "And", Text = "passed child", Status = ExecutionResult.Passed },
                        new ScenarioStep { Keyword = "And", Text = "bypassed child", Status = ExecutionResult.Bypassed },
                        new ScenarioStep { Keyword = "And", Text = "skipped child", Status = ExecutionResult.Skipped }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "MixedBypassedSkipped.html");
        // Skipped takes priority over bypassed
        Assert.Contains("step-status passed-skipped", content);
        Assert.DoesNotContain("step-status passed-bypassed", content);
    }

    [Fact]
    public void Report_does_not_render_passed_skipped_for_skipped_after_failure_substeps()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "SkippedAfterFailure not treated as skipped",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "a composite step",
                    Status = ExecutionResult.Passed,
                    SubSteps =
                    [
                        new ScenarioStep { Keyword = "And", Text = "passed child", Status = ExecutionResult.Passed },
                        new ScenarioStep { Keyword = "And", Text = "skipped-after-failure child", Status = ExecutionResult.SkippedAfterFailure }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "NotPassedSkipped.html");
        Assert.DoesNotContain("step-status passed-skipped", content);
    }

    [Fact]
    public void Report_passed_skipped_tooltip_mentions_skipped_substeps()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Tooltip test",
            Steps =
            [
                new ScenarioStep
                {
                    Keyword = "Given", Text = "a composite step",
                    Status = ExecutionResult.Passed,
                    SubSteps =
                    [
                        new ScenarioStep { Keyword = "And", Text = "passed child", Status = ExecutionResult.Passed },
                        new ScenarioStep { Keyword = "And", Text = "skipped child", Status = ExecutionResult.Skipped }
                    ]
                }
            ]
        };
        var content = GenerateReport(MakeFeatures(scenario), "SkippedTooltip.html");
        Assert.Contains("with skipped sub-steps", content);
    }

    [Fact]
    public void Report_passed_skipped_css_class_uses_grey_background()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "CSS test",
            Steps = [new ScenarioStep { Keyword = "Given", Text = "something" }]
        };
        var content = GenerateReport(MakeFeatures(scenario), "PassedSkippedCss.html");
        Assert.Contains(".step-status.passed-skipped", content);
        Assert.Contains("#949494", content); // grey — same as skipped
    }

    [Fact]
    public void Report_step_css_classes_exist()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps = [new ScenarioStep { Keyword = "Given", Text = "something" }]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepCss.html");
        Assert.Contains(".scenario-steps", content);
        Assert.Contains(".step-keyword", content);
    }

    [Fact]
    public void Report_step_status_css_has_user_select_none_and_margin_left()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps = [new ScenarioStep { Keyword = "Given", Text = "something" }]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepStatusCss.html");
        Assert.Contains("user-select: none", content);
        Assert.Contains("margin-left: 0.5em", content);
    }

    [Fact]
    public void Report_renders_category_filter_when_categories_present()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Categories = ["Orders"]
        };
        var content = GenerateReport(MakeFeatures(scenario), "CategoryFilter.html");
        Assert.Contains("category-filters", content);
        Assert.Contains("Orders", content);
    }

    [Fact]
    public void Report_does_not_render_category_filter_when_no_categories()
    {
        var scenario = new Scenario { Id = "s1", DisplayName = "Test" };
        var content = GenerateReport(MakeFeatures(scenario), "NoCategoryFilter.html");
        Assert.DoesNotContain("<div class=\"category-filters\">", content);
    }

    [Fact]
    public void Report_scenario_steps_css_has_rounded_border()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps = [new ScenarioStep { Keyword = "Given", Text = "something" }]
        };
        var content = GenerateReport(MakeFeatures(scenario), "StepBorder.html");
        var cssStart = content.IndexOf(".scenario-steps {");
        Assert.True(cssStart >= 0, ".scenario-steps CSS class should exist");
        var cssEnd = content.IndexOf("}", cssStart);
        var cssBlock = content[cssStart..cssEnd];
        Assert.Contains("border-radius: 1em", cssBlock);
        Assert.Contains("border: 1px solid", cssBlock);
        Assert.Contains("border-color: rgb(224, 224, 224)", cssBlock);
    }
}
