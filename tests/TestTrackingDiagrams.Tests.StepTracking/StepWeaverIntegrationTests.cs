using System.Reflection;
using TestTrackingDiagrams.StepTracking;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.StepTracking;

/// <summary>
/// Integration tests that compile, weave, load, and execute step-attributed methods
/// to verify the full pipeline works end-to-end with StepCollector.
/// </summary>
public class StepWeaverIntegrationTests
{
    [Fact]
    public void Weaved_GivenStep_Records_Step_In_StepCollector()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntGiven",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [GivenStep]
                public void A_User_Exists() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-given-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        // Load and invoke the weaved method
        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("A_User_Exists")!;
        method.Invoke(instance, null);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("A user exists", steps[0].Text);
        Assert.Equal(Reports.ExecutionResult.Passed, steps[0].Status);
        Assert.NotNull(steps[0].Duration);
        Assert.True(steps[0].Duration!.Value.TotalMilliseconds >= 0);
    }

    [Fact]
    public void Weaved_WhenStep_Records_Step_And_Sets_Action_Phase()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntWhen",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [WhenStep]
                public void The_User_Logs_In() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-when-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);
        TestPhaseContext.Reset();

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("The_User_Logs_In")!;
        method.Invoke(instance, null);

        Assert.Equal(TestPhase.Action, TestPhaseContext.Current);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("When", steps[0].Keyword);
        Assert.Equal("The user logs in", steps[0].Text);

        TestPhaseContext.Reset();
    }

    [Fact]
    public void Weaved_Step_With_Parameters_Captures_Values()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntParams",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [GivenStep]
                public void A_User_With_Name(string name, int age) { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-params-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("A_User_With_Name")!;
        method.Invoke(instance, new object[] { "John", 42 });

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.NotNull(steps[0].Parameters);
        Assert.Equal(2, steps[0].Parameters!.Length);
        Assert.Equal("name", steps[0].Parameters![0].Name);
        Assert.Equal("John", steps[0].Parameters![0].InlineValue!.Value);
        Assert.Equal("age", steps[0].Parameters![1].Name);
        Assert.Equal("42", steps[0].Parameters![1].InlineValue!.Value);
    }

    [Fact]
    public void Weaved_Step_That_Throws_Records_Failed_Step()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntThrows",
            """
            using System;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [ThenStep]
                public void The_Result_Is_Correct()
                {
                    throw new InvalidOperationException("Expected 42 but got 0");
                }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-throws-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("The_Result_Is_Correct")!;

        // The exception should still propagate
        var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(instance, null));
        Assert.IsType<InvalidOperationException>(ex.InnerException);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("Then", steps[0].Keyword);
        Assert.Equal(Reports.ExecutionResult.Failed, steps[0].Status);
    }

    [Fact]
    public void Weaved_Multiple_Steps_Record_Sequence()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntSequence",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [GivenStep]
                public void A_User_Exists() { }

                [GivenStep]
                public void The_User_Has_A_Session() { }

                [WhenStep]
                public void The_User_Logs_Out() { }

                [ThenStep]
                public void The_Session_Is_Destroyed() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-sequence-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;

        type.GetMethod("A_User_Exists")!.Invoke(instance, null);
        type.GetMethod("The_User_Has_A_Session")!.Invoke(instance, null);
        type.GetMethod("The_User_Logs_Out")!.Invoke(instance, null);
        type.GetMethod("The_Session_Is_Destroyed")!.Invoke(instance, null);

        var steps = StepCollector.GetSteps(testId);
        Assert.Equal(4, steps.Length);

        // Keyword sequencing: Given, And (second Given becomes And), When, Then
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("And", steps[1].Keyword);
        Assert.Equal("When", steps[2].Keyword);
        Assert.Equal("Then", steps[3].Keyword);
    }
}
