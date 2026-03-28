using System.Net;
using System.Text;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public static class BDDfyReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(ReportConfigurationOptions options)
    {
        var scenarios = BDDfyScenarioCollector.GetAll();
        var startRunTime = BDDfyScenarioCollector.StartRunTime == default ? DateTime.UtcNow : BDDfyScenarioCollector.StartRunTime;
        var endRunTime = BDDfyScenarioCollector.EndRunTime == default ? DateTime.UtcNow : BDDfyScenarioCollector.EndRunTime;
        CreateStandardReportsWithDiagrams(scenarios, startRunTime, endRunTime, options);
    }

    public static void CreateStandardReportsWithDiagrams(IEnumerable<BDDfyScenarioInfo> scenarios, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        var scenarioArray = scenarios.ToArray();
        var features = scenarioArray.ToFeatures();

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
            FocusDeEmphasis = options.FocusDeEmphasis
        };
        var diagrams = DefaultDiagramsFetcher.GetDiagramsFetcher(fetcherOptions)();

        GenerateBDDfyHtmlReport(diagrams, features, scenarioArray, startRunTime, endRunTime, options.HtmlSpecificationsCustomStyleSheet, $"{options.HtmlSpecificationsFileName}.html", options.SpecificationsTitle, false, generateBlankOnFailedTests: true, lazyLoadImages: options.LazyLoadDiagramImages);
        GenerateBDDfyHtmlReport(diagrams, features, scenarioArray, startRunTime, endRunTime, null, $"{options.HtmlTestRunReportFileName}.html", "Features Report", true, lazyLoadImages: options.LazyLoadDiagramImages);
        GenerateBDDfyYamlSpecs(features, scenarioArray, $"{options.YamlSpecificationsFileName}.yml", options.SpecificationsTitle, generateBlankOnFailedTests: true);
    }

    private static void GenerateBDDfyHtmlReport(
        DefaultDiagramsFetcher.DiagramAsCode[] diagrams,
        Feature[] features,
        BDDfyScenarioInfo[] allScenarios,
        DateTime startRunTime,
        DateTime endRunTime,
        string? stylesheet,
        string fileName,
        string title,
        bool includeTestRunData,
        bool generateBlankOnFailedTests = false,
        bool lazyLoadImages = true)
    {
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ScenarioResult.Failed)))
        {
            WriteFile(string.Empty, fileName);
            return;
        }

        var scenarioLookup = allScenarios.ToDictionary(x => x.TestId);

        var bddStepStyles = """
                             .bdd-steps {
                                 margin: 0.5em 0 1em 1em;
                                 padding: 0;
                                 list-style: none;
                             }
                             .bdd-steps li {
                                 padding: 0.15em 0;
                                 font-family: monospace;
                                 font-size: 0.95em;
                             }
                             .bdd-steps .keyword {
                                 font-weight: bold;
                                 color: #4070a0;
                             }
                             .story-description {
                                 color: #666;
                                 font-style: italic;
                                 margin: 0.25em 0 0.5em 1em;
                             }
                             """;

        var combinedStylesheet = $"""
                                 {Stylesheets.HtmlReportStyleSheet}
                                 {bddStepStyles}
                                 {stylesheet}
                                 """;

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
                             var feature;
                             var scenario;
                             var searchTimeoutId;
                             
                             function search_scenarios() {
                                 if (!feature)
                                    feature = document.getElementsByClassName('feature');
                                 if (!scenario)
                                    scenario = document.getElementsByClassName('scenario');
                                 for (let i = 0; i < feature.length; i++)
                                    feature[i].style.opacity = '0.5';
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
                                 if (searchTokens.length === 0) {
                                     for (let i = 0; i < features.length; i++) {
                                         features[i].style.display = '';
                                         features[i].style.opacity = '';
                                     }
                                     for (let i = 0; i < scenarios.length; i++) {
                                         scenarios[i].style.display = '';
                                     }
                                     return;
                                 }
                                 let visibleScenarioCount = 0;
                                 let lastVisibleScenario = null;
                                 for (let i = 0; i < features.length; i++) {
                                     let featureMatch = true;
                                     for (let j = 0; j < searchTokens.length; j++) {
                                         if (!(features[i].textContent.toLowerCase().includes(searchTokens[j]))) {
                                             featureMatch = false;
                                             break;
                                         }
                                     }
                                     if (featureMatch) {
                                         features[i].style.display = '';
                                         features[i].open = true;
                                         let childScenarios = features[i].querySelectorAll('.scenario');
                                         for (let k = 0; k < childScenarios.length; k++) {
                                             childScenarios[k].style.display = '';
                                             visibleScenarioCount++;
                                             lastVisibleScenario = childScenarios[k];
                                         }
                                     } else {
                                         features[i].style.display = 'none';
                                     }
                                 }
                                 if (visibleScenarioCount === 1 && lastVisibleScenario) {
                                     lastVisibleScenario.open = true;
                                 }
                                 for (let i = 0; i < features.length; i++) { features[i].style.opacity = ''; }
                             }
                             
                             function parseSearchTokensIncludingQuotes(str) {
                                 let quoteTokens = [];
                                 for (let match of str.matchAll(/"(.*?)"/g)) {
                                     quoteTokens.push(match[1]);
                                 }
                                 for (let i = 0; i < quoteTokens.length; i++)
                                     str = str.replace('"' + quoteTokens[i] + '"', '');
                                 let simpleTokens = [];
                                 let rawWords = str.trim().split(" ");
                                 for (let i = 0; i < rawWords.length; i++) {
                                     let token = rawWords[i].trim();
                                     simpleTokens.push(token);
                                 }
                                 let tokens = quoteTokens.concat(simpleTokens);
                                 tokens = tokens.filter(x => x !== "");
                                 return tokens;
                             }
                             """;

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
                        </head>
                        <body>
                    """;

        var body = $"<h1>{WebUtility.HtmlEncode(title)}</h1>";

        if (includeTestRunData)
        {
            var numberOfFeatures = features.Length;
            var scenarios = features.SelectMany(x => x.Scenarios).ToArray();
            var passedScenarios = scenarios.Where(x => x.Result == ScenarioResult.Passed).ToArray();
            var skippedScenarios = scenarios.Where(x => x.Result == ScenarioResult.Skipped).ToArray();
            var failedScenarios = scenarios.Where(x => x.Result == ScenarioResult.Failed).ToArray();
            var overallStatus = failedScenarios.Any() ? "Failed" : "Passed";

            body += $"""
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
                    """;
        }

        body += $"""
                 <div class="filters">
                    <label for="toggle-happy-paths">Show Only Happy Paths</label>
                    <input id="toggle-happy-paths" type="checkbox" onchange="toggleHappyPaths(this.checked)" />
                    <div><input id="searchbar" placeholder="Search" onkeyup="search_scenarios()" /></div>
                 </div>
                 """;

        foreach (var feature in features)
        {
            var storyDescription = allScenarios.FirstOrDefault(s => s.StoryTitle == feature.DisplayName)?.StoryDescription;

            body += $"""
                     <details class="feature">
                        <summary class="h2">{WebUtility.HtmlEncode(feature.DisplayName)}{(feature.Endpoint is null ? "" : $" <div class=\"endpoint\">{WebUtility.HtmlEncode(feature.Endpoint)}</div>")}</summary>
                     """;

            if (!string.IsNullOrWhiteSpace(storyDescription))
            {
                body += $"""<div class="story-description">{WebUtility.HtmlEncode(storyDescription)}</div>""";
            }

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName);

            foreach (var scenario in orderedScenarios)
            {
                var failed = scenario.Result == ScenarioResult.Failed;
                body += $"""
                         <details class="scenario{(scenario.IsHappyPath ? " happy-path" : "")}">
                            <summary class="h3{(failed ? " failed" : "")}">{WebUtility.HtmlEncode(scenario.DisplayName)}{(scenario.IsHappyPath ? " <span class=\"label\">Happy Path</span>" : "")}</summary>
                         """;

                // BDDfy steps (Given/When/Then)
                if (scenarioLookup.TryGetValue(scenario.Id, out var scenarioInfo) && scenarioInfo.Steps.Count > 0)
                {
                    body += """<ul class="bdd-steps">""";
                    foreach (var step in scenarioInfo.Steps)
                    {
                        body += $"""<li><span class="keyword">{WebUtility.HtmlEncode(step.Keyword)}</span> {WebUtility.HtmlEncode(step.Text)}</li>""";
                    }
                    body += "</ul>";
                }

                if (failed)
                {
                    body += $"""
                              <details class="failure-result" open>
                                 <summary class="h4">Failure Result</summary>
                                 <pre>
                              Failure Cause: {WebUtility.HtmlEncode(scenario.ErrorMessage)}

                              {WebUtility.HtmlEncode(scenario.ErrorStackTrace)}
                                 </pre>
                              </details>
                              """;
                }

                var diagramsForTest = diagrams.Where(x => x.TestRuntimeId == scenario.Id).ToArray();

                if (diagramsForTest.Length > 0)
                {
                    body += """
                            <details class="example-diagrams" open>
                            <summary class="h4">Sequence Diagrams</summary>
                            """;

                    var lazyLoadAttr = lazyLoadImages ? " loading=\"lazy\"" : "";
                    foreach (var diagram in diagramsForTest)
                    {
                        body += $"""
                                 <details class="example">
                                    <summary class="example-image">
                                        <img{lazyLoadAttr} src="{diagram.ImgSrc}">
                                    </summary>
                                    <div class="raw-plantuml">
                                        <h4>Raw Plant UML</h4>
                                        <pre>{WebUtility.HtmlEncode(diagram.CodeBehind)}</pre>
                                     </div>
                                 </details>
                                 """;
                    }
                    body += "</details>";
                }
                body += "</details>";
            }
            body += "</details>";
        }

        html += body;
        html += """
                    </body>
                </html>
                """;

        WriteFile(html, fileName);
    }

    private static void GenerateBDDfyYamlSpecs(
        Feature[] features,
        BDDfyScenarioInfo[] allScenarios,
        string fileName,
        string title,
        bool generateBlankOnFailedTests = false)
    {
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ScenarioResult.Failed)))
        {
            WriteFile(string.Empty, fileName);
            return;
        }

        var scenarioLookup = allScenarios.ToDictionary(x => x.TestId);
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
                yml.Append("        IsHappyPath: " + scenario.IsHappyPath.ToString().ToLower() + "\n");

                if (scenarioLookup.TryGetValue(scenario.Id, out var scenarioInfo) && scenarioInfo.Steps.Count > 0)
                {
                    yml.Append("        Steps:\n");
                    foreach (var step in scenarioInfo.Steps)
                    {
                        yml.Append("          - " + step.Keyword + " " + step.Text.SanitiseForYml() + "\n");
                    }
                }

                yml.Append("\n");
            }
        }

        WriteFile(yml.ToString(), fileName);
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

    private static void WriteFile(string text, string fileName)
    {
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, text);
    }
}
