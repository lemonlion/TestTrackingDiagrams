using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace TestTrackingDiagrams.AssertionTracking;

/// <summary>
/// MSBuild task that runs after compilation and uses Mono.Cecil to instrument
/// FluentAssertions .Should() call sites with assertion tracking (try/catch around
/// each assertion statement that calls Track.AssertionPassed/Track.AssertionFailed).
/// Only activates if [assembly: TrackAssertionsBeta] is found in the compiled assembly.
/// </summary>
public class WeaveAssertionsTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Root paths for source files, used to locate source code referenced by PDB sequence points.
    /// </summary>
    public ITaskItem[] SourceRoots { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Resolved reference paths (from MSBuild @(ReferencePath)). Used to configure
    /// Cecil's assembly resolver so it can locate referenced assemblies (NuGet packages, etc.).
    /// </summary>
    public ITaskItem[] References { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            return ExecuteCore();
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private bool ExecuteCore()
    {
        if (!File.Exists(AssemblyPath))
        {
            Log.LogMessage(MessageImportance.Low, "AssertionTracking: Assembly not found at {0}", AssemblyPath);
            return true;
        }

        var pdbPath = Path.ChangeExtension(AssemblyPath, ".pdb");
        if (!File.Exists(pdbPath))
        {
            Log.LogMessage(MessageImportance.Low, "AssertionTracking: PDB not found at {0}, skipping", pdbPath);
            return true;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var searchDirectories = References
            .Select(r => Path.GetDirectoryName(r.ItemSpec))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var weaver = new AssertionWeaver(Log, searchDirectories!);
        var result = weaver.Weave(AssemblyPath, pdbPath);
        sw.Stop();

        if (result.WeavedCount > 0)
        {
            Log.LogMessage(MessageImportance.Normal,
                "TestTrackingDiagrams.AssertionTracking: Instrumented {0} assertion(s) in {1} method(s) ({2}ms)",
                result.WeavedCount, result.MethodCount, sw.ElapsedMilliseconds);
        }
        else
        {
            Log.LogMessage(MessageImportance.Low,
                "TestTrackingDiagrams.AssertionTracking: Completed in {0}ms (no assertions found{1})",
                sw.ElapsedMilliseconds,
                result.SkipReason != null ? $" - {result.SkipReason}" : "");
        }

        return true;
    }
}
