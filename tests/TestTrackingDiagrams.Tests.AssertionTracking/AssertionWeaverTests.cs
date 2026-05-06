using System.Reflection;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TestTrackingDiagrams.AssertionTracking;

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
}
