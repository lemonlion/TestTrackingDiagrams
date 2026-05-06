using LightBDD.Core.Results;
using LightBDD.Core.Formatting.NameDecorators;
using LightBDD.Core.Metadata;
using LightBDD.Core.Results.Parameters;
using LightBDD.Core.Results.Parameters.Tabular;
using TestTrackingDiagrams.LightBDD;
using TestTrackingDiagrams.Reports;
using FileAttachment = LightBDD.Core.Results.FileAttachment;

namespace TestTrackingDiagrams.Tests.LightBDD;

public class FeatureResultExtensionsTests
{
    [Fact]
    public void ToFeatures_maps_feature_name_and_description()
    {
        var feature = new StubFeatureResult("OrderService", "Handles orders")
            .WithScenario(new StubExecutionResult("s1", "Place order"));

        var features = new[] { feature }.ToFeatures();

        Assert.Single(features);
        Assert.Equal("Order Service", features[0].DisplayName);
        Assert.Equal("Handles orders", features[0].Description);
    }

    [Fact]
    public void ToFeatures_maps_feature_labels()
    {
        var feature = new StubFeatureResult("Svc", labels: ["api", "v2"])
            .WithScenario(new StubExecutionResult("s1", "Test"));

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(["api", "v2"], features[0].Labels!);
    }

    [Fact]
    public void ToFeatures_maps_scenario_labels_and_categories()
    {
        var scenario = new StubExecutionResult("s1", "Test", labels: ["smoke"], categories: ["Orders"]);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(["smoke"], features[0].Scenarios[0].Labels!);
        Assert.Equal(["Orders"], features[0].Scenarios[0].Categories!);
    }

