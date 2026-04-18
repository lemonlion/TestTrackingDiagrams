namespace TestTrackingDiagrams.Reports;

public static class CiSummaryWriter
{
    public static void Write(string markdown, CiEnvironment environment)
        => Write(markdown, environment, Environment.GetEnvironmentVariable, File.AppendAllText, Console.WriteLine);

    internal static void Write(
        string markdown,
        CiEnvironment environment,
        Func<string, string?> getEnvVar,
        Action<string, string> appendFile,
        Action<string> writeLine)
    {
        switch (environment)
        {
            case CiEnvironment.GitHubActions:
            {
                var summaryPath = getEnvVar("GITHUB_STEP_SUMMARY");
                if (string.IsNullOrEmpty(summaryPath)) return;
                appendFile(summaryPath, markdown);
                break;
            }
            case CiEnvironment.AzureDevOps:
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"ci-summary-{Guid.NewGuid():N}.md");
                appendFile(tempPath, markdown);
                writeLine($"##vso[task.uploadsummary]{tempPath}");
                break;
            }
            case CiEnvironment.None:
            default:
                break;
        }
    }
}
