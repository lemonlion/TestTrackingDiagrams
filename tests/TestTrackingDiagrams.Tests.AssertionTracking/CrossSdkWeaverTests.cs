using System.Reflection;
using System.Runtime.Loader;
using FluentAssertions;
using TestTrackingDiagrams.AssertionTracking;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.AssertionTracking;

/// <summary>
/// Tests that the IL weaver produces valid IL from assemblies compiled by different
/// .NET SDK versions (different Roslyn codegen patterns — especially async state machines
/// and null-conditional branches).
/// </summary>
public class CrossSdkWeaverTests
{
    // .NET 11 preview installed to local user directory
    private static readonly string? DotNet11Path = FindDotNet11();

    private static string? FindDotNet11()
    {
        var localPreview = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dotnet-preview", "dotnet.exe");
        if (File.Exists(localPreview) && TestAssemblyBuilder.IsSdkAvailable("11.0", localPreview))
            return localPreview;
        if (TestAssemblyBuilder.IsSdkAvailable("11.0"))
            return null; // use default dotnet
        return null;
    }

    private static bool HasNet11 => DotNet11Path != null || TestAssemblyBuilder.IsSdkAvailable("11.0");

    private const string AsyncNoAwaitSource = """
        using System.Threading.Tasks;
        using FluentAssertions;
        using TestTrackingDiagrams.Tracking;

        [assembly: TrackAssertions]

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
        """;

    private const string AsyncWithAwaitSource = """
        using System.Threading.Tasks;
        using FluentAssertions;
        using TestTrackingDiagrams.Tracking;

        [assembly: TrackAssertions]

        public class Tests
        {
            public async Task Delayed_assertion()
            {
                await Task.Delay(1);
                42.Should().Be(42);
            }

            public async Task Multiple_assertions_with_await()
            {
                await Task.Yield();
                1.Should().Be(1);
                await Task.Yield();
                2.Should().Be(2);
            }
        }
        """;

    private const string NullConditionalSource = """
        using System.Threading.Tasks;
        using FluentAssertions;
        using TestTrackingDiagrams.Tracking;

        [assembly: TrackAssertions]

        public class Order
        {
            public string? Status { get; set; }
        }

        public class Tests
        {
            public async Task Null_conditional_assertion()
            {
                var order = new Order { Status = "Shipped" };
                await Task.Yield();
                order?.Status.Should().Be("Shipped");
            }

            public async Task Null_conditional_with_null_value()
            {
                Order? order = null;
                await Task.Yield();
                order?.Status.Should().Be("anything");
            }
        }
        """;

    // ========== .NET 9 SDK tests ==========

