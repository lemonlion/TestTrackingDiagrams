using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Example.Api.Tests.Integration.Helpers;

public record TestProjectRunResult(
    bool Success,
    string StandardOutput,
    string StandardError,
    string ReportsFolderPath,
    int ExitCode);

public static class TestProjectRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

    private static readonly string ArchivedReportsRoot =
        Path.Combine(TestProjects.SolutionRoot, "TestResults", "ArchivedReports");

    public static async Task<TestProjectRunResult> RunAsync(
        string projectName,
        Dictionary<string, string>? environmentVariables = null,
        TimeSpan? timeout = null,
        [CallerMemberName] string runLabel = "")
    {
        var projectPath = TestProjects.GetProjectPath(projectName);
        var reportsFolderPath = TestProjects.GetReportsFolderPath(projectName);

        // Clean previous reports so we only see fresh output
        if (Directory.Exists(reportsFolderPath))
            Directory.Delete(reportsFolderPath, recursive: true);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "test --no-restore --verbosity quiet",
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Always enable integration mode
        psi.Environment["TTD_INTEGRATION_MODE"] = "true";

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
                psi.Environment[key] = value;
        }

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var effectiveTimeout = timeout ?? DefaultTimeout;
        var completed = await Task.Run(() => process.WaitForExit((int)effectiveTimeout.TotalMilliseconds));

        if (!completed)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            return new TestProjectRunResult(false, stdout.ToString(), $"Process timed out after {effectiveTimeout.TotalSeconds}s", reportsFolderPath, -1);
        }

        ArchiveReports(reportsFolderPath, projectName, runLabel);

        return new TestProjectRunResult(
            process.ExitCode == 0,
            stdout.ToString(),
            stderr.ToString(),
            reportsFolderPath,
            process.ExitCode);
    }

    private static void ArchiveReports(string reportsFolderPath, string projectName, string runLabel)
    {
        if (!Directory.Exists(reportsFolderPath))
            return;

        // Short project name for the folder: strip the common prefix
        var shortName = projectName.Replace("Example.Api.Tests.Component.", "");
        var archiveDir = Path.Combine(ArchivedReportsRoot, shortName);
        Directory.CreateDirectory(archiveDir);

        foreach (var sourceFile in Directory.GetFiles(reportsFolderPath))
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFile);
            var ext = Path.GetExtension(sourceFile);
            var label = string.IsNullOrEmpty(runLabel) ? "" : $".{runLabel}";
            var destFile = Path.Combine(archiveDir, $"{fileName}{label}{ext}");
            File.Copy(sourceFile, destFile, overwrite: true);
        }
    }
}