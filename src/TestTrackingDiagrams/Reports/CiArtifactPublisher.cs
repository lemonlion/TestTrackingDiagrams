namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Publishes generated report files as CI artifacts using platform-specific mechanisms
/// (GitHub Actions artifact upload commands or Azure DevOps artifact publishing).
/// </summary>
public static class CiArtifactPublisher
{
    public static void Publish(
        string[] reportFilePaths,
        CiEnvironment environment,
        string artifactName = "TestReports",
        int retentionDays = 1)
        => Publish(reportFilePaths, environment, artifactName, retentionDays,
            Environment.GetEnvironmentVariable, File.AppendAllText, Console.WriteLine, File.Exists);

    internal static void Publish(
        string[] reportFilePaths,
        CiEnvironment environment,
        string artifactName,
        int retentionDays,
        Func<string, string?> getEnvVar,
        Action<string, string> appendFile,
        Action<string> writeLine,
        Func<string, bool> fileExists)
    {
        switch (environment)
        {
            case CiEnvironment.AzureDevOps:
                foreach (var path in reportFilePaths)
                {
                    if (!fileExists(path)) continue;
                    writeLine($"##vso[artifact.upload containerfolder={artifactName};artifactname={artifactName}]{path}");
                }
                break;

            case CiEnvironment.GitHubActions:
            {
                var outputPath = getEnvVar("GITHUB_OUTPUT");
                if (string.IsNullOrEmpty(outputPath)) return;
                var reportsDir = reportFilePaths.Length > 0
                    ? Path.GetDirectoryName(reportFilePaths[0]) ?? ""
                    : "";
                appendFile(outputPath, $"reports-path={reportsDir}\n");
                appendFile(outputPath, $"reports-retention-days={retentionDays}\n");
                break;
            }

            case CiEnvironment.None:
            default:
                break;
        }
    }
}
