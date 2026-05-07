using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace TestTrackingDiagrams.StepTracking;

/// <summary>
/// MSBuild task that runs after compilation and uses Mono.Cecil to instrument
/// methods decorated with [GivenStep], [WhenStep], [ThenStep], [Step] attributes
/// with StepCollector.StartStep/CompleteStep calls.
/// Only activates if [assembly: TrackSteps] is found in the compiled assembly.
/// </summary>
public class WeaveStepsTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Resolved reference paths (from MSBuild @(ReferencePath)). Used to configure
    /// Cecil's assembly resolver so it can locate referenced assemblies.
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
            Log.LogMessage(MessageImportance.Low, "StepTracking: Assembly not found at {0}", AssemblyPath);
            return true;
        }

        var pdbPath = Path.ChangeExtension(AssemblyPath, ".pdb");
        if (!File.Exists(pdbPath))
        {
            Log.LogMessage(MessageImportance.Low, "StepTracking: PDB not found at {0}, skipping", pdbPath);
            return true;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var searchDirectories = References
            .Select(r => Path.GetDirectoryName(r.ItemSpec))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var weaver = new StepWeaver(Log, searchDirectories!);
        var result = weaver.Weave(AssemblyPath, pdbPath);
        sw.Stop();

        if (result.WeavedCount > 0)
        {
            Log.LogMessage(MessageImportance.Normal,
                "TestTrackingDiagrams.StepTracking: Instrumented {0} step method(s) ({1}ms)",
                result.WeavedCount, sw.ElapsedMilliseconds);
        }
        else
        {
            Log.LogMessage(MessageImportance.Low,
                "TestTrackingDiagrams.StepTracking: Completed in {0}ms (no step methods found{1})",
                sw.ElapsedMilliseconds,
                result.SkipReason != null ? $" - {result.SkipReason}" : "");
        }

        return true;
    }
}
