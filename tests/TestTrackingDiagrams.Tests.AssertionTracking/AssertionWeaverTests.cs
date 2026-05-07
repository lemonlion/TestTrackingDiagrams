using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TestTrackingDiagrams.AssertionTracking;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.AssertionTracking;

/// <summary>
/// Tests for the AssertionWeaver IL instrumentation logic.
/// These tests compile sample assemblies, run the weaver, and verify
/// that the correct IL has been injected.
/// </summary>
public class AssertionWeaverTests
{
    [Fact]
    public void Weave_WithoutTrackAssertionsBeta_DoesNothing()
    {
        // Compile a test assembly without the attribute
        var assemblyPath = TestAssemblyBuilder.Build(
            "NoAttribute",
            """
            using FluentAssertions;
            public class Tests
            {
                public void Method() { 1.Should().Be(1); }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(0);
    }

    [Fact]
    public void Weave_OldCoreLibraryVersion_SkipsWithError()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "OldVersion",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method() { 1.Should().Be(1); }
            }
            """);

        // Add an old-version TestTrackingDiagrams assembly reference to simulate version mismatch
        using (var asmDef = AssemblyDefinition.ReadAssembly(assemblyPath,
            new ReaderParameters { ReadWrite = true, ReadSymbols = true }))
        {
            var ttdRef = asmDef.MainModule.AssemblyReferences
                .FirstOrDefault(r => r.Name == "TestTrackingDiagrams");
            if (ttdRef != null)
            {
                ttdRef.Version = new Version(2, 30, 1, 0);
            }
            else
            {
                // Attribute is defined inline, so add the reference manually with old version
                asmDef.MainModule.AssemblyReferences.Add(
                    new AssemblyNameReference("TestTrackingDiagrams", new Version(2, 30, 1, 0)));
            }
            asmDef.Write(new WriterParameters { WriteSymbols = true });
        }

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(0);
        result.SkipReason.Should().Contain("version too old");
    }

