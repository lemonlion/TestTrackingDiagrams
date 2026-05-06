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

        var weaver = new AssertionWeaver(Log);
        var result = weaver.Weave(AssemblyPath, pdbPath);

        if (result.WeavedCount > 0)
        {
            Log.LogMessage(MessageImportance.Normal,
                "TestTrackingDiagrams.AssertionTracking: Instrumented {0} assertion(s) in {1} method(s)",
                result.WeavedCount, result.MethodCount);
        }

        return true;
    }
}
