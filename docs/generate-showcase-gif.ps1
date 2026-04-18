<#
.SYNOPSIS
    Open the showcase demo HTML for recording as a 60-second GIF.

.DESCRIPTION
    Opens docs/showcase-demo.html in the default browser. The page auto-plays
    an animated 8-scene showcase of TestTrackingDiagrams features over exactly
    60 seconds at 960x540.

    To capture as a GIF, use a screen-recording tool:
      - ScreenToGif (recommended): https://www.screentogif.com/
      - ShareX: https://getsharex.com/
      - LICEcap: https://www.cockos.com/licecap/

    Steps:
      1. Run this script to open the demo in your browser
      2. Start your screen recorder, select the 960x540 browser viewport
      3. The demo auto-plays for 60 seconds, then stops
      4. Save as GIF

    The demo showcases:
      Scene 1 — Auto-generated sequence diagrams from tests
      Scene 2 — Rich interactive HTML reports with pie charts
      Scene 3 — Powerful search engine with boolean operators
      Scene 4 — Parameterized test table grouping
      Scene 5 — Focus/emphasis on JSON fields in diagrams
      Scene 6 — Track every integration (HTTP, Cosmos, EF, Redis, Events)
      Scene 7 — Every .NET test framework supported
      Scene 8 — Get started CTA
#>

$htmlPath = Join-Path $PSScriptRoot 'showcase-demo.html'

Write-Host "Opening showcase demo in browser..." -ForegroundColor Cyan
Write-Host ""
Write-Host "The demo auto-plays 8 scenes over 60 seconds at 960x540." -ForegroundColor Yellow
Write-Host "Use a screen recorder (e.g. ScreenToGif) to capture it as a GIF." -ForegroundColor Yellow
Write-Host ""

Start-Process $htmlPath
