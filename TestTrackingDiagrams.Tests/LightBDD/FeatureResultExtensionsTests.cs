using LightBDD.Core.Results;
using LightBDD.Core.Formatting.NameDecorators;
using LightBDD.Core.Metadata;
using LightBDD.Core.Results.Parameters;
using TestTrackingDiagrams.LightBDD.xUnit3;
using TestTrackingDiagrams.Reports;
using FileAttachment = LightBDD.Core.Results.FileAttachment;

namespace TestTrackingDiagrams.Tests.LightBDD;

public class FeatureResultExtensionsTests
{
    [Fact]
    public void ToFeatures_maps_feature_name_and_description()
    {
        var feature = new StubFeatureResult("OrderService", "Handles orders")
            .WithScenario(new StubScenarioResult("s1", "Place order"));

        var features = new[] { feature }.ToFeatures();

        Assert.Single(features);
        Assert.Equal("OrderService", features[0].DisplayName);
        Assert.Equal("Handles orders", features[0].Description);
    }

    [Fact]
    public void ToFeatures_maps_feature_labels()
    {
        var feature = new StubFeatureResult("Svc", labels: ["api", "v2"])
            .WithScenario(new StubScenarioResult("s1", "Test"));

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(["api", "v2"], features[0].Labels);
    }

    [Fact]
    public void ToFeatures_maps_scenario_labels_and_categories()
    {
        var scenario = new StubScenarioResult("s1", "Test", labels: ["smoke"], categories: ["Orders"]);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(["smoke"], features[0].Scenarios[0].Labels);
        Assert.Equal(["Orders"], features[0].Scenarios[0].Categories);
    }