    [Fact]
    public void ToFeatures_maps_scenario_status()
    {
        var scenario = new StubExecutionResult("s1", "Test", status: ExecutionStatus.Failed, statusDetails: "Expected 200");
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ExecutionResult.Failed, features[0].Scenarios[0].Result);
        Assert.Equal("Expected 200", features[0].Scenarios[0].ErrorMessage);
    }

    [Fact]
    public void ToFeatures_maps_scenario_duration()
    {
        var scenario = new StubExecutionResult("s1", "Test", duration: TimeSpan.FromSeconds(2));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(TimeSpan.FromSeconds(2), features[0].Scenarios[0].Duration);
    }

    [Fact]
    public void ToFeatures_maps_steps_with_keyword_and_text()
    {
        var scenario = new StubExecutionResult("s1", "Test")
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
        var scenario = new StubExecutionResult("s1", "Test")
            .WithStep(new StubStepResult("Given", "x", ExecutionStatus.Failed, duration: TimeSpan.FromMilliseconds(500)));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];
        Assert.Equal(ExecutionResult.Failed, step.Status);
        Assert.Equal(TimeSpan.FromMilliseconds(500), step.Duration);
    }

    [Fact]
    public void ToFeatures_maps_substeps()
    {
        var subStep = new StubStepResult("And", "the body has milk", ExecutionStatus.Passed);
        var parentStep = new StubStepResult("Given", "a valid request body", ExecutionStatus.Passed)
            .WithSubStep(subStep);
        var scenario = new StubExecutionResult("s1", "Test").WithStep(parentStep);
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
        var scenario = new StubExecutionResult("s1", "Test").WithStep(step);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(["Business rule XYZ"], features[0].Scenarios[0].Steps![0].Comments!);
    }

    [Fact]
    public void ToFeatures_maps_file_attachments()
    {
        var attachment = new FileAttachment("screenshot", "C:\\Reports\\shot.png", "Reports/shot.png");
        var step = new StubStepResult("Then", "verify", ExecutionStatus.Passed, attachments: [attachment]);
        var scenario = new StubExecutionResult("s1", "Test").WithStep(step);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var att = features[0].Scenarios[0].Steps![0].Attachments;
        Assert.NotNull(att);
        Assert.Equal("screenshot", att![0].Name);
        Assert.Equal("Reports/shot.png", att[0].RelativePath);
    }

    [Fact]
    public void ToFeatures_maps_bypassed_status()
    {
        var s1 = new StubExecutionResult("s1", "Bypassed", status: ExecutionStatus.Bypassed);
        var feature = new StubFeatureResult("F").WithScenario(s1);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ExecutionResult.Bypassed, features[0].Scenarios[0].Result);
    }

    [Fact]
    public void ToFeatures_maps_ignored_scenario_to_skipped()
    {
        var s1 = new StubExecutionResult("s1", "Ignored", status: ExecutionStatus.Ignored);
        var feature = new StubFeatureResult("F").WithScenario(s1);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Result);
    }

    [Fact]
    public void ToFeatures_maps_not_run_to_skipped()
    {
        var s1 = new StubExecutionResult("s1", "NotRun", status: ExecutionStatus.NotRun);
        var feature = new StubFeatureResult("F").WithScenario(s1);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Result);
    }

    [Fact]
    public void ToFeatures_maps_not_run_steps_to_skipped_when_scenario_is_skipped()
    {
        var scenario = new StubExecutionResult("s1", "Test", status: ExecutionStatus.Ignored)
            .WithStep(new StubStepResult("Given", "a precondition", ExecutionStatus.NotRun))
            .WithStep(new StubStepResult("When", "an action", ExecutionStatus.NotRun))
            .WithStep(new StubStepResult("Then", "an assertion", ExecutionStatus.NotRun));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.All(features[0].Scenarios[0].Steps!, step =>
            Assert.Equal(ExecutionResult.Skipped, step.Status));
    }

    [Fact]
    public void ToFeatures_maps_ignored_step_to_skipped_when_no_prior_failure()
    {
        var scenario = new StubExecutionResult("s1", "Test")
            .WithStep(new StubStepResult("Given", "a valid request", ExecutionStatus.Passed))
            .WithStep(new StubStepResult("When", "something is ignored", ExecutionStatus.Ignored));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Steps![1].Status);
    }

    [Fact]
    public void ToFeatures_maps_ignored_step_to_skipped_after_failure_when_prior_step_failed()
    {
        var scenario = new StubExecutionResult("s1", "Test")
            .WithStep(new StubStepResult("Given", "a failing step", ExecutionStatus.Failed))
            .WithStep(new StubStepResult("When", "this was ignored", ExecutionStatus.Ignored))
            .WithStep(new StubStepResult("Then", "this was also ignored", ExecutionStatus.Ignored));
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(ExecutionResult.Failed, features[0].Scenarios[0].Steps![0].Status);
        Assert.Equal(ExecutionResult.SkippedAfterFailure, features[0].Scenarios[0].Steps![1].Status);
        Assert.Equal(ExecutionResult.SkippedAfterFailure, features[0].Scenarios[0].Steps![2].Status);
    }

    [Fact]
    public void ToFeatures_leaves_steps_null_when_no_steps()
    {
        var scenario = new StubExecutionResult("s1", "Test");
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Steps);
    }

    [Fact]
    public void ToFeatures_maps_error_stack_trace_from_failed_step_exception()
    {
        Exception capturedException;
        try { throw new InvalidOperationException("Something went wrong"); }
        catch (Exception ex) { capturedException = ex; }

        var step = new StubStepResult("When", "an action fails", ExecutionStatus.Failed,
            executionException: capturedException);
        var scenario = new StubExecutionResult("s1", "Test", status: ExecutionStatus.Failed, statusDetails: "Something went wrong")
            .WithStep(step);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.NotNull(features[0].Scenarios[0].ErrorStackTrace);
        Assert.Contains("Something went wrong", features[0].Scenarios[0].ErrorMessage!);
    }

    [Fact]
    public void ToFeatures_maps_error_stack_trace_from_nested_failed_substep()
    {
        Exception capturedException;
        try { throw new InvalidOperationException("Nested failure"); }
        catch (Exception ex) { capturedException = ex; }

        var subStep = new StubStepResult("And", "a nested failing action", ExecutionStatus.Failed,
            executionException: capturedException);
        var parentStep = new StubStepResult("When", "an action", ExecutionStatus.Failed)
            .WithSubStep(subStep);
        var scenario = new StubExecutionResult("s1", "Test", status: ExecutionStatus.Failed, statusDetails: "Nested failure")
            .WithStep(parentStep);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.NotNull(features[0].Scenarios[0].ErrorStackTrace);
        Assert.Contains("ToFeatures_maps_error_stack_trace_from_nested_failed_substep", features[0].Scenarios[0].ErrorStackTrace!);
    }

    [Fact]
    public void ToFeatures_leaves_error_stack_trace_null_when_scenario_passed()
    {
        var scenario = new StubExecutionResult("s1", "Test", status: ExecutionStatus.Passed);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].ErrorStackTrace);
    }

    // ─── Feature name titleization ────────────────────────────────

    [Theory]
    [InlineData("OrderService", "Order Service")]
    [InlineData("AlternativeEvidenceScenarios", "Alternative Evidence Scenarios")]
    [InlineData("Alternative Evidence Scenarios", "Alternative Evidence Scenarios")]
    [InlineData("my_feature_name", "My Feature Name")]
    public void ToFeatures_titleizes_feature_display_name(string featureName, string expected)
    {
        var feature = new StubFeatureResult(featureName)
            .WithScenario(new StubExecutionResult("s1", "Test"));

        var features = new[] { feature }.ToFeatures();
        Assert.Equal(expected, features[0].DisplayName);
    }

    // ─── Parameter extraction from NameFormat ─────────────────────

    [Fact]
    public void ToFeatures_extracts_inline_and_bracket_params_from_NameFormat()
    {
        // Simulates LightBDD NameFormat for:
        //   Method: Caller_Uses_Endpoint_With_Invalid_NewPasscode(string newPasscode, string reasonForBeingInvalid)
        //   newPasscode matched inline (case-insensitive), reasonForBeingInvalid appended as bracket
        var scenario = new StubExecutionResult("s1",
            nameFormat: "Caller Uses Endpoint With Invalid NewPasscode \"{0}\" [reasonForBeingInvalid: \"{1}\"]",
            nameParams: [new StubNameParam("111111"), new StubNameParam("TooShort")]);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var s = features[0].Scenarios[0];

        // OutlineId should be the clean base name without parameter values
        Assert.Equal("Caller Uses Endpoint With Invalid NewPasscode", s.OutlineId);

        // ExampleValues should contain both parameters
        Assert.NotNull(s.ExampleValues);
        Assert.Equal(2, s.ExampleValues!.Count);
        Assert.Equal("111111", s.ExampleValues["NewPasscode"]);
        Assert.Equal("TooShort", s.ExampleValues["reasonForBeingInvalid"]);
    }

    [Fact]
    public void ToFeatures_extracts_all_bracket_params_from_NameFormat()
    {
        // Both params unmatched in method name — both appended as brackets
        var scenario = new StubExecutionResult("s1",
            nameFormat: "Caller Uses Endpoint [version: \"{0}\"] [claimName: \"{1}\"]",
            nameParams: [new StubNameParam("V1"), new StubNameParam("LivePersonSdes")]);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var s = features[0].Scenarios[0];

        Assert.Equal("Caller Uses Endpoint", s.OutlineId);
        Assert.NotNull(s.ExampleValues);
        Assert.Equal("V1", s.ExampleValues!["version"]);
        Assert.Equal("LivePersonSdes", s.ExampleValues["claimName"]);
    }

    [Fact]
    public void ToFeatures_handles_no_params_in_NameFormat()
    {
        var scenario = new StubExecutionResult("s1", "Simple scenario with no params");
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var s = features[0].Scenarios[0];

        Assert.Null(s.OutlineId);
        Assert.Null(s.ExampleValues);
    }

    [Fact]
    public void ToFeatures_groups_parameterized_LightBDD_scenarios_by_OutlineId()
    {
        var s1 = new StubExecutionResult("s1",
            nameFormat: "Caller Uses Endpoint With Invalid NewPasscode \"{0}\" [reason: \"{1}\"]",
            nameParams: [new StubNameParam("111111"), new StubNameParam("TooShort")]);
        var s2 = new StubExecutionResult("s2",
            nameFormat: "Caller Uses Endpoint With Invalid NewPasscode \"{0}\" [reason: \"{1}\"]",
            nameParams: [new StubNameParam("1234"), new StubNameParam("TooLong")]);
        var feature = new StubFeatureResult("F").WithScenario(s1).WithScenario(s2);

        var features = new[] { feature }.ToFeatures();
        // Both scenarios should have the same OutlineId
        Assert.Equal(features[0].Scenarios[0].OutlineId, features[0].Scenarios[1].OutlineId);
        Assert.Equal("Caller Uses Endpoint With Invalid NewPasscode", features[0].Scenarios[0].OutlineId);
    }

    [Fact]
    public void MapStep_populates_TextSegments_from_NameFormat_with_params()
    {
        // NameFormat: "Given customer has \"{0}\" in account" with keyword "Given"
        var stepResult = new StubStepResult("Given", "Given customer has \"{0}\" in account",
            [new StubNameParam("105", ParameterVerificationStatus.Success)],
            ExecutionStatus.Passed)
            .WithParameters(new StubParameterResult("amount", new StubInlineDetails("105", null)));

        var scenario = new StubExecutionResult("s1", "Test").WithStep(stepResult);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];

        Assert.NotNull(step.TextSegments);
        Assert.Equal(3, step.TextSegments!.Length);
        Assert.Equal("customer has ", step.TextSegments[0].Text);
        Assert.NotNull(step.TextSegments[1].Parameter);
        Assert.Equal("105", step.TextSegments[1].Parameter!.Value);
        Assert.Equal(VerificationStatus.Success, step.TextSegments[1].Parameter!.Status);
        Assert.Equal("amount", step.TextSegments[1].ParameterName);
        Assert.Equal(" in account", step.TextSegments[2].Text);
    }

    [Fact]
    public void MapStep_TextSegments_null_when_no_name_params()
    {
        var stepResult = new StubStepResult("Given", "customer logs in", ExecutionStatus.Passed);
        var scenario = new StubExecutionResult("s1", "Test").WithStep(stepResult);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];

        Assert.Null(step.TextSegments);
    }

    [Fact]
    public void MapStep_TextSegments_maps_expectation_from_inline_parameter_details()
    {
        var stepResult = new StubStepResult("Then", "Then balance is \"{0}\"",
            [new StubNameParam("200", ParameterVerificationStatus.Failure)],
            ExecutionStatus.Failed)
            .WithParameters(new StubParameterResult("balance", new StubInlineDetails("200", "300")));

        var scenario = new StubExecutionResult("s1", "Test").WithStep(stepResult);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];

        Assert.NotNull(step.TextSegments);
        var paramSeg = step.TextSegments!.First(s => s.Parameter != null);
        Assert.Equal("200", paramSeg.Parameter!.Value);
        Assert.Equal("300", paramSeg.Parameter!.Expectation);
        Assert.Equal(VerificationStatus.Failure, paramSeg.Parameter!.Status);
    }

    [Fact]
    public void MapStep_TextSegments_emits_TableRef_for_bracket_params()
    {
        // NameFormat: "Then step verifies \"{0}\" [items: \"{1}\"]"
        // Param 0 is inline, param 1 is bracket (tabular)
        var stepResult = new StubStepResult("Then", "Then step verifies \"{0}\" [items: \"{1}\"]",
            [new StubNameParam("OK", ParameterVerificationStatus.Success), new StubNameParam("<$items>")],
            ExecutionStatus.Passed)
            .WithParameters(
                new StubParameterResult("result", new StubInlineDetails("OK", null)),
                new StubParameterResult("items", new StubTabularDetails()));

        var scenario = new StubExecutionResult("s1", "Test").WithStep(stepResult);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];

        Assert.NotNull(step.TextSegments);
        // Should have: literal "step verifies " + param "OK" + TableRef "items"
        var tableRef = step.TextSegments!.FirstOrDefault(s => s.TableReference != null);
        Assert.NotNull(tableRef);
        Assert.Equal("items", tableRef!.TableReference);
    }

    [Fact]
    public void MapStep_TextSegments_no_TableRef_when_no_bracket_params()
    {
        var stepResult = new StubStepResult("Given", "Given customer has \"{0}\" in account",
            [new StubNameParam("105", ParameterVerificationStatus.Success)],
            ExecutionStatus.Passed)
            .WithParameters(new StubParameterResult("amount", new StubInlineDetails("105", null)));

        var scenario = new StubExecutionResult("s1", "Test").WithStep(stepResult);
        var feature = new StubFeatureResult("F").WithScenario(scenario);

        var features = new[] { feature }.ToFeatures();
        var step = features[0].Scenarios[0].Steps![0];

        Assert.NotNull(step.TextSegments);
        Assert.DoesNotContain(step.TextSegments!, s => s.TableReference != null);
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

        public StubFeatureResult WithScenario(StubExecutionResult scenario)
        {
            _scenarios.Add(scenario);
            return this;
        }
    }

    private class StubExecutionResult : IScenarioResult
    {
        private readonly List<IStepResult> _steps = [];

        public StubExecutionResult(string id, string name,
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

        public StubExecutionResult(string id,
            string nameFormat,
            INameParameterInfo[] nameParams,
            ExecutionStatus status = ExecutionStatus.Passed,
            string? statusDetails = null,
            TimeSpan? duration = null,
            string[]? labels = null,
            string[]? categories = null)
        {
            Info = new StubScenarioInfo(Guid.NewGuid(), nameFormat, nameParams, labels, categories);
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

        public StubExecutionResult WithStep(StubStepResult step)
        {
            _steps.Add(step);
            return this;
        }
    }

    private class StubStepResult : IStepResult
    {
        private readonly List<IStepResult> _subSteps = [];
        private IReadOnlyList<IParameterResult> _parameters = Array.Empty<IParameterResult>();

        public StubStepResult(string? keyword, string text,
            ExecutionStatus status,
            TimeSpan? duration = null,
            string[]? comments = null,
            FileAttachment[]? attachments = null,
            Exception? executionException = null)
        {
            var fullText = keyword != null ? $"{keyword} {text}" : text;
            Info = new StubStepInfo(keyword, fullText);
            Status = status;
            ExecutionTime = duration.HasValue
                ? new ExecutionTime(DateTimeOffset.UtcNow, duration.Value)
                : null;
            Comments = comments ?? [];
            FileAttachments = attachments ?? [];
            ExecutionException = executionException;
        }

        public StubStepResult(string? keyword, string nameFormat, INameParameterInfo[] nameParams,
            ExecutionStatus status,
            TimeSpan? duration = null,
            string[]? comments = null,
            FileAttachment[]? attachments = null,
            Exception? executionException = null)
        {
            Info = new StubStepInfo(keyword, nameFormat, nameParams);
            Status = status;
            ExecutionTime = duration.HasValue
                ? new ExecutionTime(DateTimeOffset.UtcNow, duration.Value)
                : null;
            Comments = comments ?? [];
            FileAttachments = attachments ?? [];
            ExecutionException = executionException;
        }

        public IStepInfo Info { get; }
        public ExecutionStatus Status { get; }
        public string? StatusDetails => null;
        public ExecutionTime? ExecutionTime { get; }
        public IEnumerable<string> Comments { get; }
        public Exception? ExecutionException { get; }
        public IReadOnlyList<IParameterResult> Parameters => _parameters;
        public IEnumerable<FileAttachment> FileAttachments { get; }
        public IEnumerable<IStepResult> GetSubSteps() => _subSteps;

        public StubStepResult WithSubStep(StubStepResult subStep)
        {
            _subSteps.Add(subStep);
            return this;
        }

        public StubStepResult WithParameters(params IParameterResult[] parameters)
        {
            _parameters = parameters;
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

        public StubScenarioInfo(Guid id, string nameFormat, INameParameterInfo[] nameParams, string[]? labels, string[]? categories)
        {
            RuntimeId = id;
            Name = new StubNameInfo(nameFormat, nameParams);
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

        public StubStepInfo(string? keyword, string nameFormat, INameParameterInfo[] nameParams)
        {
            Name = new StubStepNameInfo(keyword, nameFormat, nameParams);
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
        private readonly INameParameterInfo[] _params;

        public StubStepNameInfo(string? keyword, string fullText) : this(keyword, fullText, []) { }

        public StubStepNameInfo(string? keyword, string nameFormat, INameParameterInfo[] nameParams)
        {
            StepTypeName = keyword != null ? new StubStepTypeNameInfo(keyword) : null;
            _fullText = nameFormat;
            _params = nameParams;
        }

        public IStepTypeNameInfo? StepTypeName { get; }
        public string NameFormat => _fullText;
        public IEnumerable<INameParameterInfo> Parameters => _params;
        public override string ToString()
        {
            if (_params.Length == 0) return _fullText;
            var result = _fullText;
            for (var i = 0; i < _params.Length; i++)
                result = result.Replace($"\"{{{i}}}\"", $"\"{_params[i].FormattedValue}\"");
            return result;
        }
        public string Format(INameDecorator decorator) => ToString();
        public string Format(IStepNameDecorator decorator) => ToString();
    }

    private class StubStepTypeNameInfo : IStepTypeNameInfo
    {
        public StubStepTypeNameInfo(string name) { Name = name; OriginalName = name; }
        public string Name { get; }
        public string OriginalName { get; }
    }

    private class StubNameInfo : INameInfo
    {
        private readonly string _nameFormat;
        private readonly INameParameterInfo[] _params;

        public StubNameInfo(string name) : this(name, []) { }

        public StubNameInfo(string nameFormat, INameParameterInfo[] nameParams)
        {
            _nameFormat = nameFormat;
            _params = nameParams;
        }

        public string NameFormat => _nameFormat;
        public IEnumerable<INameParameterInfo> Parameters => _params;

        public override string ToString()
        {
            if (_params.Length == 0) return _nameFormat;
            return string.Format(_nameFormat, _params.Select(p => (object)(p.FormattedValue ?? "")).ToArray());
        }

        public string Format(INameDecorator decorator)
        {
            if (_params.Length == 0) return decorator.DecorateNameFormat(_nameFormat);
            return string.Format(decorator.DecorateNameFormat(_nameFormat),
                _params.Select(p => (object)decorator.DecorateParameterValue(p)).ToArray());
        }
    }

    private class StubNameParam : INameParameterInfo
    {
        public StubNameParam(string value, ParameterVerificationStatus status = ParameterVerificationStatus.NotApplicable)
        {
            FormattedValue = value;
            VerificationStatus = status;
        }
        public bool IsEvaluated => true;
        public ParameterVerificationStatus VerificationStatus { get; }
        public string FormattedValue { get; }
    }

    private class StubParameterResult : IParameterResult
    {
        public StubParameterResult(string name, IParameterDetails details)
        {
            Name = name;
            Details = details;
        }
        public string Name { get; }
        public IParameterDetails Details { get; }
    }

    private class StubInlineDetails : IInlineParameterDetails
    {
        public StubInlineDetails(string value, string? expectation,
            ParameterVerificationStatus status = ParameterVerificationStatus.NotApplicable)
        {
            Value = value;
            Expectation = expectation ?? "";
            VerificationStatus = status;
        }
        public string Value { get; }
        public string Expectation { get; }
        public string VerificationMessage => "";
        public ParameterVerificationStatus VerificationStatus { get; }
    }

    private class StubTabularDetails : ITabularParameterDetails
    {
        public IReadOnlyList<ITabularParameterColumn> Columns => Array.Empty<ITabularParameterColumn>();
        public IReadOnlyList<ITabularParameterRow> Rows => Array.Empty<ITabularParameterRow>();
        public string VerificationMessage => "";
        public ParameterVerificationStatus VerificationStatus => ParameterVerificationStatus.NotApplicable;
    }
}
