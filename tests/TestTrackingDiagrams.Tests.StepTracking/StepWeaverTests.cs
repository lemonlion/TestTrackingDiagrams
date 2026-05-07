using Mono.Cecil;
using Mono.Cecil.Cil;
using TestTrackingDiagrams.StepTracking;

namespace TestTrackingDiagrams.Tests.StepTracking;

public class StepWeaverTests
{
    [Fact]
    public void Weave_WithoutTrackSteps_DoesNothing()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "NoStepAttr",
            """
            public class Tests
            {
                public void Method() { }
            }
            """);

        var weaver = new StepWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        Assert.Equal(0, result.WeavedCount);
        Assert.Equal("No TrackSteps attribute found", result.SkipReason);
    }

    [Fact]
    public void Weave_WithTrackSteps_NoStepMethods_DoesNothing()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "NoStepMethods",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                public void Method() { }
            }
            """);

        var weaver = new StepWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        Assert.Equal(0, result.WeavedCount);
    }

    [Fact]
    public void Weave_GivenStep_InstrumentsMethod()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "GivenStep",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [GivenStep]
                public void A_User_Exists() { }
            }
            """);

        var weaver = new StepWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        Assert.Equal(1, result.WeavedCount);

        // Verify IL contains StepCollector calls
        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var method = assembly.MainModule.GetType("Tests").Methods
            .First(m => m.Name == "A_User_Exists");

        var calls = method.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference mr && mr.DeclaringType.Name == "StepCollector")
            .Select(i => ((MethodReference)i.Operand).Name)
            .ToList();

        Assert.Contains("StartStep", calls);
        Assert.Contains("CompleteStep", calls);
    }

    [Fact]
    public void Weave_GivenStep_EmitsCorrectKeywordAndText()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "GivenStepText",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [GivenStep]
                public void TheUserLogsIn() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var method = assembly.MainModule.GetType("Tests").Methods
            .First(m => m.Name == "TheUserLogsIn");

        var ldStrings = method.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Ldstr)
            .Select(i => (string)i.Operand)
            .ToList();

        Assert.Contains("Given", ldStrings);
        Assert.Contains("The user logs in", ldStrings);
    }

    [Fact]
    public void Weave_WhenStep_EmitsWhenKeyword()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "WhenStep",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [WhenStep]
                public void TheUserSubmitsForm() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var method = assembly.MainModule.GetType("Tests").Methods
            .First(m => m.Name == "TheUserSubmitsForm");

        var ldStrings = method.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Ldstr)
            .Select(i => (string)i.Operand)
            .ToList();

        Assert.Contains("When", ldStrings);
        Assert.Contains("The user submits form", ldStrings);
    }

    [Fact]
    public void Weave_ThenStep_EmitsThenKeyword()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "ThenStep",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [ThenStep]
                public void TheResultIsCorrect() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var method = assembly.MainModule.GetType("Tests").Methods
            .First(m => m.Name == "TheResultIsCorrect");

        var ldStrings = method.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Ldstr)
            .Select(i => (string)i.Operand)
            .ToList();

        Assert.Contains("Then", ldStrings);
        Assert.Contains("The result is correct", ldStrings);
    }

    [Fact]
    public void Weave_StepAttribute_EmitsNullKeyword()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "StepAttr",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [Step]
                public void DoSomething() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var method = assembly.MainModule.GetType("Tests").Methods
            .First(m => m.Name == "DoSomething");

        // Should have ldnull before step text (null keyword)
        var instructions = method.Body.Instructions.ToList();
        var startStepCall = instructions.First(i =>
            i.OpCode == OpCodes.Call &&
            i.Operand is MethodReference mr && mr.Name == "StartStep");

        // Find the ldnull that provides the keyword (should be 4 instructions before the call)
        var startStepIdx = instructions.IndexOf(startStepCall);
        var hasLdnull = instructions.Take(startStepIdx)
            .Any(i => i.OpCode == OpCodes.Ldnull);

        Assert.True(hasLdnull);
    }

    [Fact]
    public void Weave_WithDescriptionOverride_UsesDescription()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "DescOverride",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [GivenStep(Description = "A custom step description")]
                public void SomeMethodName() { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var method = assembly.MainModule.GetType("Tests").Methods
            .First(m => m.Name == "SomeMethodName");

        var ldStrings = method.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Ldstr)
            .Select(i => (string)i.Operand)
            .ToList();

        Assert.Contains("A custom step description", ldStrings);
        Assert.DoesNotContain("Some method name", ldStrings);
    }

    [Fact]
    public void Weave_WithParameters_CapturesParameterNamesAndValues()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "WithParams",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [GivenStep]
                public void A_User_With_Name(string name, int age) { }
            }
            """);

        var weaver = new StepWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var method = assembly.MainModule.GetType("Tests").Methods
            .First(m => m.Name == "A_User_With_Name");

        var ldStrings = method.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Ldstr)
            .Select(i => (string)i.Operand)
            .ToList();

        // Parameter names should appear as string constants
        Assert.Contains("name", ldStrings);
        Assert.Contains("age", ldStrings);
    }

    [Fact]
    public void Weave_DoesNotDoubleWeave()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "DoubleWeave",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [GivenStep]
                public void A_Step() { }
            }
            """);

        var weaver = new StepWeaver();
        var result1 = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        Assert.Equal(1, result1.WeavedCount);

        var result2 = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        Assert.Equal(0, result2.WeavedCount);
        Assert.Equal("Assembly already weaved (sentinel found)", result2.SkipReason);
    }

    [Fact]
    public void Weave_MultipleStepMethods_InstrumentsAll()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "MultiStep",
            """
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackSteps]

            public class Tests
            {
                [GivenStep]
                public void A_User_Exists() { }

                [WhenStep]
                public void The_User_Logs_In() { }

                [ThenStep]
                public void The_Dashboard_Is_Shown() { }
            }
            """);

        var weaver = new StepWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        Assert.Equal(3, result.WeavedCount);
    }

    [Fact]
    public void HumanizeMethodName_PascalCase()
    {
        Assert.Equal("The user logs in", StepWeaver.HumanizeMethodName("TheUserLogsIn"));
    }

    [Fact]
    public void HumanizeMethodName_Underscores()
    {
        Assert.Equal("A user exists", StepWeaver.HumanizeMethodName("A_User_Exists"));
    }

    [Fact]
    public void HumanizeMethodName_MixedCase()
    {
        Assert.Equal("Get http response", StepWeaver.HumanizeMethodName("GetHTTPResponse"));
    }
}
