using System.Text;

namespace TestTrackingDiagrams.Reports;

public static class ReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(Feature[] features, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
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
            DiagramFormat = options.DiagramFormat
        };
        var diagrams = DefaultDiagramsFetcher.GetDiagramsFetcher(fetcherOptions)();

        Parallel.Invoke(
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, options.HtmlSpecificationsCustomStyleSheet, $"{options.HtmlSpecificationsFileName}.html", options.SpecificationsTitle, false, generateBlankOnFailedTests: true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat),
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, null, $"{options.HtmlTestRunReportFileName}.html", "Features Report", true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat),
            () => GenerateYamlSpecs(diagrams, features, $"{options.YamlSpecificationsFileName}.yml", options.SpecificationsTitle, true)
        );
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
        DiagramFormat diagramFormat = DiagramFormat.PlantUml)
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
                                 }
                             
                                 if (searchTokens.length === 0)
                                     return;
                             
                                 // Match at the scenario level
                                 let matchingScenarios = [];
                                 for (let i = 0; i < scenarios.length; i++) {
                                     let text = scenarios[i].textContent.toLowerCase();
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
                             
                                 // Hide features with no visible scenarios
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
        var mermaidScript = isMermaid
            ? """
              <script type="module">
                  import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
                  mermaid.initialize({ startOnLoad: true, securityLevel: 'loose' });
              </script>
              """
            : "";

        var html = $$"""
                    <html>
                        <head>
                            <style>
                                {{combinedStylesheet}}
                            </style>
                            <script>
                                {{toggleHappyPathsFunction}}
                                {{searchFunction}}
                            </script>
                            {{mermaidScript}}
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
                            body.Append($"""
                                     <details class="example">
                                        <summary class="example-image">
                                            <pre class="mermaid">{diagram.CodeBehind}</pre>
                                        </summary>
                                        <div class="raw-plantuml">
                                            <h4>{rawLabel}</h4>
                                            <pre>{diagram.CodeBehind}</pre>
                                         </div>
                                     </details>
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
}