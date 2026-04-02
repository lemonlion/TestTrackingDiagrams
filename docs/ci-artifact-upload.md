# CI Artifact Upload

Automatically publish generated reports as CI build artifacts.

## Overview

When `PublishCiArtifacts = true` is set on `ReportConfigurationOptions`, TestTrackingDiagrams publishes the generated report files (HTML reports, YAML specs, CI summary Markdown) as CI artifacts after test execution.

- **Azure DevOps** — Uses `##vso[artifact.upload]` logging commands. Reports are uploaded automatically during test execution with no additional pipeline configuration.
- **GitHub Actions** — Writes `reports-path` and `reports-retention-days` to `$GITHUB_OUTPUT`. Add a single `upload-artifact` step to your workflow to consume these outputs.

## Configuration

```csharp
new ReportConfigurationOptions
{
    PublishCiArtifacts = true,                // Enable artifact upload
    CiArtifactName = "TestReports",           // Artifact name (default: "TestReports")
    CiArtifactRetentionDays = 1,              // Retention in days (default: 1)
    // ... your existing configuration
}
```

| Property | Type | Default | Description |
|---|---|---|---|
| `PublishCiArtifacts` | `bool` | `false` | When `true`, publishes report files as CI artifacts. |
| `CiArtifactName` | `string` | `"TestReports"` | Name of the artifact in the CI system. |
| `CiArtifactRetentionDays` | `int` | `1` | Retention period in days. Used by GitHub Actions `upload-artifact` step. Azure DevOps uses project-level retention settings. |

## GitHub Actions Setup

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Test
        id: test
        run: dotnet test

      - name: Upload reports
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-reports
          path: ${{ steps.test.outputs.reports-path }}
          retention-days: ${{ steps.test.outputs.reports-retention-days }}
          if-no-files-found: ignore
```

The `reports-path` and `reports-retention-days` outputs are set automatically by the library when `PublishCiArtifacts = true` and `GITHUB_OUTPUT` is available.

## Azure DevOps Setup

No additional pipeline configuration is needed. The library emits `##vso[artifact.upload]` commands to stdout, which the Azure DevOps agent processes automatically:

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'

  - script: dotnet test
    displayName: 'Run tests'
```

Each report file is uploaded individually with the configured artifact name.

## What Gets Uploaded

All files in the `Reports` directory matching `.html`, `.yml`, or `.md` extensions:

| File | Condition |
|---|---|
| `FeaturesReport.html` | Always generated |
| `ComponentSpecificationsWithExamples.html` | Always generated |
| `ComponentSpecifications.yml` | Always generated |
| `CiSummary.md` | When `WriteCiSummary = true` |
| `CiSummaryInteractive.html` | When `WriteCiSummaryInteractiveHtml = true` |
| `ComponentDiagram.html` | When `GenerateComponentDiagram = true` |

## See Also

- [CI Summary Integration](https://github.com/lemonlion/TestTrackingDiagrams/wiki/CI-Summary-Integration) — inline summaries in CI build pages
- [Report Configuration](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Report-Configuration) — all configuration properties
- [Generated Reports](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Generated-Reports) — report file descriptions
