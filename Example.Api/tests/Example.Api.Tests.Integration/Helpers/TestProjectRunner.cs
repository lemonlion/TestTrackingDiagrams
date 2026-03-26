using System.Diagnostics;
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

    public static async Task<TestProjectRunResult> RunAsync(
        string projectName,
        Dictionary<string, string>? environmentVariables = null,
        TimeSpan? timeout = null)
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

        return new TestProjectRunResult(
            process.ExitCode == 0,
            stdout.ToString(),
            stderr.ToString(),
            reportsFolderPath,
            process.ExitCode);
    }
}