    [Fact]
    public void Net9_Debug_AsyncNoAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("9.0");
        AssertWeaveAndExecute("Net9AsyncNoAwaitDebug", AsyncNoAwaitSource,
            "9.0.0", "net9.0", "Debug",
            ["The_value_should_not_be_null", "The_value_should_be_hello"]);
    }

    [Fact]
    public void Net9_Release_AsyncNoAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("9.0");
        AssertWeaveAndExecute("Net9AsyncNoAwaitRelease", AsyncNoAwaitSource,
            "9.0.0", "net9.0", "Release",
            ["The_value_should_not_be_null", "The_value_should_be_hello"]);
    }

    [Fact]
    public void Net9_Debug_AsyncWithAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("9.0");
        AssertWeaveAndExecute("Net9AsyncWithAwaitDebug", AsyncWithAwaitSource,
            "9.0.0", "net9.0", "Debug",
            ["Delayed_assertion", "Multiple_assertions_with_await"]);
    }

    [Fact]
    public void Net9_Release_AsyncWithAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("9.0");
        AssertWeaveAndExecute("Net9AsyncWithAwaitRelease", AsyncWithAwaitSource,
            "9.0.0", "net9.0", "Release",
            ["Delayed_assertion", "Multiple_assertions_with_await"]);
    }

    [Fact]
    public void Net9_Debug_NullConditional_WeaveAndExecute()
    {
        SkipIfSdkMissing("9.0");
        AssertWeaveAndExecute("Net9NullCondDebug", NullConditionalSource,
            "9.0.0", "net9.0", "Debug",
            ["Null_conditional_assertion", "Null_conditional_with_null_value"],
            expectAllPass: false);
    }

    [Fact]
    public void Net9_Release_NullConditional_WeaveAndExecute()
    {
        SkipIfSdkMissing("9.0");
        AssertWeaveAndExecute("Net9NullCondRelease", NullConditionalSource,
            "9.0.0", "net9.0", "Release",
            ["Null_conditional_assertion", "Null_conditional_with_null_value"],
            expectAllPass: false);
    }

    // ========== .NET 10 SDK tests ==========

    [Fact]
    public void Net10_Debug_AsyncNoAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("10.0");
        AssertWeaveAndExecute("Net10AsyncNoAwaitDebug", AsyncNoAwaitSource,
            "10.0.0", "net10.0", "Debug",
            ["The_value_should_not_be_null", "The_value_should_be_hello"]);
    }

    [Fact]
    public void Net10_Release_AsyncNoAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("10.0");
        AssertWeaveAndExecute("Net10AsyncNoAwaitRelease", AsyncNoAwaitSource,
            "10.0.0", "net10.0", "Release",
            ["The_value_should_not_be_null", "The_value_should_be_hello"]);
    }

    [Fact]
    public void Net10_Debug_AsyncWithAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("10.0");
        AssertWeaveAndExecute("Net10AsyncWithAwaitDebug", AsyncWithAwaitSource,
            "10.0.0", "net10.0", "Debug",
            ["Delayed_assertion", "Multiple_assertions_with_await"]);
    }

    [Fact]
    public void Net10_Release_AsyncWithAwait_WeaveAndExecute()
    {
        SkipIfSdkMissing("10.0");
        AssertWeaveAndExecute("Net10AsyncWithAwaitRelease", AsyncWithAwaitSource,
            "10.0.0", "net10.0", "Release",
            ["Delayed_assertion", "Multiple_assertions_with_await"]);
    }

    [Fact]
    public void Net10_Debug_NullConditional_WeaveAndExecute()
    {
        SkipIfSdkMissing("10.0");
        AssertWeaveAndExecute("Net10NullCondDebug", NullConditionalSource,
            "10.0.0", "net10.0", "Debug",
            ["Null_conditional_assertion", "Null_conditional_with_null_value"],
            expectAllPass: false);
    }

    [Fact]
    public void Net10_Release_NullConditional_WeaveAndExecute()
    {
        SkipIfSdkMissing("10.0");
        AssertWeaveAndExecute("Net10NullCondRelease", NullConditionalSource,
            "10.0.0", "net10.0", "Release",
            ["Null_conditional_assertion", "Null_conditional_with_null_value"],
            expectAllPass: false);
    }

    // ========== .NET 11 Preview SDK tests ==========
    // We compile with the .NET 11 SDK but target net10.0 so the assemblies
    // can be loaded and executed in the current (.NET 10) test host.
    // This still tests the .NET 11 Roslyn compiler codegen patterns.

    [Fact]
    public void Net11_Debug_AsyncNoAwait_WeaveAndExecute()
    {
        SkipIfNet11Missing();
        AssertWeaveAndExecute("Net11AsyncNoAwaitDebug", AsyncNoAwaitSource,
            "11.0.100-preview.3", "net10.0", "Debug",
            ["The_value_should_not_be_null", "The_value_should_be_hello"],
            dotnetPath: DotNet11Path);
    }

    [Fact]
    public void Net11_Release_AsyncNoAwait_WeaveAndExecute()
    {
        SkipIfNet11Missing();
        AssertWeaveAndExecute("Net11AsyncNoAwaitRelease", AsyncNoAwaitSource,
            "11.0.100-preview.3", "net10.0", "Release",
            ["The_value_should_not_be_null", "The_value_should_be_hello"],
            dotnetPath: DotNet11Path);
    }

    [Fact]
    public void Net11_Debug_AsyncWithAwait_WeaveAndExecute()
    {
        SkipIfNet11Missing();
        AssertWeaveAndExecute("Net11AsyncWithAwaitDebug", AsyncWithAwaitSource,
            "11.0.100-preview.3", "net10.0", "Debug",
            ["Delayed_assertion", "Multiple_assertions_with_await"],
            dotnetPath: DotNet11Path);
    }

    [Fact]
    public void Net11_Release_AsyncWithAwait_WeaveAndExecute()
    {
        SkipIfNet11Missing();
        AssertWeaveAndExecute("Net11AsyncWithAwaitRelease", AsyncWithAwaitSource,
            "11.0.100-preview.3", "net10.0", "Release",
            ["Delayed_assertion", "Multiple_assertions_with_await"],
            dotnetPath: DotNet11Path);
    }

    [Fact]
    public void Net11_Debug_NullConditional_WeaveAndExecute()
    {
        SkipIfNet11Missing();
        AssertWeaveAndExecute("Net11NullCondDebug", NullConditionalSource,
            "11.0.100-preview.3", "net10.0", "Debug",
            ["Null_conditional_assertion", "Null_conditional_with_null_value"],
            expectAllPass: false, dotnetPath: DotNet11Path);
    }

    [Fact]
    public void Net11_Release_NullConditional_WeaveAndExecute()
    {
        SkipIfNet11Missing();
        AssertWeaveAndExecute("Net11NullCondRelease", NullConditionalSource,
            "11.0.100-preview.3", "net10.0", "Release",
            ["Null_conditional_assertion", "Null_conditional_with_null_value"],
            expectAllPass: false, dotnetPath: DotNet11Path);
    }

    // ========== Helpers ==========

    private static void SkipIfSdkMissing(string sdkMajorMinor)
    {
        if (!TestAssemblyBuilder.IsSdkAvailable(sdkMajorMinor))
            Assert.Skip($".NET {sdkMajorMinor} SDK not installed");
    }

    private static void SkipIfHostCantLoad(string tfm)
    {
        // Extract major version from TFM (e.g. "net10.0" → 10)
        var tfmMajor = int.Parse(tfm.Replace("net", "").Split('.')[0]);
        if (Environment.Version.Major < tfmMajor)
            Assert.Skip($"Test host is .NET {Environment.Version.Major}, can't load {tfm} assemblies");
    }

    private static void SkipIfNet11Missing()
    {
        if (!HasNet11)
            Assert.Skip(".NET 11 preview SDK not installed");
    }

    private static void AssertWeaveAndExecute(
        string name, string source, string sdkVersion, string tfm,
        string configuration, string[] methodNames,
        bool expectAllPass = true, string? dotnetPath = null)
    {
        SkipIfHostCantLoad(tfm);

        var assemblyPath = TestAssemblyBuilder.BuildWithSdk(
            name, source, sdkVersion, tfm, configuration, dotnetPath);

        var weaver = new AssertionWeaver();
        var result = weaver.Weave(assemblyPath, Path.ChangeExtension(assemblyPath, ".pdb"));

        result.WeavedCount.Should().BeGreaterThan(0,
            $"weaver should instrument assertions in assembly compiled by SDK {sdkVersion} ({configuration})");

        // Load into an isolated AssemblyLoadContext to avoid name collisions
        // (all fixture assemblies are named 'WeaverFixture')
        var alc = new AssemblyLoadContext(name, isCollectible: true);
        try
        {
            var asm = alc.LoadFromAssemblyPath(assemblyPath);
            var testType = asm.GetType("Tests")!;
            var instance = Activator.CreateInstance(testType)!;

            var testId = $"{name}_{Guid.NewGuid():N}";
            using var scope = TestIdentityScope.Begin(testId, testId);

            foreach (var methodName in methodNames)
            {
                var method = testType.GetMethod(methodName);
                method.Should().NotBeNull($"method '{methodName}' should exist");

                var returnType = method!.ReturnType;

                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    var task = (Task)method.Invoke(instance, null)!;
                    var ex = Record.Exception(() => task.GetAwaiter().GetResult());

                    if (expectAllPass)
                    {
                        ex.Should().BeNull(
                            $"SDK {sdkVersion} ({configuration}): '{methodName}' should not throw InvalidProgramException");
                    }
                    else
                    {
                        // For null-conditional tests: should NOT be InvalidProgramException
                        // (assertion failures are OK, CLR rejection is not)
                        if (ex is not null)
                            ex.Should().NotBeOfType<InvalidProgramException>(
                                $"SDK {sdkVersion} ({configuration}): '{methodName}' must not produce invalid IL");
                    }
                }
                else
                {
                    var ex = Record.Exception(() => method.Invoke(instance, null));
                    if (expectAllPass)
                        ex.Should().BeNull(
                            $"SDK {sdkVersion} ({configuration}): '{methodName}' should not throw");
                }
            }
        }
        finally
        {
            alc.Unload();
        }
    }
}
