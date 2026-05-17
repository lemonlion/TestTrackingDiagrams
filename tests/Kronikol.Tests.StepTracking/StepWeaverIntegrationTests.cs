using System.Reflection;
using Kronikol.StepTracking;
using Kronikol.Tracking;

namespace Kronikol.Tests.StepTracking;

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
            using Kronikol.Tracking;

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
            using Kronikol.Tracking;

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
            using Kronikol.Tracking;

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
            using Kronikol.Tracking;

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
            using Kronikol.Tracking;

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

    [Fact]
    public void Weaved_ButStep_Records_Step_And_Sets_Setup_Phase()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntBut",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [ButStep]
                public void The_User_Is_Not_An_Admin() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-but-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);
        TestPhaseContext.Reset();

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("The_User_Is_Not_An_Admin")!;
        method.Invoke(instance, null);

        Assert.Equal(TestPhase.Setup, TestPhaseContext.Current);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("But", steps[0].Keyword);
        Assert.Equal("The user is not an admin", steps[0].Text);
        Assert.Equal(Reports.ExecutionResult.Passed, steps[0].Status);

        TestPhaseContext.Reset();
    }

    [Fact]
    public void Weaved_ButWhenStep_Records_Step_And_Sets_Action_Phase()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntButWhen",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [ButWhenStep]
                public void The_Api_Is_Called() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-butwhen-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);
        TestPhaseContext.Reset();

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("The_Api_Is_Called")!;
        method.Invoke(instance, null);

        Assert.Equal(TestPhase.Action, TestPhaseContext.Current);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("But", steps[0].Keyword);
        Assert.Equal("The api is called", steps[0].Text);
        Assert.Equal(Reports.ExecutionResult.Passed, steps[0].Status);

        TestPhaseContext.Reset();
    }

    [Fact]
    public async Task Weaved_Async_GivenStep_Records_Step_In_StepCollector()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntAsyncGiven",
            """
            using System.Threading.Tasks;
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [GivenStep]
                public async Task A_User_Exists()
                {
                    await Task.Delay(1);
                }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-async-given-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("A_User_Exists")!;
        var task = (Task)method.Invoke(instance, null)!;
        await task;

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("A user exists", steps[0].Text);
        Assert.Equal(Reports.ExecutionResult.Passed, steps[0].Status);
        Assert.NotNull(steps[0].Duration);
    }

    [Fact]
    public async Task Weaved_Async_Step_That_Throws_Records_Failed_Step()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntAsyncThrows",
            """
            using System;
            using System.Threading.Tasks;
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [ThenStep]
                public async Task The_Result_Is_Correct()
                {
                    await Task.Delay(1);
                    throw new InvalidOperationException("Expected 42 but got 0");
                }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-async-throws-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("The_Result_Is_Correct")!;
        var task = (Task)method.Invoke(instance, null)!;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Equal("Expected 42 but got 0", ex.Message);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("Then", steps[0].Keyword);
        Assert.Equal(Reports.ExecutionResult.Failed, steps[0].Status);
    }

    [Fact]
    public async Task Weaved_Async_Multiple_Steps_Record_Sequence()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntAsyncSequence",
            """
            using System.Threading.Tasks;
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [GivenStep]
                public async Task A_User_Exists() { await Task.Delay(1); }

                [WhenStep]
                public async Task The_User_Logs_In() { await Task.Delay(1); }

                [ThenStep]
                public async Task The_Session_Is_Created() { await Task.Delay(1); }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-async-sequence-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;

        await (Task)type.GetMethod("A_User_Exists")!.Invoke(instance, null)!;
        await (Task)type.GetMethod("The_User_Logs_In")!.Invoke(instance, null)!;
        await (Task)type.GetMethod("The_Session_Is_Created")!.Invoke(instance, null)!;

        var steps = StepCollector.GetSteps(testId);
        Assert.Equal(3, steps.Length);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("When", steps[1].Keyword);
        Assert.Equal("Then", steps[2].Keyword);
    }

    [Fact]
    public async Task Weaved_Async_Step_With_Parameters_Captures_Values()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "IntAsyncParams",
            """
            using System.Threading.Tasks;
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [GivenStep]
                public async Task A_User_With_Name(string name, int age)
                {
                    await Task.Delay(1);
                }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"int-async-params-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("A_User_With_Name")!;
        var task = (Task)method.Invoke(instance, new object[] { "Jane", 30 })!;
        await task;

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.NotNull(steps[0].Parameters);
        Assert.Equal(2, steps[0].Parameters!.Length);
        Assert.Equal("name", steps[0].Parameters![0].Name);
        Assert.Equal("Jane", steps[0].Parameters![0].InlineValue!.Value);
        Assert.Equal("age", steps[0].Parameters![1].Name);
        Assert.Equal("30", steps[0].Parameters![1].InlineValue!.Value);
    }

    [Theory]
    [InlineData(Microsoft.CodeAnalysis.OptimizationLevel.Debug)]
    [InlineData(Microsoft.CodeAnalysis.OptimizationLevel.Release)]
    public void Weaved_Void_ThenStep_With_If_Branch_DoesNotThrowInvalidProgram(Microsoft.CodeAnalysis.OptimizationLevel optimization)
    {
        // Regression test: void method with [ThenStep] containing an if-branch and
        // method calls (but no assertions). StepWeaver replaces ret with leave.
        // This pattern from BreakfastProvider caused InvalidProgramException.
        var assemblyPath = TestAssemblyBuilder.Build(
            $"VoidThenStepIf_{optimization}",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                public bool IsExternalSut { get; set; }

                [ThenStep]
                public void The_downstream_services_received_requests()
                {
                    if (!IsExternalSut)
                    {
                        AssertCowRequest();
                        AssertKitchenRequest();
                    }
                }

                private void AssertCowRequest() { }
                private void AssertKitchenRequest() { }
            }
            """,
            optimization);

        var weaver = new StepWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        Assert.Equal(1, result.WeavedCount);

        var testId = $"void-then-if-{optimization}-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("The_downstream_services_received_requests")!;
        var ex = Record.Exception(() => method.Invoke(instance, null));
        Assert.Null(ex);
    }

    [Fact]
    public void SkipIf_True_Property_Bypasses_Step()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SkipIfTrue",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                public bool GatewayUnavailable => true;

                [WhenStep(SkipIf = nameof(GatewayUnavailable), SkipReason = "Gateway down")]
                public void Payment_Is_Submitted() { throw new System.Exception("Should not execute"); }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"skipif-true-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Payment_Is_Submitted")!;
        method.Invoke(instance, null); // Should NOT throw

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal(Reports.ExecutionResult.Bypassed, steps[0].Status);
        Assert.Equal("Gateway down", steps[0].BypassReason);
        Assert.Equal("When", steps[0].Keyword);
        Assert.Equal("Payment is submitted", steps[0].Text);
    }

    [Fact]
    public void SkipIf_False_Property_Executes_Step_Normally()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SkipIfFalse",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                public bool GatewayUnavailable => false;

                [WhenStep(SkipIf = nameof(GatewayUnavailable), SkipReason = "Gateway down")]
                public void Payment_Is_Submitted() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"skipif-false-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Payment_Is_Submitted")!;
        method.Invoke(instance, null);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal(Reports.ExecutionResult.Passed, steps[0].Status);
        Assert.Null(steps[0].BypassReason);
    }

    [Fact]
    public void SkipIf_Static_Property_Works()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SkipIfStatic",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                public static bool FeatureDisabled => true;

                [GivenStep(SkipIf = nameof(FeatureDisabled), SkipReason = "Feature flag off")]
                public void Feature_Is_Enabled() { throw new System.Exception("Should not execute"); }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"skipif-static-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Feature_Is_Enabled")!;
        method.Invoke(instance, null);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal(Reports.ExecutionResult.Bypassed, steps[0].Status);
        Assert.Equal("Feature flag off", steps[0].BypassReason);
    }

    [Fact]
    public void SkipIf_Field_Works()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SkipIfField",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                public bool _skipPayment = true;

                [WhenStep(SkipIf = nameof(_skipPayment))]
                public void Payment_Is_Made() { throw new System.Exception("Should not execute"); }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"skipif-field-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Payment_Is_Made")!;
        method.Invoke(instance, null);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal(Reports.ExecutionResult.Bypassed, steps[0].Status);
        Assert.Null(steps[0].BypassReason); // No reason specified
    }

    [Fact]
    public async Task SkipIf_Async_Method_Returns_CompletedTask()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SkipIfAsync",
            """
            using System.Threading.Tasks;
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                public bool ExternalServiceDown => true;

                [WhenStep(SkipIf = nameof(ExternalServiceDown), SkipReason = "Service unavailable")]
                public async Task Call_External_Service()
                {
                    await Task.Delay(1);
                    throw new System.Exception("Should not execute");
                }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"skipif-async-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Call_External_Service")!;
        var task = (Task)method.Invoke(instance, null)!;
        await task; // Should complete immediately

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal(Reports.ExecutionResult.Bypassed, steps[0].Status);
        Assert.Equal("Service unavailable", steps[0].BypassReason);
    }

    [Fact]
    public void SkipIf_NonExistent_Member_Executes_Normally()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SkipIfMissing",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class Steps
            {
                [WhenStep(SkipIf = "NonExistentProperty")]
                public void Do_Something() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"skipif-missing-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Do_Something")!;
        method.Invoke(instance, null);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal(Reports.ExecutionResult.Passed, steps[0].Status); // Executes normally
    }

    [Fact]
    public void SkipIf_Base_Class_Property_Works()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SkipIfBase",
            """
            using Kronikol.Tracking;

            [assembly: TrackSteps]

            public class BaseSteps
            {
                public bool ServiceUnavailable { get; set; } = true;
            }

            public class Steps : BaseSteps
            {
                [WhenStep(SkipIf = nameof(ServiceUnavailable), SkipReason = "Inherited skip")]
                public void Call_Service() { throw new System.Exception("Should not execute"); }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        var testId = $"skipif-base-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("Steps")!;
        var instance = Activator.CreateInstance(type)!;
        var method = type.GetMethod("Call_Service")!;
        method.Invoke(instance, null);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal(Reports.ExecutionResult.Bypassed, steps[0].Status);
        Assert.Equal("Inherited skip", steps[0].BypassReason);
    }
}
