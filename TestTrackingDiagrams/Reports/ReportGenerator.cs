using System.Diagnostics;
using System.Text;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Reports;

public static class ReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(Feature[] features, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        if (options.InternalFlowTracking && options.DiagramFormat == DiagramFormat.PlantUml)
        {
            if (options.PlantUmlRendering is PlantUmlRendering.Server or PlantUmlRendering.Local)
            {
                options.InlineSvgRendering = true;
                options.PlantUmlImageFormat = PlantUmlImageFormat.Svg;
            }
        }

        var fetcherOptions = new DiagramsFetcherOptions
        {
            PlantUmlServerBaseUrl = options.PlantUmlServerBaseUrl,
            RequestPostFormattingProcessor = options.RequestResponsePostProcessor,
            ResponsePostFormattingProcessor = options.RequestResponsePostProcessor,
            RequestMidFormattingProcessor = options.RequestResponseMidProcessor,
            ResponseMidFormattingProcessor = options.RequestResponseMidProcessor,
            ExcludedHeaders = options.ExcludedHeaders,
            SeparateSetup = options.SeparateSetup,
            HighlightSetup = options.HighlightSetup,
            LazyLoadDiagramImages = options.LazyLoadDiagramImages,
            FocusEmphasis = options.FocusEmphasis,
            FocusDeEmphasis = options.FocusDeEmphasis,
            PlantUmlTheme = options.PlantUmlTheme,
            PlantUmlImageFormat = options.PlantUmlImageFormat,
            LocalDiagramRenderer = options.LocalDiagramRenderer,
            LocalDiagramImageDirectory = options.LocalDiagramImageDirectory,
            DiagramFormat = options.DiagramFormat,
            PlantUmlRendering = options.PlantUmlRendering,
            InlineSvgRendering = options.InlineSvgRendering,
            InternalFlowTracking = options.InternalFlowTracking
        };
        var diagrams = DefaultDiagramsFetcher.GetDiagramsFetcher(fetcherOptions)();

        var internalFlowDataScript = "";
        if (options.InternalFlowTracking)
        {
            internalFlowDataScript = BuildInternalFlowDataScript(options);
        }

        var actions = new List<Action>
        {
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, options.HtmlSpecificationsCustomStyleSheet, $"{options.HtmlSpecificationsFileName}.html", options.SpecificationsTitle, false, generateBlankOnFailedTests: true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript),
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, null, $"{options.HtmlTestRunReportFileName}.html", "Features Report", true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript),
            () => GenerateYamlSpecs(diagrams, features, $"{options.YamlSpecificationsFileName}.yml", options.SpecificationsTitle, true)
        };

        if (options.GenerateComponentDiagram)
        {
            actions.Add(() => ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
                RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
                options));
        }

        Parallel.Invoke(actions.ToArray());

        if (options.WriteCiSummary)
        {
            var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, startRunTime, endRunTime, options.MaxCiSummaryDiagrams,
                options.DiagramFormat, options.CiSummaryPlantUmlRendering, options.PlantUmlServerBaseUrl, options.LocalDiagramRenderer);

            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "CiSummary.md"), markdown);

            var ciEnvironment = CiEnvironmentDetector.Detect();
            CiSummaryWriter.Write(markdown, ciEnvironment);

            if (options.WriteCiSummaryInteractiveHtml)
            {
                var ciFeatures = FilterFeaturesForCiSummary(features, diagrams, options.MaxCiSummaryDiagrams);
                GenerateHtmlReport(diagrams, ciFeatures, startRunTime, endRunTime, null, "CiSummaryInteractive.html", "CI Test Run Summary", true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript);
            }
        }

        if (options.PublishCiArtifacts)
        {
            var ciEnv = CiEnvironmentDetector.Detect();
            var reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            if (Directory.Exists(reportsDirectory))
            {
                var reportFiles = Directory.GetFiles(reportsDirectory)
                    .Where(f => f.EndsWith(".html") || f.EndsWith(".yml") || f.EndsWith(".md"))
                    .ToArray();
                CiArtifactPublisher.Publish(reportFiles, ciEnv, options.CiArtifactName, options.CiArtifactRetentionDays);
            }
        }
    }

    public static string GenerateHtmlReport(DefaultDiagramsFetcher.DiagramAsCode[] diagrams,
        Feature[] features,
        DateTime startRunTime,
        DateTime endRunTime,
        string? stylesheet,
        string fileName,
        string title,
        bool includeTestRunData,
        bool generateBlankOnFailedTests = false,
        bool lazyLoadImages = true,
        DiagramFormat diagramFormat = DiagramFormat.PlantUml,
        PlantUmlRendering plantUmlRendering = PlantUmlRendering.Server,
        bool inlineSvgRendering = false,
        bool internalFlowTracking = false,
        string internalFlowDataScript = "")
    {
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ScenarioResult.Failed)))
            return WriteFile(string.Empty, fileName);

        var toggleHappyPathsFunction = """
                                       function toggleHappyPaths(showOnlyHappyPaths)
                                       {
                                           var hideNonHappyPathsSheetId = "hideNonHappyPathsSheet";
                                           if(showOnlyHappyPaths)
                                           {
                                               var sheet = document.createElement('style');
                                               sheet.id = hideNonHappyPathsSheetId;
                                               sheet.innerHTML = ".scenario { display:none; } .scenario.happy-path { display:block; }";
                                               document.body.appendChild(sheet);
                                           }
                                           else
                                           {
                                               var sheetToBeRemoved = document.getElementById(hideNonHappyPathsSheetId);
                                               sheetToBeRemoved.parentNode.removeChild(sheetToBeRemoved);
                                           }
                                       }
                                       """;
        var searchFunction = """
                             var searchTimeoutId;
                             
                             function search_scenarios() {
                                 let features = document.getElementsByClassName('feature');
                                 for (let i = 0; i < features.length; i++)
                                     features[i].style.opacity = '0.5';
                             
                                 if (searchTimeoutId)
                                     clearTimeout(searchTimeoutId);
                             
                                 searchTimeoutId = setTimeout(function () {
                                     run_search_scenarios();
                                 }, 1000);
                             }
                             
                             function run_search_scenarios() {
                                 let input = document.getElementById('searchbar').value;
                                 input = input.toLowerCase().trim();
                             
                                 let searchTokens = parseSearchTokensIncludingQuotes(input);
                             
                                 let features = document.getElementsByClassName('feature');
                                 let scenarios = document.getElementsByClassName('scenario');
                             
                                 // Clear previous search state
                                 for (let i = 0; i < scenarios.length; i++) {
                                     scenarios[i].classList.remove('search-hidden');
                                     scenarios[i].removeAttribute('open');
                                 }
                                 for (let i = 0; i < features.length; i++) {
                                     features[i].classList.remove('search-hidden');
                                     features[i].style.opacity = '';
                                     if (features[i].classList.contains('search-opened')) {
                                         features[i].removeAttribute('open');
                                         features[i].classList.remove('search-opened');
                                     }
                                 }
                             
                                 if (searchTokens.length === 0)
                                     return;
                             
                                 // Match at the scenario level
                                 let matchingScenarios = [];
                                 for (let i = 0; i < scenarios.length; i++) {
                                     let text = scenarios[i].textContent.toLowerCase();
                                     let diagramEls = scenarios[i].querySelectorAll('[data-plantuml],[data-mermaid-source]');
                                     for (let d = 0; d < diagramEls.length; d++) {
                                         var src = diagramEls[d].getAttribute('data-plantuml')
                                                || diagramEls[d].getAttribute('data-mermaid-source');
                                         if (src) text += ' ' + src.toLowerCase();
                                     }
                                     let allMatch = true;
                                     for (let j = 0; j < searchTokens.length; j++) {
                                         if (!text.includes(searchTokens[j])) {
                                             allMatch = false;
                                             break;
                                         }
                                     }
                                     if (allMatch) {
                                         matchingScenarios.push(scenarios[i]);
                                     } else {
                                         scenarios[i].classList.add('search-hidden');
                                     }
                                 }
                             
                                 // Single match: expand scenario with diagrams
                                 if (matchingScenarios.length === 1) {
                                     let s = matchingScenarios[0];
                                     s.setAttribute('open', '');
                                     let diagrams = s.querySelector('details.example-diagrams');
                                     if (diagrams) diagrams.setAttribute('open', '');
                                     let rawPuml = s.querySelector('details.example');
                                     if (rawPuml) rawPuml.removeAttribute('open');
                                 }
                             
                                 // Hide features with no visible scenarios, open features with matches
                                 for (let i = 0; i < features.length; i++) {
                                     let childScenarios = features[i].querySelectorAll('.scenario');
                                     let hasVisible = false;
                                     for (let k = 0; k < childScenarios.length; k++) {
                                         if (!childScenarios[k].classList.contains('search-hidden')) {
                                             hasVisible = true;
                                             break;
                                         }
                                     }
                                     if (!hasVisible) {
                                         features[i].classList.add('search-hidden');
                                     } else if (!features[i].hasAttribute('open')) {
                                         features[i].setAttribute('open', '');
                                         features[i].classList.add('search-opened');
                                     }
                                 }
                             }
                             
                             function parseSearchTokensIncludingQuotes(str) {
                                 let quoteTokens = [];
                                 for (let match of str.matchAll(/"(.*?)"/g)) {
                                     let phrase = match[1].trim();
                                     if (phrase.length > 0) quoteTokens.push(phrase);
                                 }
                             
                                 let remaining = str.replace(/"(.*?)"/g, '').trim();
                             
                                 let simpleTokens = [];
                                 if (remaining.length > 0) {
                                     let rawWords = remaining.split(/\s+/);
                                     for (let i = 0; i < rawWords.length; i++) {
                                         let token = rawWords[i].trim();
                                         if (token.length > 0) simpleTokens.push(token);
                                     }
                                 }
                             
                                 return quoteTokens.concat(simpleTokens);
                             }
                             """;

        var combinedStylesheet = $"""
                                 {Stylesheets.HtmlReportStyleSheet}
                                 {stylesheet}
                                 """;

        var isMermaid = diagramFormat == DiagramFormat.Mermaid;
        var isPlantUmlBrowser = !isMermaid && plantUmlRendering == PlantUmlRendering.BrowserJs;
        var isInlineSvg = !isMermaid && !isPlantUmlBrowser && inlineSvgRendering;
        var hasInteractiveDiagrams = isMermaid || isPlantUmlBrowser || isInlineSvg;
        var mermaidScript = isMermaid ? DiagramContextMenu.GetMermaidScript() : "";
        var plantUmlBrowserScript = isPlantUmlBrowser ? DiagramContextMenu.GetPlantUmlBrowserRenderScript() : "";
        var contextMenuScript = hasInteractiveDiagrams ? DiagramContextMenu.GetContextMenuScript() : "";
        var contextMenuStyles = hasInteractiveDiagrams ? DiagramContextMenu.GetStyles() : "";
        var inlineSvgStyles = isInlineSvg ? DiagramContextMenu.GetInlineSvgStyles() : "";
        var internalFlowPopupStyles = internalFlowTracking ? DiagramContextMenu.GetInternalFlowPopupStyles() : "";
        var internalFlowPopupScript = internalFlowTracking ? DiagramContextMenu.GetInternalFlowPopupScript() : "";

        var html = $$"""
                    <html>
                        <head>
                            <style>
                                {{combinedStylesheet}}
                                {{contextMenuStyles}}
                                {{inlineSvgStyles}}
                                {{internalFlowPopupStyles}}
                            </style>
                            <script>
                                {{toggleHappyPathsFunction}}
                                {{searchFunction}}
                            </script>
                            {{mermaidScript}}
                            {{plantUmlBrowserScript}}
                            {{contextMenuScript}}
                            {{internalFlowDataScript}}
                            {{internalFlowPopupScript}}
                        </head>
                        <body>
                    """;

        var body = new StringBuilder();
        body.Append($"<h1>{title}</h1>");

        if (includeTestRunData)
        {
            var numberOfFeatures = features.Length;
            var scenarios = features.SelectMany(x => x.Scenarios).ToArray();
            var passedScenarios = scenarios.Where(x => x.Result == ScenarioResult.Passed).ToArray();
            var skippedScenarios = scenarios.Where(x => x.Result == ScenarioResult.Skipped).ToArray();
            var failedScenarios = scenarios.Where(x => x.Result == ScenarioResult.Failed).ToArray();
            var overallStatus = failedScenarios.Any() ? "Failed" : "Passed";

            body.Append($"""
                    <div class="test-execution-summary">
                        <h2>Test Execution Summary</h2>
                        <table>
                            <tr><td colspan="2" class="column-header">Execution</td><td colspan="2" class="column-header">Content</td></tr>
                            <tr><td>Overall status:</td><td>{overallStatus}</td><td>Features: </td><td>{numberOfFeatures}</td></tr>
                            <tr><td>Start Date:</td><td>{startRunTime.ToShortDateString()} (UTC)</td><td>Scenarios: </td><td>{scenarios.Length}</td></tr>
                            <tr><td>Start Time:</td><td>{startRunTime:HH:mm:ss}</td><td>Passed Scenarios: </td><td>{passedScenarios.Length}</td></tr>
                            <tr><td>End Time:</td><td>{endRunTime:HH:mm:ss}</td><td>Failed Scenarios: </td><td>{failedScenarios.Length}</td></tr>
                            <tr><td>Duration:</td><td>{FormatDuration(endRunTime - startRunTime)}</td><td>Skipped Scenarios: </td><td>{skippedScenarios.Length}</td></tr>
                        </table>
                    </div>
                    
                    <h2>Features Summary</h2>
                    """);
        }

        body.Append($"""
                 <div class="filters">
                    <label for="toggle-happy-paths">Show Only Happy Paths</label>
                    <input id="toggle-happy-paths" type="checkbox" onchange="toggleHappyPaths(this.checked)" />
                    <div><input id="searchbar" placeholder="Search" onkeyup="search_scenarios()" /></div>
                 </div>
                 """);

        var diagramsByTestId = diagrams.ToLookup(x => x.TestRuntimeId);
        var plantUmlBrowserCounter = 0;

        foreach (var feature in features)
        {
            body.Append($"""
                     <details class="feature">
                        <summary class="h2">{feature.DisplayName}{(feature.Endpoint is null ? "" : $" <div class=\"endpoint\">{feature.Endpoint}</div>")}</summary>
                     """);

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName);

            foreach (var scenario in orderedScenarios)
            {
                var failed = scenario.Result == ScenarioResult.Failed;
                body.Append($"""
                         <details class="scenario{(scenario.IsHappyPath ? " happy-path" : "")}">
                            <summary class="h3{(failed ? " failed" : "")}">{scenario.DisplayName}{(scenario.IsHappyPath ? " <span class=\"label\">Happy Path</span>" : "")}</summary>
                         """);

                if (failed)
                {
                    body.Append($"""
                              <details class="failure-result" open>
                                 <summary class="h4">Failure Result</summary>
                                 <pre>
                              Failure Cause: {scenario.ErrorMessage}
                              
                              {scenario.ErrorStackTrace}
                                 </pre>
                              </details>
                              """);
                }

                var diagramsForTest = diagramsByTestId[scenario.Id].ToArray();


                if (diagramsForTest.Length > 0)
                {
                    body.Append("""
                            <details class="example-diagrams" open>
                            <summary class="h4">Sequence Diagrams</summary>
                            """);

                    var lazyLoadAttr = lazyLoadImages ? " loading=\"lazy\"" : "";
                    var rawLabel = isMermaid ? "Raw Mermaid" : "Raw Plant UML";
                    foreach (var diagram in diagramsForTest)
                    {
                        if (isMermaid)
                        {
                            var mermaidEncoded = System.Net.WebUtility.HtmlEncode(diagram.CodeBehind);
                            body.Append($"""
                                     <pre class="mermaid" data-mermaid-source="{mermaidEncoded}" data-diagram-type="mermaid">{diagram.CodeBehind}</pre>
                                     """);
                        }
                        else if (isPlantUmlBrowser)
                        {
                            var diagramId = $"puml-{plantUmlBrowserCounter++}";
                            var encoded = System.Net.WebUtility.HtmlEncode(diagram.CodeBehind);
                            body.Append($"""
                                     <div class="plantuml-browser" id="{diagramId}" data-plantuml="{encoded}" data-diagram-type="plantuml">Loading diagram...</div>
                                     """);
                        }
                        else if (isInlineSvg)
                        {
                            var sourceEncoded = System.Net.WebUtility.HtmlEncode(diagram.CodeBehind);
                            body.Append($"""
                                     <div class="plantuml-inline-svg" data-plantuml="{sourceEncoded}" data-diagram-type="plantuml">{diagram.ImgSrc}</div>
                                     """);
                        }
                        else
                        {
                            body.Append($"""
                                     <details class="example">
                                        <summary class="example-image">
                                            <img{lazyLoadAttr} src="{diagram.ImgSrc}">
                                        </summary>
                                        <div class="raw-plantuml">
                                            <h4>{rawLabel}</h4>
                                            <pre>{diagram.CodeBehind}</pre>
                                         </div>
                                     </details>
                                     """);
                        }
                    }
                    body.Append("</details>");

                }
                body.Append("</details>");
            }
            body.Append("</details>");
        }

        html += body;
        html += """
                    </body>
                </html>
                """
        ;

        return WriteFile(html, fileName);
    }

    public static string GenerateYamlSpecs(DefaultDiagramsFetcher.DiagramAsCode[] diagrams,
        Feature[] features,
        string fileName,
        string title,
        bool generateBlankOnFailedTests = false)
    {
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ScenarioResult.Failed)))
            return WriteFile(string.Empty, fileName);

        var yml = new StringBuilder();
        yml.Append("Title: " + title + "\n");
        yml.Append("Features:\n");

        foreach (var feature in features.OrderBy(x => x.DisplayName))
        {
            yml.Append("  - Feature: " + feature.DisplayName.SanitiseForYml() + "\n");

            if (feature.Endpoint is not null)
                yml.Append("    Endpoint: " + feature.Endpoint + "\n");

            yml.Append("    Scenarios:\n");

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName);
            foreach (var scenario in orderedScenarios)
            {
                yml.Append("      - Scenario: " + scenario.DisplayName.SanitiseForYml() + "\n");
                yml.Append("        IsHappyPath: " + scenario.IsHappyPath.ToString().ToLower());
                yml.Append("\n\n");
            }
        }

        return WriteFile(yml.ToString(), fileName);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var total = duration.Duration();
        if (total.TotalSeconds < 1)
            return $"{total.Milliseconds}ms";
        if (total.TotalMinutes < 1)
            return $"{total.Seconds}s";
        return $"{(int)total.TotalMinutes}m {total.Seconds}s";
    }

    private static string WriteFile(string text, string fileName)
    {
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, text);
        return filePath;
    }

    private static Feature[] FilterFeaturesForCiSummary(Feature[] features, DefaultDiagramsFetcher.DiagramAsCode[] diagrams, int maxDiagrams)
    {
        var diagramsByTestId = diagrams.ToLookup(d => d.TestRuntimeId);
        var hasFailed = features.SelectMany(f => f.Scenarios).Any(s => s.Result == ScenarioResult.Failed);

        if (hasFailed)
        {
            var shown = 0;
            var filtered = new List<Feature>();
            foreach (var feature in features)
            {
                var failedScenarios = feature.Scenarios.Where(s => s.Result == ScenarioResult.Failed).ToArray();
                if (failedScenarios.Length == 0) continue;
                var taken = failedScenarios.Take(maxDiagrams - shown).ToArray();
                filtered.Add(feature with { Scenarios = taken });
                shown += taken.Length;
                if (shown >= maxDiagrams) break;
            }
            return filtered.ToArray();
        }

        {
            var shown = 0;
            var filtered = new List<Feature>();
            foreach (var feature in features)
            {
                var scenariosWithDiagrams = feature.Scenarios
                    .Where(s => diagramsByTestId[s.Id].Any())
                    .Take(maxDiagrams - shown)
                    .ToArray();
                if (scenariosWithDiagrams.Length == 0) continue;
                filtered.Add(feature with { Scenarios = scenariosWithDiagrams });
                shown += scenariosWithDiagrams.Length;
                if (shown >= maxDiagrams) break;
            }
            return filtered.ToArray();
        }
    }

    private static string BuildInternalFlowDataScript(ReportConfigurationOptions options)
    {
        var logs = RequestResponseLogger.RequestAndResponseLogs
            .Where(x => !(x?.TrackingIgnore ?? true))
            .ToArray();

        var spans = InternalFlowSpanCollector.CollectSpans(
            options.InternalFlowSpanGranularity,
            options.InternalFlowActivitySources);

        var segments = InternalFlowSegmentBuilder.BuildSegments(logs, spans);

        return InternalFlowHtmlGenerator.GenerateSegmentDataScript(
            segments,
            options.InternalFlowDiagramStyle);
    }
}