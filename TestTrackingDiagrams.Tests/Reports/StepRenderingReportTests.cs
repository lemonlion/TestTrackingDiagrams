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
        Assert.DoesNotContain("<div class=\"scenario-steps\">", content);
    }

    [Fact]
    public void Report_renders_step_status_icon()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Test",
            Steps =
            [
                new ScenarioStep { Keyword = "Given", Text = "something", Status = ScenarioResult.Passed },
                new ScenarioStep { Keyword = "When", Text = "action", Status = ScenarioResult.Failed }
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
            Result = ScenarioResult.Bypassed
        };
        var content = GenerateReport(MakeFeatures(scenario), "Bypassed.html");
        Assert.Contains("data-status=\"Bypassed\"", content);
    }

    [Fact]
    public void Report_renders_ignored_status()
    {
        var scenario = new Scenario
        {
            Id = "s1", DisplayName = "Ignored test",
            Result = ScenarioResult.Ignored
        };
        var content = GenerateReport(MakeFeatures(scenario), "Ignored.html");
        Assert.Contains("data-status=\"Ignored\"", content);
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
}