    [Fact]
    public void Weave_WithAttribute_InstrumentsShould()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "WithAttribute",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method() { 1.Should().Be(1); }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(1, $"SkipReason: {result.SkipReason}");
        result.MethodCount.Should().Be(1);
    }

    [Fact]
    public void Weave_WithSuppressOnMethod_SkipsMethod()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SuppressMethod",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                [SuppressAssertionTracking]
                public void Method() { 1.Should().Be(1); }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(0);
    }

    [Fact]
    public void Weave_WithSuppressOnClass_SkipsAllMethods()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "SuppressClass",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            [SuppressAssertionTracking]
            public class Tests
            {
                public void Method1() { 1.Should().Be(1); }
                public void Method2() { 2.Should().Be(2); }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(0);
    }

    [Fact]
    public void Weave_MultipleAssertions_InstrumentsEach()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "MultipleAssertions",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method()
                {
                    1.Should().Be(1);
                    "hello".Should().StartWith("h");
                    true.Should().BeTrue();
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(3);
        result.MethodCount.Should().Be(1);
    }

    [Fact]
    public void Weave_InstrumentedAssembly_ContainsExceptionHandlers()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "HasHandlers",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method() { 1.Should().Be(1); }
            }
            """);

        var weaver = new AssertionWeaver();
        weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        // Read the assembly back and verify exception handlers exist
        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath,
            new ReaderParameters { ReadSymbols = true });

        var method = assembly.MainModule.GetTypes()
            .First(t => t.Name == "Tests")
            .Methods.First(m => m.Name == "Method");

        method.Body.ExceptionHandlers.Should().HaveCount(1);
        method.Body.ExceptionHandlers[0].HandlerType.Should().Be(ExceptionHandlerType.Catch);
    }

    [Fact]
    public void Weave_NullPropagation_PreservesSemantics()
    {
        // The whole point of IL weaving: null propagation works correctly.
        // The ?. branch is retargeted to the leave instruction inside the try block,
        // so when Name is null the ?. short-circuits cleanly (no assertion tracked,
        // no InvalidProgramException).
        var assemblyPath = TestAssemblyBuilder.Build(
            "NullProp",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public string? Name { get; set; }
                public void Method()
                {
                    Name?.Length.Should().BeGreaterThan(0);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        // The assertion IS instrumented (not skipped)
        result.WeavedCount.Should().Be(1);

        // The weaved assembly should be loadable and executable without error
        // when Name is null (null propagation short-circuits cleanly)
        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        // Should NOT throw NullReferenceException or InvalidProgramException
        // Null propagation means .Should() never gets called — the ?. branch
        // hits the leave instruction and exits the try block cleanly
        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();
    }

    [Fact]
    public void Weave_AsyncMethod_InstrumentsAssertions()
    {
        // Synchronous assertions inside async methods should be instrumented.
        var assemblyPath = TestAssemblyBuilder.Build(
            "AsyncMethod",
            """
            using System.Threading.Tasks;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public async Task Method()
                {
                    await Task.Delay(1);
                    var x = 42;
                    x.Should().Be(42);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        // The assertion inside the async method IS instrumented
        result.WeavedCount.Should().Be(1);

        // The weaved assembly should be loadable and executable without error
        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        // Invoke the async method and await its result
        var task = (Task)method.Invoke(instance, null)!;
        var ex = Record.Exception(() => task.GetAwaiter().GetResult());
        ex.Should().BeNull();
    }

    [Fact]
    public void Weave_AsyncMethod_NoAwait_ExpressionBody_InstrumentsWithoutInvalidProgram()
    {
        // Expression-bodied async methods with no actual await (sync-over-async pattern)
        // generate a degenerate state machine. The weaver must produce valid IL for these.
        var assemblyPath = TestAssemblyBuilder.Build(
            "AsyncNoAwait",
            """
            using System.Threading.Tasks;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Steps
            {
                public string? Value { get; set; } = "hello";
            }

            public class Tests
            {
                private Steps _steps = new Steps();

                public async Task The_value_should_not_be_null()
                    => _steps.Value.Should().NotBeNull();

                public async Task The_value_should_be_hello()
                    => _steps.Value.Should().Be("hello");
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(2);

        // Critical: the weaved assembly must execute without InvalidProgramException
        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;

        var testId = $"AsyncNoAwait_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var method1 = testType.GetMethod("The_value_should_not_be_null")!;
        var task1 = (Task)method1.Invoke(instance, null)!;
        var ex1 = Record.Exception(() => task1.GetAwaiter().GetResult());
        ex1.Should().BeNull("async method with no await should not throw InvalidProgramException");

        var method2 = testType.GetMethod("The_value_should_be_hello")!;
        var task2 = (Task)method2.Invoke(instance, null)!;
        var ex2 = Record.Exception(() => task2.GetAwaiter().GetResult());
        ex2.Should().BeNull("async expression-body method should not throw InvalidProgramException");
    }

    [Fact]
    public void Weave_AsyncMethod_NoAwait_ExpressionBody_Release_InstrumentsWithoutInvalidProgram()
    {
        // Same as above but compiled with Release optimizations — the compiler generates
        // different IL (fewer nops, potentially different state machine structure).
        var assemblyPath = TestAssemblyBuilder.Build(
            "AsyncNoAwaitRelease",
            """
            using System.Threading.Tasks;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Steps
            {
                public string? Value { get; set; } = "hello";
            }

            public class Tests
            {
                private Steps _steps = new Steps();

                public async Task The_value_should_not_be_null()
                    => _steps.Value.Should().NotBeNull();

                public async Task The_value_should_be_hello()
                    => _steps.Value.Should().Be("hello");
            }
            """,
            Microsoft.CodeAnalysis.OptimizationLevel.Release);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(2);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;

        var testId = $"AsyncNoAwaitRelease_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var method1 = testType.GetMethod("The_value_should_not_be_null")!;
        var task1 = (Task)method1.Invoke(instance, null)!;
        var ex1 = Record.Exception(() => task1.GetAwaiter().GetResult());
        ex1.Should().BeNull("Release-compiled async method with no await should not throw InvalidProgramException");

        var method2 = testType.GetMethod("The_value_should_be_hello")!;
        var task2 = (Task)method2.Invoke(instance, null)!;
        var ex2 = Record.Exception(() => task2.GetAwaiter().GetResult());
        ex2.Should().BeNull("Release-compiled async expression-body method should not throw InvalidProgramException");
    }

    [Fact]
    public void Weave_AsyncMethod_MultipleAssertions_InstrumentsAll()
    {
        // Multiple assertions in an async method should all be instrumented
        var assemblyPath = TestAssemblyBuilder.Build(
            "AsyncMultiple",
            """
            using System.Threading.Tasks;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public async Task Method()
                {
                    var x = await Task.FromResult(42);
                    x.Should().Be(42);
                    
                    var y = await Task.FromResult("hello");
                    y.Should().StartWith("h");
                    y.Should().HaveLength(5);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(3);

        // Verify execution works
        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var task = (Task)method.Invoke(instance, null)!;
        var ex = Record.Exception(() => task.GetAwaiter().GetResult());
        ex.Should().BeNull();
    }

    [Fact]
    public void Weave_AsyncMethod_MultipleAssertions_Release_InstrumentsAll()
    {
        // Same as above but compiled with Release optimizations — the CI builds
        // in Release mode and the state machine IL is significantly different.
        var assemblyPath = TestAssemblyBuilder.Build(
            "AsyncMultipleRelease",
            """
            using System.Threading.Tasks;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public async Task Method()
                {
                    var x = await Task.FromResult(42);
                    x.Should().Be(42);
                    
                    var y = await Task.FromResult("hello");
                    y.Should().StartWith("h");
                    y.Should().HaveLength(5);
                }
            }
            """,
            OptimizationLevel.Release);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().Be(3);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var task = (Task)method.Invoke(instance, null)!;
        var ex = Record.Exception(() => task.GetAwaiter().GetResult());
        ex.Should().BeNull("Release-compiled async method with multiple assertions should not throw InvalidProgramException");
    }

    [Fact]
    public void Weave_WithLocalVariable_CapturesVariableForValueResolution()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "LocalVar",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method()
                {
                    var expected = 42;
                    var result = 42;
                    result.Should().Be(expected);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        // Execute and verify it calls AssertionPassedWithValues (doesn't throw)
        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        // Set up Track to capture the assertion
        var testId = $"LocalVar_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();

        // Verify the PlantUML output contains the resolved value
        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'42'", "the variable value should be resolved");
    }

    [Fact]
    public void Weave_WithMultipleVariables_CapturesAll()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "MultiVar",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method()
                {
                    var min = 1;
                    var max = 100;
                    var result = 50;
                    result.Should().BeInRange(min, max);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"MultiVar_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();

        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'1'", "min should be resolved");
        logs[0].PlantUml.Should().Contain("'100'", "max should be resolved");
    }

    [Fact]
    public void Weave_WithConstantArg_DoesNotEmitArrays()
    {
        // When all arguments are constants (like .Be(42)), no variable capture is needed
        var assemblyPath = TestAssemblyBuilder.Build(
            "ConstArg",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method()
                {
                    var x = 42;
                    x.Should().Be(42);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        // Verify assembly runs without error (using simple AssertionPassed path)
        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();
    }

    [Fact]
    public void Weave_AsyncMethod_WithVariable_CapturesStateField()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "AsyncVar",
            """
            using System.Threading.Tasks;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public async Task Method()
                {
                    var expected = "hello";
                    var result = await Task.FromResult("hello");
                    result.Should().Be(expected);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"AsyncVar_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var task = (Task)method.Invoke(instance, null)!;
        var ex = Record.Exception(() => task.GetAwaiter().GetResult());
        ex.Should().BeNull();

        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'hello'", "async variable value should be resolved from state machine field");
    }

    [Fact]
    public void Weave_WithNullVariable_ShowsNull()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "NullVar",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method()
                {
                    string? expected = null;
                    string? result = null;
                    result.Should().Be(expected);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"NullVar_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();

        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'null'", "null variable should be resolved as 'null'");
    }

    [Fact]
    public void Weave_LambdaClosure_SingleVariable_ResolvesValue()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "LambdaClosure",
            """
            using System.Collections.Generic;
            using System.Linq;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Item
            {
                public string Id { get; set; } = "";
            }

            public class Tests
            {
                public void Method()
                {
                    var expectedId = "abc";
                    var list = new List<Item> { new Item { Id = "abc" } };
                    list.Should().Contain(x => x.Id == expectedId);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"LambdaClosure_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();

        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'abc'", "closure variable should be resolved from display class field");
    }

    [Fact]
    public void Weave_LambdaClosure_MultipleVariables_ResolvesAll()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "LambdaClosureMulti",
            """
            using System.Collections.Generic;
            using System.Linq;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Item
            {
                public string Id { get; set; } = "";
                public string Foo { get; set; } = "";
            }

            public class Tests
            {
                public void Method()
                {
                    var expectedId = "abc";
                    var expectedFoo = "xyz";
                    var list = new List<Item> { new Item { Id = "abc", Foo = "xyz" } };
                    list.Should().Contain(x => x.Id == expectedId && x.Foo == expectedFoo);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"LambdaClosureMulti_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();

        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'abc'", "expectedId should be resolved");
        logs[0].PlantUml.Should().Contain("'xyz'", "expectedFoo should be resolved");
    }

    [Fact]
    public void Weave_LambdaClosure_OrCondition_ResolvesValue()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "LambdaClosureOr",
            """
            using System.Collections.Generic;
            using System.Linq;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Item
            {
                public string Id { get; set; } = "";
                public string Foo { get; set; } = "";
            }

            public class Tests
            {
                public void Method()
                {
                    var expectedId = "abc";
                    var list = new List<Item> { new Item { Id = "abc", Foo = "other" } };
                    list.Should().Contain(x => x.Id == expectedId || x.Foo == expectedId);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"LambdaClosureOr_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull();

        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'abc'", "closure variable should be resolved even with || condition");
    }

    [Fact]
    public void Weave_AsyncMethod_LambdaClosure_ResolvesValue()
    {
        var assemblyPath = TestAssemblyBuilder.Build(
            "AsyncLambdaClosure",
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Item
            {
                public string Id { get; set; } = "";
            }

            public class Tests
            {
                public async Task Method()
                {
                    var expectedId = "abc";
                    var list = await Task.FromResult(new List<Item> { new Item { Id = "abc" } });
                    list.Should().Contain(x => x.Id == expectedId);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"AsyncLambdaClosure_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var task = (Task)method.Invoke(instance, null)!;
        var ex = Record.Exception(() => task.GetAwaiter().GetResult());
        ex.Should().BeNull();

        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == testId && l.PlantUml != null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();
        logs.Should().HaveCount(1);
        logs[0].PlantUml.Should().Contain("'abc'", "async closure variable should be resolved");
    }

    [Fact]
    public void Weave_AssertionAsLastStatement_DoesNotProduceInvalidIL()
    {
        // This test verifies that when the assertion is the last statement in a method
        // (immediately followed by ret), the weaver does not place ret inside a try block.
        var assemblyPath = TestAssemblyBuilder.Build(
            "LastStatement",
            """
            using FluentAssertions;
            using TestTrackingDiagrams.Tracking;

            [assembly: TrackAssertionsBeta]

            public class Tests
            {
                public void Method()
                {
                    var x = 42;
                    x.Should().Be(42);
                }
            }
            """);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));
        result.WeavedCount.Should().Be(1);

        // Load and execute — InvalidProgramException would be thrown here if ret is inside try
        var asm = Assembly.LoadFrom(assemblyPath);
        var testType = asm.GetType("Tests")!;
        var instance = Activator.CreateInstance(testType)!;
        var method = testType.GetMethod("Method")!;

        var testId = $"LastStmt_{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        var ex = Record.Exception(() => method.Invoke(instance, null));
        ex.Should().BeNull("weaved method should run without InvalidProgramException");
    }
}
