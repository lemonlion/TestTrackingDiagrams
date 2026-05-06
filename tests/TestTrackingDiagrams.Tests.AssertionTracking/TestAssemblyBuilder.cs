using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace TestTrackingDiagrams.Tests.AssertionTracking;

/// <summary>
/// Compiles C# source code into a test assembly with PDB for the weaver tests.
/// </summary>
public static class TestAssemblyBuilder
{
    private static readonly string OutputDir = Path.Combine(
        Path.GetTempPath(), "TTD_WeaverTests", Guid.NewGuid().ToString("N")[..8]);

    static TestAssemblyBuilder()
    {
        Directory.CreateDirectory(OutputDir);
    }

    // Auto-generated attribute sources (matches what the .targets file generates)
    private const string AttributeSource = """
        using System;
        namespace TestTrackingDiagrams.Tracking
        {
            [AttributeUsage(AttributeTargets.Assembly)]
            internal sealed class TrackAssertionsBetaAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
            internal sealed class SuppressAssertionTrackingAttribute : Attribute { }
        }
        """;

    /// <summary>
    /// Compiles source code into an assembly with PDB. Returns the assembly file path.
    /// The assembly references FluentAssertions and TestTrackingDiagrams for realistic assertion tracking.
    /// </summary>
    public static string Build(string name, string source)
    {
        var assemblyPath = Path.Combine(OutputDir, $"{name}.dll");
        var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

        var sourcePath = Path.Combine(OutputDir, $"{name}.cs");
        var sourceText = SourceText.From(source, Encoding.UTF8);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText,
            new CSharpParseOptions(LanguageVersion.Latest),
            path: sourcePath);

        // Write the source file to disk so the weaver can read it for expression text
        File.WriteAllText(sourcePath, source, Encoding.UTF8);

        // Include the auto-generated attributes so [assembly: TrackAssertionsBeta] compiles
        var attrSourceText = SourceText.From(AttributeSource, Encoding.UTF8);
        var attrTree = CSharpSyntaxTree.ParseText(attrSourceText,
            new CSharpParseOptions(LanguageVersion.Latest));

        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            name,
            new[] { syntaxTree, attrTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Debug));

        var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);

        using var peStream = File.Create(assemblyPath);
        using var pdbStream = File.Create(pdbPath);
        var result = compilation.Emit(peStream, pdbStream, options: emitOptions);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage());
            throw new InvalidOperationException(
                $"Compilation failed for '{name}':\n{string.Join("\n", errors)}");
        }

        return assemblyPath;
    }

    private static MetadataReference[] GetReferences()
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Include all trusted platform assemblies (covers all runtime deps)
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (trustedAssemblies != null)
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
            {
                if (File.Exists(path))
                    refs.Add(path);
            }
        }

        // FluentAssertions
        var faAssembly = typeof(FluentAssertions.AssertionExtensions).Assembly;
        refs.Add(faAssembly.Location);

        // FluentAssertions dependencies
        foreach (var referencedAssembly in faAssembly.GetReferencedAssemblies())
        {
            try
            {
                var loaded = Assembly.Load(referencedAssembly);
                if (!string.IsNullOrEmpty(loaded.Location))
                    refs.Add(loaded.Location);
            }
            catch { /* skip unresolvable */ }
        }

        // TestTrackingDiagrams (for Track type and attributes)
        var ttdAssembly = typeof(TestTrackingDiagrams.Tracking.Track).Assembly;
        refs.Add(ttdAssembly.Location);

        return refs
            .Where(File.Exists)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToArray();
    }
}