    [Fact]
    public void ToFeatures_maps_scenario_status()
    {
        var scenario = new StubScenarioResult("s1", "Test", status: ExecutionStatus.Failed, statusDetails: "Expected 200");
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ScenarioResult.Failed, features[0].Scenarios[0].Result);
        Assert.Equal("Expected 200", features[0].Scenarios[0].ErrorMessage);
    }

    [Fact]
    public void ToFeatures_maps_scenario_duration()
    {
        var scenario = new StubScenarioResult("s1", "Test", duration: TimeSpan.FromSeconds(2));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(TimeSpan.FromSeconds(2), features[0].Scenarios[0].Duration);
    }

    [Fact]
    public void ToFeatures_maps_steps_with_keyword_and_text()
    {
        var scenario = new StubScenarioResult("s1", "Test")
            .WithStep(new StubStepResult("Given", "a valid request", ExecutionStatus.Passed))
            .WithStep(new StubStepResult("When", "the request is sent", ExecutionStatus.Passed))
            .WithStep(new StubStepResult("Then", "the response is 200", ExecutionStatus.Passed));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var steps = features[0].Scenarios[0].Steps;
        Assert.NotNull(steps);
        Assert.Equal(3, steps!.Length);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("a valid request", steps[0].Text);
    }

    [Fact]
    public void ToFeatures_maps_step_status_and_duration()
    {
        var scenario = new StubScenarioResult("s1", "Test")
            .WithStep(new StubStepResult("Given", "x", ExecutionStatus.Failed, duration: TimeSpan.FromMilliseconds(500)));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];
        Assert.Equal(ScenarioResult.Failed, step.Status);
        Assert.Equal(TimeSpan.FromMilliseconds(500), step.Duration);
    }

    [Fact]
    public void ToFeatures_maps_substeps()
    {
        var subStep = new StubStepResult("And", "the body has milk", ExecutionStatus.Passed);
        var parentStep = new StubStepResult("Given", "a valid request body", ExecutionStatus.Passed)
            .WithSubStep(subStep);
        var scenario = new StubScenarioResult("s1", "Test").WithStep(parentStep);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];
        Assert.NotNull(step.SubSteps);
        Assert.Single(step.SubSteps!);
        Assert.Equal("the body has milk", step.SubSteps![0].Text);
    }

    [Fact]
    public void ToFeatures_maps_step_comments()
    {
        var step = new StubStepResult("Then", "validate", ExecutionStatus.Passed, comments: ["Business rule XYZ"]);
        var scenario = new StubScenarioResult("s1", "Test").WithStep(step);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(["Business rule XYZ"], features[0].Scenarios[0].Steps![0].Comments);
    }

    [Fact]
    public void ToFeatures_maps_file_attachments()
    {
        var attachment = new FileAttachment("screenshot", "C:\\Reports\\shot.png", "Reports/shot.png");
        var step = new StubStepResult("Then", "verify", ExecutionStatus.Passed, attachments: [attachment]);
        var scenario = new StubScenarioResult("s1", "Test").WithStep(step);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var att = features[0].Scenarios[0].Steps![0].Attachments;
        Assert.NotNull(att);
        Assert.Equal("screenshot", att![0].Name);
        Assert.Equal("Reports/shot.png", att[0].RelativePath);
    }

    [Fact]
    public void ToFeatures_maps_bypassed_and_ignored_status()
    {
        var s1 = new StubScenarioResult("s1", "Bypassed", status: ExecutionStatus.Bypassed);
        var s2 = new StubScenarioResult("s2", "Ignored", status: ExecutionStatus.Ignored);
        var feature = new StubFeatureResult("F").WithScenario(s1).WithScenario(s2);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ScenarioResult.Bypassed, features[0].Scenarios[0].Result);
        Assert.Equal(ScenarioResult.Ignored, features[0].Scenarios[1].Result);
    }

    [Fact]
    public void ToFeatures_leaves_steps_null_when_no_steps()
    {
        var scenario = new StubScenarioResult("s1", "Test");
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Steps);
    }

    // ── Stub implementations ──

    private class StubFeatureResult : IFeatureResult
    {
        private readonly List<IScenarioResult> _scenarios = [];

        public StubFeatureResult(string name, string? description = null, string[]? labels = null)
        {
            Info = new StubFeatureInfo(name, description, labels);
        }

        public IFeatureInfo Info { get; }
        public IEnumerable<IScenarioResult> GetScenarios() => _scenarios;

        public StubFeatureResult WithScenario(StubScenarioResult scenario)
        {
            _scenarios.Add(scenario);
            return this;
        }
    }

    private class StubScenarioResult : IScenarioResult
    {
        private readonly List<IStepResult> _steps = [];

        public StubScenarioResult(string id, string name,
            ExecutionStatus status = ExecutionStatus.Passed,
            string? statusDetails = null,
            TimeSpan? duration = null,
            string[]? labels = null,
            string[]? categories = null)
        {
            Info = new StubScenarioInfo(Guid.NewGuid(), name, labels, categories);
            Status = status;
            StatusDetails = statusDetails;
            ExecutionTime = duration.HasValue
                ? new ExecutionTime(DateTimeOffset.UtcNow, duration.Value)
                : null;
        }

        public IScenarioInfo Info { get; }
        public ExecutionStatus Status { get; }
        public string? StatusDetails { get; }
        public ExecutionTime? ExecutionTime { get; }
        public IEnumerable<IStepResult> GetSteps() => _steps;

        public StubScenarioResult WithStep(StubStepResult step)
        {
            _steps.Add(step);
            return this;
        }
    }

    private class StubStepResult : IStepResult
    {
        private readonly List<IStepResult> _subSteps = [];

        public StubStepResult(string? keyword, string text,
            ExecutionStatus status,
            TimeSpan? duration = null,
            string[]? comments = null,
            FileAttachment[]? attachments = null)
        {
            var fullText = keyword != null ? $"{keyword} {text}" : text;
            Info = new StubStepInfo(keyword, fullText);
            Status = status;
            ExecutionTime = duration.HasValue
                ? new ExecutionTime(DateTimeOffset.UtcNow, duration.Value)
                : null;
            Comments = comments ?? [];
            FileAttachments = attachments ?? [];
        }

        public IStepInfo Info { get; }
        public ExecutionStatus Status { get; }
        public string? StatusDetails => null;
        public ExecutionTime? ExecutionTime { get; }
        public IEnumerable<string> Comments { get; }
        public Exception? ExecutionException => null;
        public IReadOnlyList<IParameterResult> Parameters => Array.Empty<IParameterResult>();
        public IEnumerable<FileAttachment> FileAttachments { get; }
        public IEnumerable<IStepResult> GetSubSteps() => _subSteps;

        public StubStepResult WithSubStep(StubStepResult subStep)
        {
            _subSteps.Add(subStep);
            return this;
        }
    }

    private class StubFeatureInfo : IFeatureInfo
    {
        public StubFeatureInfo(string name, string? description, string[]? labels)
        {
            Name = new StubNameInfo(name);
            Description = description;
            Labels = labels ?? [];
            RuntimeId = Guid.NewGuid();
        }

        public INameInfo Name { get; }
        public Guid RuntimeId { get; }
        public string? Description { get; }
        public IEnumerable<string> Labels { get; }
    }

    private class StubScenarioInfo : IScenarioInfo
    {
        public StubScenarioInfo(Guid id, string name, string[]? labels, string[]? categories)
        {
            RuntimeId = id;
            Name = new StubNameInfo(name);
            Labels = labels ?? [];
            Categories = categories ?? [];
        }

        public INameInfo Name { get; }
        public Guid RuntimeId { get; }
        public IFeatureInfo Parent => null!;
        public IEnumerable<string> Labels { get; }
        public IEnumerable<string> Categories { get; }
        public string? Description => null;
    }

    private class StubStepInfo : IStepInfo
    {
        public StubStepInfo(string? keyword, string fullText)
        {
            Name = new StubStepNameInfo(keyword, fullText);
            RuntimeId = Guid.NewGuid();
        }

        public IStepNameInfo Name { get; }
        INameInfo IMetadataInfo.Name => Name;
        public Guid RuntimeId { get; }
        public IMetadataInfo Parent => null!;
        public string GroupPrefix => "";
        public int Number => 1;
        public int Total => 1;
    }

    private class StubStepNameInfo : IStepNameInfo
    {
        private readonly string _fullText;

        public StubStepNameInfo(string? keyword, string fullText)
        {
            StepTypeName = keyword != null ? new StubStepTypeNameInfo(keyword) : null;
            _fullText = fullText;
        }

        public IStepTypeNameInfo? StepTypeName { get; }
        public string NameFormat => _fullText;
        public IEnumerable<INameParameterInfo> Parameters => [];
        public override string ToString() => _fullText;
        public string Format(INameDecorator decorator) => _fullText;
        public string Format(IStepNameDecorator decorator) => _fullText;
    }

    private class StubStepTypeNameInfo : IStepTypeNameInfo
    {
        public StubStepTypeNameInfo(string name) { Name = name; OriginalName = name; }
        public string Name { get; }
        public string OriginalName { get; }
    }

    private class StubNameInfo : INameInfo
    {
        private readonly string _name;
        public StubNameInfo(string name) { _name = name; }
        public string NameFormat => _name;
        public IEnumerable<INameParameterInfo> Parameters => [];
        public override string ToString() => _name;
        public string Format(INameDecorator decorator) => _name;
    }
}
