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
        Dictionary<string, InternalFlowSegment>? wholeTestSegments = null;
        Dictionary<string, InternalFlowSegment>? perBoundarySegments = null;
        RequestResponseLog[]? trackedLogs = null;
        if (options.InternalFlowTracking)
        {
            trackedLogs = RequestResponseLogger.RequestAndResponseLogs
                .Where(x => !(x?.TrackingIgnore ?? true))
                .ToArray();

            var spans = InternalFlowSpanCollector.CollectSpans(
                options.InternalFlowSpanGranularity,
                options.InternalFlowActivitySources);

            perBoundarySegments = InternalFlowSegmentBuilder.BuildSegments(trackedLogs, spans);

            internalFlowDataScript = DiagramContextMenu.GetInternalFlowConfigScript(options.InternalFlowHasDataBehavior)
                + InternalFlowHtmlGenerator.GenerateSegmentDataScript(
                perBoundarySegments,
                options.InternalFlowDiagramStyle,
                options.InternalFlowShowFlameChart,
                options.InternalFlowFlameChartPosition,
                options.InternalFlowNoDataBehavior);

            if (options.WholeTestFlowVisualization != WholeTestFlowVisualization.None)
            {
                wholeTestSegments = InternalFlowSegmentBuilder.BuildWholeTestSegments(trackedLogs, spans);
            }
        }

        var actions = new List<Action>
        {
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, options.HtmlSpecificationsCustomStyleSheet, $"{options.HtmlSpecificationsFileName}.html", options.SpecificationsTitle, false, generateBlankOnFailedTests: true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript, wholeTestSegments: wholeTestSegments, trackedLogs: trackedLogs, wholeTestVisualization: options.WholeTestFlowVisualization),
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, null, $"{options.HtmlTestRunReportFileName}.html", "Features Report", true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript, wholeTestSegments: wholeTestSegments, trackedLogs: trackedLogs, wholeTestVisualization: options.WholeTestFlowVisualization),
            () => GenerateYamlSpecs(diagrams, features, $"{options.YamlSpecificationsFileName}.yml", options.SpecificationsTitle, true)
        };

        if (options.GenerateComponentDiagram)
        {
            actions.Add(() => ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
                RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
                options,
                perBoundarySegments: perBoundarySegments,
                wholeTestSegments: wholeTestSegments));
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
                GenerateHtmlReport(diagrams, ciFeatures, startRunTime, endRunTime, null, "CiSummaryInteractive.html", "CI Test Run Summary", true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript, wholeTestSegments: wholeTestSegments, trackedLogs: trackedLogs, wholeTestVisualization: options.WholeTestFlowVisualization);
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
        PlantUmlRendering plantUmlRendering = PlantUmlRendering.BrowserJs,
        bool inlineSvgRendering = false,
        bool internalFlowTracking = false,
        string internalFlowDataScript = "",
        Dictionary<string, InternalFlowSegment>? wholeTestSegments = null,
        RequestResponseLog[]? trackedLogs = null,
        WholeTestFlowVisualization wholeTestVisualization = WholeTestFlowVisualization.None)
    {
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ScenarioResult.Failed)))
            return WriteFile(string.Empty, fileName);

        var scenarioFeatureMapHelper = """
                                      var _filterCache;
                                      function fc() {
                                          if (_filterCache) return _filterCache;
                                          var scenarios = document.getElementsByClassName('scenario');
                                          var features = document.getElementsByClassName('feature');
                                          var items = [];
                                          var fMap = new Map();
                                          for (var fi = 0; fi < features.length; fi++) {
                                              var sc = features[fi].getElementsByClassName('scenario');
                                              var arr = [];
                                              for (var si = 0; si < sc.length; si++) {
                                                  var s = sc[si];
                                                  var raw = s.getAttribute('data-dependencies') || '';
                                                  var d = raw ? new Set(raw.split(',')) : new Set();
                                                  var sText = '';
                                                  for (var ci = 0; ci < s.children.length; ci++) {
                                                      if (s.children[ci].classList.contains('whole-test-flow')) continue;
                                                      sText += s.children[ci].textContent;
                                                  }
                                                  var pumlEls = s.querySelectorAll('[data-plantuml],[data-mermaid-source]');
                                                  for (var pi = 0; pi < pumlEls.length; pi++) {
                                                      if (pumlEls[pi].closest('.whole-test-flow')) continue;
                                                      var src = pumlEls[pi].getAttribute('data-plantuml') || pumlEls[pi].getAttribute('data-mermaid-source');
                                                      if (src) sText += ' ' + src;
                                                  }
                                                  var item = { el: s, deps: d, status: s.getAttribute('data-status') || '', isHappy: s.classList.contains('happy-path'), f: features[fi], searchText: sText.toLowerCase() };
                                                  items.push(item);
                                                  arr.push(item);
                                                  fMap.set(s, features[fi]);
                                              }
                                          }
                                          _filterCache = { items: items, features: features, scenarios: scenarios, fMap: fMap };
                                          return _filterCache;
                                      }
                                      """;

        var toggleHappyPathsFunction = """
                                       function toggle_happy_paths(btn) {
                                           btn.classList.toggle('happy-path-active');
                                           filter_happy_paths();
                                       }
                                       
                                       function filter_happy_paths() {
                                           var c = fc();
                                           var active = document.querySelector('.happy-path-toggle.happy-path-active') !== null;
                                       
                                           for (var i = 0; i < c.items.length; i++) {
                                               c.items[i].el.classList.remove('hp-hidden');
                                           }
                                           for (var i = 0; i < c.features.length; i++) {
                                               var f = c.features[i];
                                               f.classList.remove('hp-hidden');
                                               if (f.classList.contains('hp-opened')) {
                                                   f.removeAttribute('open');
                                                   f.classList.remove('hp-opened');
                                               }
                                           }
                                       
                                           if (!active) return;
                                       
                                           var featureVisibleCounts = new Map();
                                           var totalVisible = 0;
                                           for (var i = 0; i < c.features.length; i++) featureVisibleCounts.set(c.features[i], 0);
                                       
                                           for (var i = 0; i < c.items.length; i++) {
                                               var d = c.items[i];
                                               if (!d.isHappy) {
                                                   d.el.classList.add('hp-hidden');
                                               } else if (!d.el.classList.contains('dep-hidden') && !d.el.classList.contains('status-hidden') && !d.el.classList.contains('search-hidden')) {
                                                   featureVisibleCounts.set(d.f, (featureVisibleCounts.get(d.f) || 0) + 1);
                                                   totalVisible++;
                                               }
                                           }
                                       
                                           var shouldOpen = totalVisible <= 10;
                                           for (var i = 0; i < c.features.length; i++) {
                                               var f = c.features[i];
                                               if ((featureVisibleCounts.get(f) || 0) === 0) {
                                                   f.classList.add('hp-hidden');
                                               } else if (shouldOpen && !f.hasAttribute('open')) {
                                                   f.setAttribute('open', '');
                                                   f.classList.add('hp-opened');
                                               }
                                           }
                                       }
                                       """;
        var searchFunction = """
                             var searchTimeoutId;
                             
                             function search_scenarios() {
                                 if (searchTimeoutId)
                                     clearTimeout(searchTimeoutId);
                             
                                 searchTimeoutId = setTimeout(function () {
                                     run_search_scenarios();
                                 }, 300);
                             }
                             
                             function run_search_scenarios() {
                                 var c = fc();
                                 let input = document.getElementById('searchbar').value;
                                 input = input.toLowerCase().trim();
                             
                                 let searchTokens = parseSearchTokensIncludingQuotes(input);
                             
                                 // Clear previous search state
                                 for (let i = 0; i < c.items.length; i++) {
                                     c.items[i].el.classList.remove('search-hidden');
                                     c.items[i].el.removeAttribute('open');
                                 }
                                 for (let i = 0; i < c.features.length; i++) {
                                     c.features[i].classList.remove('search-hidden');
                                     if (c.features[i].classList.contains('search-opened')) {
                                         c.features[i].removeAttribute('open');
                                         c.features[i].classList.remove('search-opened');
                                     }
                                 }
                             
                                 if (searchTokens.length === 0) return;
                             
                                 // Match at the scenario level
                                 let matchingScenarios = [];
                                 for (let i = 0; i < c.items.length; i++) {
                                     let text = c.items[i].searchText;
                                     let allMatch = true;
                                     for (let j = 0; j < searchTokens.length; j++) {
                                         if (!text.includes(searchTokens[j])) {
                                             allMatch = false;
                                             break;
                                         }
                                     }
                                     if (allMatch) {
                                         matchingScenarios.push(s);
                                     } else {
                                         s.classList.add('search-hidden');
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
                                 var totalVisible = matchingScenarios.length;
                                 var shouldOpen = totalVisible <= 10;
                                 for (let i = 0; i < c.features.length; i++) {
                                     let f = c.features[i];
                                     let childScenarios = f.querySelectorAll('.scenario');
                                     let hasVisible = false;
                                     for (let k = 0; k < childScenarios.length; k++) {
                                         if (!childScenarios[k].classList.contains('search-hidden')) {
                                             hasVisible = true;
                                             break;
                                         }
                                     }
                                     if (!hasVisible) {
                                         f.classList.add('search-hidden');
                                     } else if (shouldOpen && !f.hasAttribute('open')) {
                                         f.setAttribute('open', '');
                                         f.classList.add('search-opened');
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

        var dependencyFilterFunction = """
                                       function toggle_dependency(btn) {
                                           btn.classList.toggle('dependency-active');
                                           filter_dependencies();
                                       }
                                       
                                       function filter_dependencies() {
                                           var c = fc();
                                           var activeSet = new Set();
                                           document.querySelectorAll('.dependency-toggle.dependency-active').forEach(function(b) {
                                               activeSet.add(b.getAttribute('data-dependency'));
                                           });
                                       
                                           for (var i = 0; i < c.items.length; i++) {
                                               c.items[i].el.classList.remove('dep-hidden');
                                           }
                                           for (var i = 0; i < c.features.length; i++) {
                                               var f = c.features[i];
                                               f.classList.remove('dep-hidden');
                                               if (f.classList.contains('dep-opened')) {
                                                   f.removeAttribute('open');
                                                   f.classList.remove('dep-opened');
                                               }
                                           }
                                       
                                           if (activeSet.size === 0) return;
                                       
                                           var activeArr = Array.from(activeSet);
                                           var featureVisibleCounts = new Map();
                                           var totalVisible = 0;
                                           for (var i = 0; i < c.features.length; i++) featureVisibleCounts.set(c.features[i], 0);
                                       
                                           for (var i = 0; i < c.items.length; i++) {
                                               var d = c.items[i];
                                               var matchesAll = d.deps.size > 0;
                                               if (matchesAll) {
                                                   for (var j = 0; j < activeArr.length; j++) {
                                                       if (!d.deps.has(activeArr[j])) { matchesAll = false; break; }
                                                   }
                                               }
                                               if (!matchesAll) {
                                                   d.el.classList.add('dep-hidden');
                                               } else if (!d.el.classList.contains('search-hidden') && !d.el.classList.contains('status-hidden') && !d.el.classList.contains('hp-hidden')) {
                                                   featureVisibleCounts.set(d.f, (featureVisibleCounts.get(d.f) || 0) + 1);
                                                   totalVisible++;
                                               }
                                           }
                                       
                                           var shouldOpen = totalVisible <= 10;
                                           for (var i = 0; i < c.features.length; i++) {
                                               var f = c.features[i];
                                               if ((featureVisibleCounts.get(f) || 0) === 0) {
                                                   f.classList.add('dep-hidden');
                                               } else if (shouldOpen && !f.hasAttribute('open')) {
                                                   f.setAttribute('open', '');
                                                   f.classList.add('dep-opened');
                                               }
                                           }
                                       }
                                       """;

        var statusFilterFunction = """
                                   function toggle_status(btn) {
                                       btn.classList.toggle('status-active');
                                       filter_statuses();
                                   }
                                   
                                   function filter_statuses() {
                                       var c = fc();
                                       var activeSet = new Set();
                                       document.querySelectorAll('.status-toggle.status-active').forEach(function(b) {
                                           activeSet.add(b.getAttribute('data-status'));
                                       });
                                   
                                       for (var i = 0; i < c.items.length; i++) {
                                           c.items[i].el.classList.remove('status-hidden');
                                       }
                                       for (var i = 0; i < c.features.length; i++) {
                                           var f = c.features[i];
                                           f.classList.remove('status-hidden');
                                           if (f.classList.contains('status-opened')) {
                                               f.removeAttribute('open');
                                               f.classList.remove('status-opened');
                                           }
                                       }
                                   
                                       if (activeSet.size === 0) return;
                                   
                                       var featureVisibleCounts = new Map();
                                       var totalVisible = 0;
                                       for (var i = 0; i < c.features.length; i++) featureVisibleCounts.set(c.features[i], 0);
                                   
                                       for (var i = 0; i < c.items.length; i++) {
                                           var d = c.items[i];
                                           if (!activeSet.has(d.status)) {
                                               d.el.classList.add('status-hidden');
                                           } else if (!d.el.classList.contains('dep-hidden') && !d.el.classList.contains('search-hidden') && !d.el.classList.contains('hp-hidden')) {
                                               featureVisibleCounts.set(d.f, (featureVisibleCounts.get(d.f) || 0) + 1);
                                               totalVisible++;
                                           }
                                       }
                                   
                                       var shouldOpen = totalVisible <= 10;
                                       for (var i = 0; i < c.features.length; i++) {
                                           var f = c.features[i];
                                           if ((featureVisibleCounts.get(f) || 0) === 0) {
                                               f.classList.add('status-hidden');
                                           } else if (shouldOpen && !f.hasAttribute('open')) {
                                               f.setAttribute('open', '');
                                               f.classList.add('status-opened');
                                           }
                                       }
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
        var contextMenuScript = hasInteractiveDiagrams || internalFlowTracking ? DiagramContextMenu.GetContextMenuScript() : "";
        var contextMenuStyles = hasInteractiveDiagrams || internalFlowTracking ? DiagramContextMenu.GetStyles() : "";
        var inlineSvgStyles = (isInlineSvg || isPlantUmlBrowser) ? DiagramContextMenu.GetInlineSvgStyles() : "";
        var internalFlowPopupStyles = internalFlowTracking ? DiagramContextMenu.GetInternalFlowPopupStyles() : "";
        var internalFlowPopupScript = internalFlowTracking ? DiagramContextMenu.GetInternalFlowPopupScript() : "";
        var flameChartRenderScript = internalFlowTracking ? DiagramContextMenu.GetFlameChartRenderScript() : "";
        var toggleScript = internalFlowTracking ? DiagramContextMenu.GetToggleScript() : "";

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
                                {{scenarioFeatureMapHelper}}
                                {{toggleHappyPathsFunction}}
                                {{searchFunction}}
                                {{dependencyFilterFunction}}
                                {{statusFilterFunction}}
                            </script>
                            {{mermaidScript}}
                            {{plantUmlBrowserScript}}
                            {{contextMenuScript}}
                            {{flameChartRenderScript}}
                            {{internalFlowDataScript}}
                            {{internalFlowPopupScript}}
                            {{toggleScript}}
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

        var diagramsByTestId = diagrams.ToLookup(x => x.TestRuntimeId);

        // Extract dependencies per scenario from diagram source code
        var scenarioDependencies = new Dictionary<string, HashSet<string>>();
        var allDependencies = new HashSet<string>();
        foreach (var feature in features)
        foreach (var scenario in feature.Scenarios)
        {
            var deps = new HashSet<string>();
            foreach (var diagram in diagramsByTestId[scenario.Id])
            {
                foreach (var dep in ExtractDependencies(diagram.CodeBehind, diagramFormat))
                    deps.Add(dep);
            }
            scenarioDependencies[scenario.Id] = deps;
            foreach (var d in deps) allDependencies.Add(d);
        }

        body.Append($"""
                 <div class="filters">
                    <div><input id="searchbar" placeholder="Search" onkeyup="search_scenarios()" /></div>
                    <div class="happy-path-filters"><span class="happy-path-filters-label">Happy Paths:</span><button class="happy-path-toggle" onclick="toggle_happy_paths(this)">Happy Paths Only</button></div>
                 """);

        if (allDependencies.Count > 0)
        {
            body.Append("""<div class="dependency-filters"><span class="dependency-filters-label">Dependencies:</span>""");
            foreach (var dep in allDependencies.OrderBy(d => d))
            {
                body.Append($"""<button class="dependency-toggle" data-dependency="{System.Net.WebUtility.HtmlEncode(dep)}" onclick="toggle_dependency(this)">{System.Net.WebUtility.HtmlEncode(dep)}</button>""");
            }
            body.Append("</div>");
        }

        // Status filter toggles (always show all three statuses)
        var allStatuses = features.SelectMany(f => f.Scenarios).Select(s => s.Result).Distinct().OrderBy(s => s).ToArray();
        {
            body.Append("""<div class="status-filters"><span class="status-filters-label">Status:</span>""");
            foreach (var status in Enum.GetValues<ScenarioResult>().OrderBy(s => s))
            {
                var statusName = status.ToString();
                body.Append($"""<button class="status-toggle" data-status="{statusName}" onclick="toggle_status(this)">{statusName}</button>""");
            }
            body.Append("</div>");
        }

        body.Append("</div>");
        var plantUmlBrowserCounter = 0;

        body.Append("<div id=\"report-content\">");
        foreach (var feature in features)
        {
            var hasFailures = feature.Scenarios.Any(s => s.Result == ScenarioResult.Failed);
            body.Append($"""
                     <details class="feature">
                        <summary class="h2{(hasFailures ? " failed" : "")}">{feature.DisplayName}{(feature.Endpoint is null ? "" : $" <div class=\"endpoint\">{feature.Endpoint}</div>")}</summary>
                     """);

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName);

            foreach (var scenario in orderedScenarios)
            {
                var failed = scenario.Result == ScenarioResult.Failed;
                var depsAttr = scenarioDependencies.TryGetValue(scenario.Id, out var deps) && deps.Count > 0
                    ? $" data-dependencies=\"{System.Net.WebUtility.HtmlEncode(string.Join(",", deps.OrderBy(d => d)))}\""
                    : "";
                var statusAttr = $" data-status=\"{scenario.Result}\"";
                body.Append($"""
                         <details class="scenario{(scenario.IsHappyPath ? " happy-path" : "")}"{depsAttr}{statusAttr}>
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

                if (wholeTestSegments is not null && wholeTestVisualization != WholeTestFlowVisualization.None)
                {
                    var boundaryLogs = trackedLogs?
                        .Where(l => l.TestId == scenario.Id && l.Type == RequestResponseType.Request && l.Timestamp.HasValue)
                        .OrderBy(l => l.Timestamp!.Value)
                        .Select(l => ($"{l.Method.Value}: {l.Uri.PathAndQuery}", l.Timestamp!.Value))
                        .ToArray() ?? [];

                    var wholeTestHtml = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
                        wholeTestSegments, scenario.Id, boundaryLogs, wholeTestVisualization);
                    if (!string.IsNullOrEmpty(wholeTestHtml))
                        body.Append(wholeTestHtml);
                }

                body.Append("</details>");
            }
            body.Append("</details>");
        }
        body.Append("</div>");

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
        try
        {
            File.WriteAllText(filePath, text);
        }
        catch (IOException)
        {
            var fallback = Path.Combine(directory,
                Path.GetFileNameWithoutExtension(fileName) + "2" + Path.GetExtension(fileName));
            File.WriteAllText(fallback, text);
            return fallback;
        }
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

    internal static HashSet<string> ExtractDependencies(string codeBehind, DiagramFormat format)
    {
        var deps = new HashSet<string>();
        if (string.IsNullOrEmpty(codeBehind)) return deps;

        foreach (var line in codeBehind.Split('\n'))
        {
            var trimmed = line.Trim();

            if (format == DiagramFormat.PlantUml)
            {
                // Match: entity "ServiceName" as alias  OR  participant "ServiceName" as alias
                // Skip: actor "Caller" as caller (these are the test caller, not a dependency)
                var match = System.Text.RegularExpressions.Regex.Match(trimmed,
                    @"^(?:entity|participant)\s+""([^""]+)""\s+as\s+");
                if (match.Success)
                    deps.Add(match.Groups[1].Value);
            }
            else // Mermaid
            {
                // Match: participant ServiceName
                // Skip: actor Caller
                var match = System.Text.RegularExpressions.Regex.Match(trimmed,
                    @"^participant\s+(\S+)");
                if (match.Success)
                    deps.Add(match.Groups[1].Value);
            }
        }

        return deps;
    }
}