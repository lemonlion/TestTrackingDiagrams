using System.Text;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams;

public static class ReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(Feature[] features, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        var diagrams = DiagramsFetcher.GetDiagramsFetcher(options.PlantUmlServerBaseUrl, options.RequestResponsePostProcessor)();

        GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, options.HtmlSpecificationsCustomStyleSheet, $"{options.HtmlSpecificationsFileName}.html", options.SpecificationsTitle, false, generateBlankOnFailedTests: true);
        GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, null, $"{options.HtmlTestRunReportFileName}.html", "Features Report", true);
        GenerateYamlSpecs(diagrams, features, $"{options.YamlSpecificationsFileName}.yml", options.SpecificationsTitle, true);
    }

    public static string GenerateHtmlReport(DiagramsFetcher.DiagramAsCode[] diagrams,
        Feature[] features,
        DateTime startRunTime,
        DateTime endRunTime,
        string? stylesheet,
        string fileName,
        string title,
        bool includeTestRunData,
        bool generateBlankOnFailedTests = false)
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
                             var feature;
                             var scenario;
                             var searchTimeoutId;
                             
                             function search_scenarios() {

                                 if (!feature)
                                    feature = document.getElementsByClassName('feature');
                                    
                                 if (!scenario)
                                    scenario = document.getElementsByClassName('scenario');
                                 
                                 for (i = 0; i < feature.length; i++)
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
                                 console.log("tokens: " + searchTokens);
                                 for (j = 0; j < searchTokens.length; j++)
                                     console.log("searchTokens[" + j + "]: " + searchTokens[j]);
                             
                                 let feature = document.getElementsByClassName('feature');
                                 let scenario = document.getElementsByClassName('scenario');
                             
                                 for (i = 0; i < feature.length; i++) {
                                     let tokenMiss = false;
                                     for (j = 0; j < searchTokens.length; j++) {
                                         if (!(feature[i].textContent.toLowerCase().includes(searchTokens[j]))) {
                                             tokenMiss = true;
                                             break;
                                         }
                                     }
                             
                                     feature[i].style.display = tokenMiss ? "none" : "";
                                 }
                             
                                 for (i = 0; i < scenario.length; i++) {
                                     let tokenMiss = false;
                                     for (j = 0; j < searchTokens.length; j++) {
                                         if (!(scenario[i].textContent.toLowerCase().includes(searchTokens[j]))) {
                                             tokenMiss = true;
                                             break;
                                         }
                                     }
                             
                                     scenario[i].style.display = tokenMiss ? "none" : "";
                                 }
                             
                                 for (i = 0; i < feature.length; i++) { feature[i].style.opacity = ''; }
                             }
                             
                             function parseSearchTokensIncludingQuotes(str) {
                                 let quoteTokens = str.match(/"(.*?)"/);
                                 quoteTokens = quoteTokens?.slice(1, quoteTokens.length) ?? [];
                                 console.log("Number of quoteTokens: " + quoteTokens.length);
                             
                                 if (quoteTokens == null)
                                     quoteTokens = [];
                             
                                 for (i = 0; i < quoteTokens.length; i++)
                                     str = str.replace('\"' + quoteTokens[i] + '\"', '');
                             
                                 let simpleTokens = [];
                                 let rawWords = str.trim().split(" ");
                                 for (i = 0; i < rawWords.length; i++) {
                                     let token = rawWords[i].trim();
                                     simpleTokens.push(token);
                                 }
                             
                                 tokens = quoteTokens.concat(simpleTokens);
                                 tokens = tokens.filter(x => x !== "");
                             
                                 return tokens;
                             }
                             """;

        var combinedStylesheet = $"""
                                 {Stylesheets.HtmlReportStyleSheet}
                                 {stylesheet}
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

        var body = $"<h1>{title}</h1>";

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
                            <tr><td>Start Time:</td><td>{startRunTime.ToShortTimeString()}</td><td>Passed Scenarios: </td><td>{passedScenarios.Length}</td></tr>
                            <tr><td>End Time:</td><td>{endRunTime.ToShortTimeString()}</td><td>Failed Scenarios: </td><td>{failedScenarios.Length}</td></tr>
                            <tr><td>Duration:</td><td>{(endRunTime - startRunTime):g}</td><td>Skipped Scenarios: </td><td>{skippedScenarios.Length}</td></tr>
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
            body += $"""
                     <details class="feature">
                        <summary class="h2">{feature.DisplayName}{(feature.Endpoint is null ? "" : $" <div class=\"endpoint\">{feature.Endpoint}</div>")}</summary>
                     """;

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName);

            foreach (var scenario in orderedScenarios)
            {
                var failed = scenario.Result == ScenarioResult.Failed;
                body += $"""
                         <details class="scenario{(scenario.IsHappyPath ? " happy-path" : "")}">
                            <summary class="h3{(failed ? " failed" : "")}">{scenario.DisplayName}{(scenario.IsHappyPath ? " <span class=\"label\">Happy Path</span>" : "")}</summary>
                         """;

                if (failed)
                {
                    body += $"""
                              <details class="failure-result" open>
                                 <summary class="h4">Failure Result</summary>
                                 <pre>
                              Failure Cause: {scenario.ErrorMessage}
                              
                              {scenario.ErrorStackTrace}
                                 </pre>
                              </details>
                              """;
                }

                var diagramsForTest = diagrams.Where(x => x.TestRuntimeId == scenario.Id).ToArray();


                if (diagramsForTest.Length > 0)
                {
                    body += """
                            <details class="example-diagrams" open>
                            <summary class="h4">Example Diagram</summary>
                            """;

                    foreach (var diagram in diagramsForTest)
                    {
                        body += $"""
                                 <details class="example">
                                    <summary class="example-image">
                                        <img src="{diagram.ImgSrc}">
                                    </summary>
                                    <div class="raw-plantuml">
                                        <h4>Raw Plant UML</h4>
                                        <pre>{diagram.CodeBehind}</pre>
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
                """
        ;

        return WriteFile(html, fileName);
    }

    public static string GenerateYamlSpecs(DiagramsFetcher.DiagramAsCode[] diagrams,
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

    private static string WriteFile(string text, string fileName)
    {
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, text);
        return filePath;
    }
}