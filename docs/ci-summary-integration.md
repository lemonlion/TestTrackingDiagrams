# CI Summary Integration

> This is a local copy of the [CI Summary Integration](https://github.com/lemonlion/TestTrackingDiagrams/wiki/CI-Summary-Integration) wiki page.

## Overview

TestTrackingDiagrams can automatically surface test results and sequence diagrams in your **GitHub Actions job summary** or **Azure DevOps build summary**. This eliminates the need to download artifacts or open the full HTML report to see what happened — failed scenarios, error messages, stack traces, and the corresponding sequence diagrams appear directly in the CI build page.

The feature is **opt-in** via the `WriteCiSummary` property on `ReportConfigurationOptions`.

## Quick Start

Add `WriteCiSummary = true` to your report configuration:

```csharp
new ReportConfigurationOptions
{
    WriteCiSummary = true,
    // ... your existing configuration
}
```

The CI platform is auto-detected. On **GitHub Actions**, the summary is written to `$GITHUB_STEP_SUMMARY`. On **Azure DevOps**, it's uploaded via `##vso[task.uploadsummary]`.

## CI Platform Detection

`CiEnvironmentDetector.Detect()` checks environment variables:

| Environment Variable | Platform |
|---|---|
| `GITHUB_ACTIONS` | GitHub Actions |
| `TF_BUILD` | Azure DevOps |
| Neither | No CI (summary still written to `Reports/CiSummary.md`) |

## Summary Content

The summary is **failure-focused** to stay concise at scale (1000+ tests):

- **When tests fail:** Summary table + up to `MaxCiSummaryDiagrams` failed scenarios with error, stack trace, and diagram
- **When all pass:** Summary table + diagrams for the first `MaxCiSummaryDiagrams` scenarios

Diagrams are rendered as Markdown images using PlantUML server URLs.

## Output Files

| File | Condition | Description |
|---|---|---|
| `Reports/CiSummary.md` | Always (when `WriteCiSummary = true`) | Markdown summary |
| `Reports/CiSummaryInteractive.html` | When `WriteCiSummaryInteractiveHtml = true` | Self-contained HTML with client-side PlantUML JS rendering |

## Configuration

| Property | Type | Default | Description |
|---|---|---|---|
| `WriteCiSummary` | `bool` | `false` | Enable CI summary generation |
| `MaxCiSummaryDiagrams` | `int` | `10` | Max diagrams in the CI summary |
| `WriteCiSummaryInteractiveHtml` | `bool` | `false` | Generate self-contained HTML with PlantUML JS rendering |

## GitHub Actions Example

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-reports
          path: '**/Reports/'
```

## Azure DevOps Example

```yaml
steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'
  - script: dotnet test
    displayName: 'Run tests'
  - task: PublishBuildArtifacts@1
    condition: always()
    inputs:
      pathToPublish: '**/Reports'
      artifactName: 'test-reports'
```

## Interactive HTML Artifact

Set `WriteCiSummaryInteractiveHtml = true` to generate a self-contained HTML file that renders PlantUML diagrams client-side using the PlantUML JS engine. No server required — works offline once downloaded.

## Limitations

- Diagram images require network access to the PlantUML server (use interactive HTML for offline)
- GitHub step summary has a 1 MB size limit (failure-focused design keeps summaries small)
- Both GitHub and Azure DevOps strip `data:` URIs — diagrams must use external URLs

For full documentation, see the [wiki page](https://github.com/lemonlion/TestTrackingDiagrams/wiki/CI-Summary-Integration).
