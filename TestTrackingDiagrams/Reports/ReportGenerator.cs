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
            if (options.PlantUmlRendering is PlantUmlRendering.Server or PlantUmlRendering.Local or PlantUmlRendering.NodeJs)
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
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, null, $"{options.HtmlTestRunReportFileName}.html", GetFeaturesReportTitle(options), true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript, wholeTestSegments: wholeTestSegments, trackedLogs: trackedLogs, wholeTestVisualization: options.WholeTestFlowVisualization),
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
            var (truncatedDiagrams, fullDiagrams) = DefaultDiagramsFetcher.GetCiSummaryDiagrams(fetcherOptions);
            var markdown = CiSummaryGenerator.GenerateMarkdown(features, truncatedDiagrams, fullDiagrams, startRunTime, endRunTime, options.MaxCiSummaryDiagrams,
                options.DiagramFormat, options.PlantUmlServerBaseUrl, options.LocalDiagramRenderer);

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

    internal static string GetFeaturesReportTitle(ReportConfigurationOptions options)
    {
        var prefix = options.ComponentDiagramOptions?.Title;
        if (string.IsNullOrEmpty(prefix))
            prefix = options.FixedNameForReceivingService;
        return string.IsNullOrEmpty(prefix) ? "Features Report" : $"{prefix} - Features Report";
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
                                              for (var si = 0; si < sc.length; si++) {
                                                  var s = sc[si];
                                                  var raw = s.getAttribute('data-dependencies') || '';
                                                  var d = raw ? new Set(raw.split(',')) : new Set();
                                                  var item = { el: s, deps: d, status: s.getAttribute('data-status') || '', isHappy: s.classList.contains('happy-path'), f: features[fi], searchText: s.getAttribute('data-search') || '', hp: false, dep: false, st: false, sr: false, dur: false, cat: false };
                                                  items.push(item);
                                                  fMap.set(s, features[fi]);
                                              }
                                          }
                                          _filterCache = { items: items, features: features, scenarios: scenarios, fMap: fMap };
                                          return _filterCache;
                                      }
                                      function applyVisibility(c) {
                                          for (var i = 0; i < c.items.length; i++) {
                                              var d = c.items[i];
                                              var hidden = d.hp || d.dep || d.st || d.sr || d.dur || d.cat;
                                              d.el.style.display = hidden ? 'none' : '';
                                          }
                                          for (var i = 0; i < c.features.length; i++) {
                                              var f = c.features[i];
                                              var sc = f.getElementsByClassName('scenario');
                                              var hasVisible = false;
                                              for (var j = 0; j < sc.length; j++) {
                                                  if (sc[j].style.display !== 'none') { hasVisible = true; break; }
                                              }
                                              f.style.display = hasVisible ? '' : 'none';
                                          }
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
                                               c.items[i].hp = active && !c.items[i].isHappy;
                                           }
                                           applyVisibility(c);
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
                             
                                 if (searchTokens.length === 0) {
                                     for (let i = 0; i < c.items.length; i++) {
                                         c.items[i].sr = false;
                                         c.items[i].el.removeAttribute('open');
                                     }
                                     applyVisibility(c);
                                     return;
                                 }
                             
                                 // Match at the scenario level
                                 let matchCount = 0;
                                 let singleMatch = null;
                                 for (let i = 0; i < c.items.length; i++) {
                                     let text = c.items[i].searchText;
                                     let allMatch = true;
                                     for (let j = 0; j < searchTokens.length; j++) {
                                         if (!text.includes(searchTokens[j])) {
                                             allMatch = false;
                                             break;
                                         }
                                     }
                                     c.items[i].sr = !allMatch;
                                     if (allMatch) {
                                         matchCount++;
                                         singleMatch = c.items[i].el;
                                     }
                                 }
                             
                                 applyVisibility(c);
                             
                                 // Single match: expand scenario with diagrams
                                 if (matchCount === 1 && singleMatch) {
                                     singleMatch.setAttribute('open', '');
                                     let diagrams = singleMatch.querySelector('details.example-diagrams');
                                     if (diagrams) diagrams.setAttribute('open', '');
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
                                       
                                           if (activeSet.size === 0) {
                                               for (var i = 0; i < c.items.length; i++) c.items[i].dep = false;
                                               applyVisibility(c);
                                               return;
                                           }
                                       
                                           var activeArr = Array.from(activeSet);
                                           for (var i = 0; i < c.items.length; i++) {
                                               var d = c.items[i];
                                               var matchesAll = d.deps.size > 0;
                                               if (matchesAll) {
                                                   for (var j = 0; j < activeArr.length; j++) {
                                                       if (!d.deps.has(activeArr[j])) { matchesAll = false; break; }
                                                   }
                                               }
                                               d.dep = !matchesAll;
                                           }
                                           applyVisibility(c);
                                       }
                                       """;

        var categoryFilterFunction = """
                                     function toggle_category(btn) {
                                         var cat = btn.getAttribute('data-category');
                                         if (cat === '') {
                                             // "All" button: deactivate all specific categories
                                             document.querySelectorAll('.category-toggle').forEach(function(b) { b.classList.remove('category-active'); });
                                             btn.classList.add('category-active');
                                         } else {
                                             // Deactivate "All" button, toggle this one
                                             var allBtn = document.querySelector('.category-toggle[data-category=""]');
                                             if (allBtn) allBtn.classList.remove('category-active');
                                             btn.classList.toggle('category-active');
                                             // If nothing is active, re-activate "All"
                                             if (document.querySelectorAll('.category-toggle.category-active').length === 0) {
                                                 if (allBtn) allBtn.classList.add('category-active');
                                             }
                                         }
                                         filter_categories();
                                     }
                                     
                                     function filter_categories() {
                                         var c = fc();
                                         var allActive = document.querySelector('.category-toggle.category-active[data-category=""]') !== null;
                                         if (allActive) {
                                             for (var i = 0; i < c.items.length; i++) c.items[i].cat = false;
                                             applyVisibility(c);
                                             return;
                                         }
                                         var activeSet = new Set();
                                         document.querySelectorAll('.category-toggle.category-active').forEach(function(b) {
                                             activeSet.add(b.getAttribute('data-category'));
                                         });
                                         for (var i = 0; i < c.items.length; i++) {
                                             var raw = c.items[i].el.getAttribute('data-categories') || '';
                                             var cats = raw ? new Set(raw.split(',')) : new Set();
                                             if (activeSet.has('__uncategorized__') && cats.size === 0) {
                                                 c.items[i].cat = false;
                                             } else {
                                                 var match = false;
                                                 activeSet.forEach(function(a) { if (a !== '__uncategorized__' && cats.has(a)) match = true; });
                                                 c.items[i].cat = !match;
                                             }
                                         }
                                         applyVisibility(c);
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
                                   
                                       if (activeSet.size === 0) {
                                           for (var i = 0; i < c.items.length; i++) c.items[i].st = false;
                                           applyVisibility(c);
                                           return;
                                       }
                                   
                                       for (var i = 0; i < c.items.length; i++) {
                                           c.items[i].st = !activeSet.has(c.items[i].status);
                                       }
                                       applyVisibility(c);
                                   }
                                   """;

        // Collapse/Expand All
        var collapseExpandAllFunction = """
                                        function toggle_expand_collapse(btn, selector, expandLabel, collapseLabel) {
                                            var expanding = btn.textContent === expandLabel;
                                            var els = document.querySelectorAll(selector);
                                            for (var i = 0; i < els.length; i++) { if (expanding) els[i].setAttribute('open', ''); else els[i].removeAttribute('open'); }
                                            btn.textContent = expanding ? collapseLabel : expandLabel;
                                        }
                                        """;

        var sortTableFunction = """
                                function sort_table(col) {
                                    var table = document.querySelector('.feature-summary-table');
                                    if (!table) return;
                                    var tbody = table.querySelector('tbody');
                                    var rows = Array.from(tbody.querySelectorAll('tr'));
                                    var asc = table.getAttribute('data-sort-col') === '' + col && table.getAttribute('data-sort-dir') !== 'asc';
                                    rows.sort(function(a, b) {
                                        var ac = a.cells[col].textContent.trim();
                                        var bc = b.cells[col].textContent.trim();
                                        var an = parseFloat(ac), bn = parseFloat(bc);
                                        if (!isNaN(an) && !isNaN(bn)) return asc ? an - bn : bn - an;
                                        return asc ? ac.localeCompare(bc) : bc.localeCompare(ac);
                                    });
                                    for (var i = 0; i < rows.length; i++) tbody.appendChild(rows[i]);
                                    table.setAttribute('data-sort-col', col);
                                    table.setAttribute('data-sort-dir', asc ? 'asc' : 'desc');
                                }
                                """;



        // Copy scenario name
        var copyScenarioNameFunction = """
                                       function copy_scenario_name(btn, evt) {
                                           evt.stopPropagation();
                                           evt.preventDefault();
                                           var name = btn.getAttribute('data-scenario-name');
                                           navigator.clipboard.writeText(name).then(function() {
                                               var orig = btn.textContent;
                                               btn.textContent = '\u2713';
                                               setTimeout(function() { btn.textContent = orig; }, 1500);
                                           });
                                       }
                                       """;

        // Jump to failure
        var hasFailures = features.SelectMany(f => f.Scenarios).Any(s => s.Result == ScenarioResult.Failed);
        var failureCount = features.SelectMany(f => f.Scenarios).Count(s => s.Result == ScenarioResult.Failed);
        var jumpToFailureFunction = """
                                    var _failureIndex = -1;
                                    function jump_to_next_failure() {
                                        var failures = document.querySelectorAll('details.scenario[data-status="Failed"]');
                                        if (failures.length === 0) return;
                                        _failureIndex = (_failureIndex + 1) % failures.length;
                                        var el = failures[_failureIndex];
                                        var feature = el.closest('details.feature');
                                        if (feature) feature.setAttribute('open', '');
                                        el.setAttribute('open', '');
                                        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                        var counter = document.getElementById('failure-counter');
                                        if (counter) counter.textContent = '(' + (_failureIndex + 1) + '/' + failures.length + ')';
                                    }
                                    """;

        // Duration filter
        var hasDurations = features.SelectMany(f => f.Scenarios).Any(s => s.Duration.HasValue);
        var durationFilterFunction = """
                                     function filter_duration() {
                                         var c = fc();
                                         var input = document.getElementById('duration-threshold');
                                         if (!input) return;
                                         var threshold = parseFloat(input.value);
                                         if (isNaN(threshold) || threshold <= 0) {
                                             for (var i = 0; i < c.items.length; i++) c.items[i].dur = false;
                                             applyVisibility(c);
                                             update_url_hash();
                                             return;
                                         }
                                         var thresholdMs = threshold * 1000;
                                         for (var i = 0; i < c.items.length; i++) {
                                             var ms = parseFloat(c.items[i].el.getAttribute('data-duration-ms') || '0');
                                             c.items[i].dur = ms > 0 && ms < thresholdMs;
                                         }
                                         applyVisibility(c);
                                         update_url_hash();
                                     }
                                     function set_percentile(btn) {
                                         var wasActive = btn.classList.contains('percentile-active');
                                         document.querySelectorAll('.percentile-btn').forEach(function(b) { b.classList.remove('percentile-active'); });
                                         var input = document.getElementById('duration-threshold');
                                         var customWrap = document.getElementById('custom-duration-wrap');
                                         if (wasActive) {
                                             if (input) { input.value = ''; filter_duration(); }
                                             if (customWrap) customWrap.style.display = 'none';
                                             return;
                                         }
                                         var isCustom = btn.getAttribute('data-custom') === '1';
                                         if (isCustom) {
                                             btn.classList.add('percentile-active');
                                             if (customWrap) customWrap.style.display = 'inline-flex';
                                             if (input) { input.focus(); if (input.value) filter_duration(); }
                                         } else {
                                             if (customWrap) customWrap.style.display = 'none';
                                             var ms = parseFloat(btn.getAttribute('data-threshold-ms'));
                                             if (input) { input.value = (ms / 1000).toFixed(1); filter_duration(); }
                                             btn.classList.add('percentile-active');
                                         }
                                     }
                                     """;

        // Export filtered view
        var exportFunction = """
                             function export_html() {
                                 var c = fc();
                                 var head = document.querySelector('head');
                                 var headHtml = head ? head.innerHTML : '';
                                 var html = '<html><head>' + headHtml + '</head><body>';
                                 html += '<h1>Filtered Report</h1>';
                                 for (var i = 0; i < c.features.length; i++) {
                                     if (c.features[i].style.display === 'none') continue;
                                     html += c.features[i].outerHTML;
                                 }
                                 html += '</body></html>';
                                 var blob = new Blob([html], { type: 'text/html' });
                                 var a = document.createElement('a');
                                 a.href = URL.createObjectURL(blob);
                                 a.download = 'filtered-report.html';
                                 a.click();
                                 URL.revokeObjectURL(a.href);
                             }
                             function export_csv() {
                                 var c = fc();
                                 var lines = ['Feature,Scenario,Status,Duration'];
                                 for (var i = 0; i < c.items.length; i++) {
                                     var d = c.items[i];
                                     if (d.el.style.display === 'none') continue;
                                     var f = d.f;
                                     var fname = (f.querySelector('summary.h2') || f.querySelector('summary')).textContent.trim();
                                     var sname = (d.el.querySelector('summary.h3') || d.el.querySelector('summary')).textContent.trim();
                                     var dur = d.el.getAttribute('data-duration-ms') || '';
                                     lines.push('"' + fname.replace(/"/g,'""') + '","' + sname.replace(/"/g,'""') + '","' + d.status + '","' + dur + '"');
                                 }
                                 var blob = new Blob([lines.join('\n')], { type: 'text/csv' });
                                 var a = document.createElement('a');
                                 a.href = URL.createObjectURL(blob);
                                 a.download = 'filtered-report.csv';
                                 a.click();
                                 URL.revokeObjectURL(a.href);
                             }
                             """;

        // Persistent filter state
        // No-op stubs (localStorage persistence removed)
        var persistentFilterFunction = """
                                       function save_filter_state() {}
                                       function restore_filter_state() {}
                                       """;

        // URL-anchored filters
        var urlHashFunction = """
                              function update_url_hash() {
                                  var parts = [];
                                  var search = document.getElementById('searchbar');
                                  if (search && search.value) parts.push('q=' + encodeURIComponent(search.value));
                                  var statuses = [];
                                  document.querySelectorAll('.status-toggle.status-active').forEach(function(b) { statuses.push(b.getAttribute('data-status')); });
                                  if (statuses.length > 0) parts.push('status=' + statuses.join(','));
                                  var deps = [];
                                  document.querySelectorAll('.dependency-toggle.dependency-active').forEach(function(b) { deps.push(b.getAttribute('data-dependency')); });
                                  if (deps.length > 0) parts.push('deps=' + encodeURIComponent(deps.join(',')));
                                  if (document.querySelector('.happy-path-toggle.happy-path-active')) parts.push('hp=1');
                                  var dur = document.getElementById('duration-threshold');
                                  if (dur && dur.value) parts.push('dur=' + dur.value);
                                  var activeP = document.querySelector('.percentile-btn.percentile-active');
                                  if (activeP) parts.push('pctl=' + encodeURIComponent(activeP.textContent));
                                  var hash = parts.length > 0 ? '#' + parts.join('&') : '';
                                  history.replaceState(null, '', window.location.pathname + window.location.search + hash);
                              }
                              function parse_url_hash() {
                                  var hash = window.location.hash.substring(1);
                                  if (!hash) return;
                                  // Check if it's a scenario anchor (starts with 'scenario-')
                                  if (hash.indexOf('scenario-') === 0) {
                                      var el = document.getElementById(hash);
                                      if (el) {
                                          var feature = el.closest('details.feature');
                                          if (feature) feature.setAttribute('open', '');
                                          el.setAttribute('open', '');
                                          el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                      }
                                      return;
                                  }
                                  var params = {};
                                  hash.split('&').forEach(function(p) {
                                      var kv = p.split('=');
                                      if (kv.length === 2) params[kv[0]] = decodeURIComponent(kv[1]);
                                  });
                                  if (params.q) {
                                      var sb = document.getElementById('searchbar');
                                      if (sb) { sb.value = params.q; run_search_scenarios(); }
                                  }
                                  if (params.status) {
                                      params.status.split(',').forEach(function(s) {
                                          var btn = document.querySelector('.status-toggle[data-status="' + s + '"]');
                                          if (btn) btn.classList.add('status-active');
                                      });
                                      filter_statuses();
                                  }
                                  if (params.deps) {
                                      params.deps.split(',').forEach(function(d) {
                                          var btn = document.querySelector('.dependency-toggle[data-dependency="' + d + '"]');
                                          if (btn) btn.classList.add('dependency-active');
                                      });
                                      filter_dependencies();
                                  }
                                  if (params.hp === '1') {
                                      var hp = document.querySelector('.happy-path-toggle');
                                      if (hp) { hp.classList.add('happy-path-active'); filter_happy_paths(); }
                                  }
                                  if (params.pctl) {
                                      document.querySelectorAll('.percentile-btn').forEach(function(b) {
                                          if (b.textContent === params.pctl) {
                                              b.classList.add('percentile-active');
                                              if (b.getAttribute('data-custom') === '1') {
                                                  var cw = document.getElementById('custom-duration-wrap');
                                                  if (cw) cw.style.display = 'inline-flex';
                                              }
                                          }
                                      });
                                  }
                                  if (params.dur) {
                                      var dur = document.getElementById('duration-threshold');
                                      if (dur) { dur.value = params.dur; filter_duration(); }
                                  }
                              }
                              """;

        // Keyboard navigation
        var keyboardNavigationFunction = """
                                         document.addEventListener('keydown', function(e) {
                                             if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
                                             if (e.key === '/') {
                                                 e.preventDefault();
                                                 var sb = document.getElementById('searchbar');
                                                 if (sb) sb.focus();
                                                 return;
                                             }
                                             var scenarios = Array.from(document.querySelectorAll('details.scenario')).filter(function(s) { return s.style.display !== 'none'; });
                                             if (scenarios.length === 0) return;
                                             var focused = document.querySelector('.scenario-focused');
                                             var idx = focused ? scenarios.indexOf(focused) : -1;
                                             if (e.key === 'ArrowDown') {
                                                 e.preventDefault();
                                                 if (focused) focused.classList.remove('scenario-focused');
                                                 idx = (idx + 1) % scenarios.length;
                                                 scenarios[idx].classList.add('scenario-focused');
                                                 scenarios[idx].scrollIntoView({ behavior: 'smooth', block: 'center' });
                                                 var feature = scenarios[idx].closest('details.feature');
                                                 if (feature) feature.setAttribute('open', '');
                                             } else if (e.key === 'ArrowUp') {
                                                 e.preventDefault();
                                                 if (focused) focused.classList.remove('scenario-focused');
                                                 idx = idx <= 0 ? scenarios.length - 1 : idx - 1;
                                                 scenarios[idx].classList.add('scenario-focused');
                                                 scenarios[idx].scrollIntoView({ behavior: 'smooth', block: 'center' });
                                                 var feature = scenarios[idx].closest('details.feature');
                                                 if (feature) feature.setAttribute('open', '');
                                             } else if (e.key === 'Enter' && focused) {
                                                 e.preventDefault();
                                                 if (focused.hasAttribute('open')) focused.removeAttribute('open');
                                                 else focused.setAttribute('open', '');
                                             }
                                         });
                                         """;

        // Deep link + init script
        var initScript = """
                         document.addEventListener('DOMContentLoaded', function() {
                             // Restore filters from URL hash if present
                             if (window.location.hash && window.location.hash.length > 1) {
                                 parse_url_hash();
                             }
                         });
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
        var collapsibleNotesScript = isPlantUmlBrowser ? DiagramContextMenu.GetCollapsibleNotesScript() : "";
        var collapsibleNotesStyles = isPlantUmlBrowser ? DiagramContextMenu.GetCollapsibleNotesStyles() : "";
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
                                {{collapsibleNotesStyles}}
                                {{internalFlowPopupStyles}}
                            </style>
                            <script>
                                {{scenarioFeatureMapHelper}}
                                {{toggleHappyPathsFunction}}
                                {{searchFunction}}
                                {{dependencyFilterFunction}}
                                {{categoryFilterFunction}}
                                {{statusFilterFunction}}
                                {{collapseExpandAllFunction}}
                                {{sortTableFunction}}
                                {{copyScenarioNameFunction}}
                                {{jumpToFailureFunction}}
                                {{durationFilterFunction}}
                                {{exportFunction}}
                                {{persistentFilterFunction}}
                                {{urlHashFunction}}
                                {{keyboardNavigationFunction}}
                                {{initScript}}
                            </script>
                            {{mermaidScript}}
                            {{plantUmlBrowserScript}}
                            {{collapsibleNotesScript}}
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

            // Feature summary table (collapsible, above execution summary)
            var hasAnySteps = features.Any(f => f.Scenarios.Any(s => s.Steps is { Length: > 0 }));
            body.Append("<details class=\"features-summary-details\"><summary class=\"h2\">Features Summary</summary>");
            body.Append("<div class=\"features-summary-table-wrapper\">");
            body.Append("<table class=\"feature-summary-table\"><thead><tr>");
            body.Append("<th onclick=\"sort_table(0)\">Feature</th>");
            body.Append("<th onclick=\"sort_table(1)\">Scenarios</th>");
            body.Append("<th onclick=\"sort_table(2)\">Passed</th>");
            body.Append("<th onclick=\"sort_table(3)\">Failed</th>");
            body.Append("<th onclick=\"sort_table(4)\">Skipped</th>");
            if (hasAnySteps)
                body.Append("<th onclick=\"sort_table(5)\">Steps</th>");
            body.Append("</tr></thead><tbody>");

            foreach (var feature in features)
            {
                var totalSc = feature.Scenarios.Length;
                var passedSc = feature.Scenarios.Count(s => s.Result == ScenarioResult.Passed);
                var failedSc = feature.Scenarios.Count(s => s.Result == ScenarioResult.Failed);
                var skippedSc = feature.Scenarios.Count(s => s.Result is ScenarioResult.Skipped or ScenarioResult.Bypassed or ScenarioResult.Ignored);
                var featureHasFail = failedSc > 0;

                body.Append($"<tr{(featureHasFail ? " class=\"failed\"" : "")}>");
                body.Append($"<td>{System.Net.WebUtility.HtmlEncode(feature.DisplayName)}</td>");
                body.Append($"<td>{totalSc}</td>");
                body.Append($"<td>{passedSc}</td>");
                body.Append($"<td>{failedSc}</td>");
                body.Append($"<td>{skippedSc}</td>");

                if (hasAnySteps)
                {
                    var allSteps = feature.Scenarios
                        .Where(s => s.Steps is not null)
                        .SelectMany(s => s.Steps!)
                        .ToArray();
                    var stepCount = CountStepsRecursive(allSteps);
                    body.Append($"<td>{stepCount}</td>");
                }

                body.Append("</tr>");
            }

            body.Append("</tbody></table>");
            body.Append("</div>");
            body.Append("</details>");

            body.Append($"""
                    <div class="header-row">
                    <div class="test-execution-summary">
                        <h2>Test Execution Summary</h2>
                        <table>
                            <tr><td colspan="2" class="column-header">Execution</td><td colspan="2" class="column-header">Content</td></tr>
                            <tr><td>Overall status:</td><td>{overallStatus}</td><td>Features: </td><td>{numberOfFeatures}</td></tr>
                            <tr><td>Start Date:</td><td>{startRunTime:yyyy-MM-dd} (UTC)</td><td>Scenarios: </td><td>{scenarios.Length}</td></tr>
                            <tr><td>Start Time:</td><td>{startRunTime:HH:mm:ss} (UTC)</td><td>Passed Scenarios: </td><td>{passedScenarios.Length}</td></tr>
                            <tr><td>End Time:</td><td>{endRunTime:HH:mm:ss} (UTC)</td><td>Failed Scenarios: </td><td>{failedScenarios.Length}</td></tr>
                            <tr><td>Duration:</td><td>{FormatDuration(endRunTime - startRunTime)}</td><td>Skipped Scenarios: </td><td>{skippedScenarios.Length}</td></tr>
                        </table>
                    </div>
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
                 <div class="filtering-box">
                    <h2>Filtering</h2>
                    <div class="filters">
                    <div class="filter-search"><input id="searchbar" placeholder="Search" onkeyup="search_scenarios()" /></div>
                    <div class="filter-row">
                 """);

        // Status filter toggles (always show all three statuses)
        {
            body.Append("""<div class="status-filters"><span class="status-filters-label">Status:</span>""");
            foreach (var status in Enum.GetValues<ScenarioResult>().OrderBy(s => s))
            {
                var statusName = status.ToString();
                body.Append($"""<button class="status-toggle" data-status="{statusName}" onclick="toggle_status(this)">{statusName}</button>""");
            }
            body.Append("</div>");
        }

        body.Append("""
                    <div class="happy-path-filters"><span class="happy-path-filters-label">Happy Paths:</span><button class="happy-path-toggle" onclick="toggle_happy_paths(this)">Happy Paths Only</button></div>
                 """);

        body.Append("</div>"); // close filter-row

        if (allDependencies.Count > 0)
        {
            body.Append("""<div class="dependency-filters"><span class="dependency-filters-label">Dependencies:</span>""");
            foreach (var dep in allDependencies.OrderBy(d => d))
            {
                body.Append($"""<button class="dependency-toggle" data-dependency="{System.Net.WebUtility.HtmlEncode(dep)}" onclick="toggle_dependency(this)">{System.Net.WebUtility.HtmlEncode(dep)}</button>""");
            }
            body.Append("</div>");
        }

        // Category filter (only shown when scenarios have category data)
        var allCategories = features.SelectMany(f => f.Scenarios)
            .Where(s => s.Categories is { Length: > 0 })
            .SelectMany(s => s.Categories!)
            .Distinct()
            .OrderBy(c => c)
            .ToArray();
        if (allCategories.Length > 0)
        {
            body.Append("""<div class="category-filters"><span class="category-filters-label">Categories:</span>""");
            body.Append("""<button class="category-toggle category-active" data-category="" onclick="toggle_category(this)">All</button>""");
            foreach (var cat in allCategories)
            {
                body.Append($"""<button class="category-toggle" data-category="{System.Net.WebUtility.HtmlEncode(cat)}" onclick="toggle_category(this)">{System.Net.WebUtility.HtmlEncode(cat)}</button>""");
            }
            body.Append("""<button class="category-toggle" data-category="__uncategorized__" onclick="toggle_category(this)">Uncategorized</button>""");
            body.Append("</div>");
        }

        // Duration filter (only shown when scenarios have duration data)
        if (hasDurations)
        {
            var durationsMs = features.SelectMany(f => f.Scenarios)
                .Where(s => s.Duration.HasValue)
                .Select(s => s.Duration!.Value.TotalMilliseconds)
                .OrderBy(d => d)
                .ToArray();
            var p50Ms = durationsMs.Length > 0 ? durationsMs[(int)(durationsMs.Length * 0.50)] : 0;
            var p90Ms = durationsMs.Length > 0 ? durationsMs[(int)(durationsMs.Length * 0.90)] : 0;
            var p95Ms = durationsMs.Length > 0 ? durationsMs[(int)(durationsMs.Length * 0.95)] : 0;
            var p99Ms = durationsMs.Length > 0 ? durationsMs[(int)(durationsMs.Length * 0.99)] : 0;

            body.Append($"""<div class="duration-filters" data-p50="{p50Ms:F0}" data-p90="{p90Ms:F0}" data-p95="{p95Ms:F0}" data-p99="{p99Ms:F0}"><span class="duration-filters-label">Duration Greater Than:</span><button class="percentile-btn" data-threshold-ms="{p50Ms:F0}" onclick="set_percentile(this)">P50 ({FormatDurationBadge(TimeSpan.FromMilliseconds(p50Ms))})</button><button class="percentile-btn" data-threshold-ms="{p90Ms:F0}" onclick="set_percentile(this)">P90 ({FormatDurationBadge(TimeSpan.FromMilliseconds(p90Ms))})</button><button class="percentile-btn" data-threshold-ms="{p95Ms:F0}" onclick="set_percentile(this)">P95 ({FormatDurationBadge(TimeSpan.FromMilliseconds(p95Ms))})</button><button class="percentile-btn" data-threshold-ms="{p99Ms:F0}" onclick="set_percentile(this)">P99 ({FormatDurationBadge(TimeSpan.FromMilliseconds(p99Ms))})</button><button class="percentile-btn" data-custom="1" onclick="set_percentile(this)">Custom</button><span id="custom-duration-wrap" style="display:none;align-items:center;gap:0.3em"><input id="duration-threshold" type="number" step="0.1" min="0" placeholder="seconds" onchange="filter_duration()" /><span class="duration-filters-unit">seconds</span></span></div>""");
        }

        body.Append("</div>"); // close filters
        body.Append("</div>"); // close filtering-box
        body.Append("</div>"); // close header-row

        // Toolbar row: Export + Expand on left, Details/Headers on right
        body.Append("""<div class="toolbar-row">""");
        body.Append("""<div class="export-filtered"><button class="export-btn" onclick="export_html()">Export HTML</button><button class="export-btn" onclick="export_csv()">Export CSV</button><span style="width:1.5em"></span><button class="collapse-expand-all" onclick="toggle_expand_collapse(this, 'details.feature', 'Expand All Features', 'Collapse All Features')">Expand All Features</button><button class="collapse-expand-all" onclick="toggle_expand_collapse(this, 'details.scenario', 'Expand All Scenarios', 'Collapse All Scenarios')">Expand All Scenarios</button></div>""");
        body.Append("""<div class="toolbar-right">""");
        if (isPlantUmlBrowser)
        {
            body.Append("""<span class="details-radio"><span class="details-radio-label">Details:</span><button class="details-radio-btn details-active" data-state="expanded" onclick="window._setReportDetails('expanded')">Expanded</button><button class="details-radio-btn" data-state="collapsed" onclick="window._setReportDetails('collapsed')">Collapsed</button><button class="details-radio-btn" data-state="truncated" onclick="window._setReportDetails('truncated')">Truncated</button><select class="truncate-lines-select" disabled onchange="window._setTruncateLines(this)"><option value="3">3</option><option value="4">4</option><option value="5">5</option><option value="10">10</option><option value="15">15</option><option value="20" selected>20</option><option value="25">25</option><option value="30">30</option><option value="35">35</option><option value="40">40</option></select><span class="truncate-lines-label">lines</span></span>""");
            body.Append("""<span class="headers-radio"><span class="details-radio-label">Headers:</span><button class="details-radio-btn headers-radio-btn details-active" data-hstate="shown" onclick="window._setReportHeaders('shown')">Shown</button><button class="details-radio-btn headers-radio-btn" data-hstate="hidden" onclick="window._setReportHeaders('hidden')">Hidden</button></span>""");
        }
        body.Append("</div>");
        body.Append("</div>");

        var plantUmlBrowserCounter = 0;

        // Pre-compute median span count for outlier detection
        var medianSpanCount = 0;
        if (wholeTestSegments is not null && wholeTestSegments.Count > 0)
        {
            var spanCounts = wholeTestSegments.Values
                .Where(s => s.Spans.Length > 0)
                .Select(s => s.Spans.Length)
                .OrderBy(c => c)
                .ToArray();
            if (spanCounts.Length > 0)
                medianSpanCount = spanCounts[(spanCounts.Length - 1) / 2];
        }

        body.Append("<div id=\"report-content\">");
        foreach (var feature in features)
        {
            var featureHasFailures = feature.Scenarios.Any(s => s.Result == ScenarioResult.Failed);
            body.Append($"""
                     <details class="feature">
                        <summary class="h2{(featureHasFailures ? " failed" : "")}">{feature.DisplayName}{(feature.Endpoint is null ? "" : $" <div class=\"endpoint\">{feature.Endpoint}</div>")}{(feature.Labels is { Length: > 0 } fl ? string.Concat(fl.Select(l => $" <span class=\"label\">{System.Net.WebUtility.HtmlEncode(l)}</span>")) : "")}</summary>
                     """);

            if (feature.Description is not null)
            {
                body.Append($"""<div class="feature-description">{System.Net.WebUtility.HtmlEncode(feature.Description)}</div>""");
            }

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName);

            foreach (var scenario in orderedScenarios)
            {
                var failed = scenario.Result == ScenarioResult.Failed;
                var depsAttr = scenarioDependencies.TryGetValue(scenario.Id, out var deps) && deps.Count > 0
                    ? $" data-dependencies=\"{System.Net.WebUtility.HtmlEncode(string.Join(",", deps.OrderBy(d => d)))}\""
                    : "";
                var statusAttr = $" data-status=\"{scenario.Result}\"";

                // Duration attributes and badge
                var durationAttr = "";
                var durationBadge = "";
                if (scenario.Duration.HasValue)
                {
                    var durationMs = scenario.Duration.Value.TotalMilliseconds;
                    durationAttr = $" data-duration-ms=\"{durationMs:F0}\"";
                    var durationClass = durationMs < 2000 ? "duration-fast" : durationMs < 5000 ? "duration-moderate" : "duration-slow";
                    durationBadge = $" <span class=\"duration-badge {durationClass}\">{FormatDurationBadge(scenario.Duration.Value)}</span>";
                }

                // Deep link anchor ID
                var anchorId = GenerateScenarioAnchorId(scenario.DisplayName);

                // Pre-build searchable text: scenario name + error info + diagram sources
                var searchParts = new List<string> { scenario.DisplayName };
                if (failed && scenario.ErrorMessage is not null) searchParts.Add(scenario.ErrorMessage);
                var diagramsForSearch = diagramsByTestId[scenario.Id].ToArray();
                foreach (var d in diagramsForSearch) searchParts.Add(d.CodeBehind);
                var searchAttr = $" data-search=\"{System.Net.WebUtility.HtmlEncode(string.Join(" ", searchParts).ToLowerInvariant())}\"";

                var categoriesAttr = scenario.Categories is { Length: > 0 }
                    ? $" data-categories=\"{System.Net.WebUtility.HtmlEncode(string.Join(",", scenario.Categories))}\""
                    : "";

                var encodedName = System.Net.WebUtility.HtmlEncode(scenario.DisplayName);
                var scenarioLabelsHtml = scenario.Labels is { Length: > 0 }
                    ? string.Concat(scenario.Labels.Select(l => $" <span class=\"label\">{System.Net.WebUtility.HtmlEncode(l)}</span>"))
                    : "";

                body.Append($"""
                         <details class="scenario{(scenario.IsHappyPath ? " happy-path" : "")}"{depsAttr}{statusAttr}{searchAttr}{durationAttr}{categoriesAttr} id="{anchorId}" tabindex="0">
                            <summary class="h3{(failed ? " failed" : "")}">{scenario.DisplayName}{(scenario.IsHappyPath ? " <span class=\"label\">Happy Path</span>" : "")}{scenarioLabelsHtml}{durationBadge}<button class="copy-scenario-name" title="Copy scenario name" data-scenario-name="{encodedName}" onclick="copy_scenario_name(this, event)">&#128203;</button><a class="scenario-link" href="#{anchorId}" title="Link to this scenario" onclick="event.stopPropagation()">&#128279;</a></summary>
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

                if (scenario.Steps is { Length: > 0 })
                {
                    body.Append("<div class=\"scenario-steps\">");
                    foreach (var step in scenario.Steps)
                    {
                        RenderStep(body, step);
                    }
                    body.Append("</div>");
                }

                var diagramsForTest = diagramsByTestId[scenario.Id].ToArray();

                // Get whole-test-flow content (activity + flame) if available
                (string ActivityHtml, string FlameHtml, int SpanCount)? wholeTestContent = null;
                if (wholeTestSegments is not null && wholeTestVisualization != WholeTestFlowVisualization.None)
                {
                    var boundaryLogs = trackedLogs?
                        .Where(l => l.TestId == scenario.Id && l.Type == RequestResponseType.Request && l.Timestamp.HasValue)
                        .OrderBy(l => l.Timestamp!.Value)
                        .Select(l => ($"{l.Method.Value}: {l.Uri.PathAndQuery}", l.Timestamp!.Value))
                        .ToArray() ?? [];

                    wholeTestContent = InternalFlowHtmlGenerator.GetWholeTestFlowContent(
                        wholeTestSegments, scenario.Id, boundaryLogs, wholeTestVisualization);
                }

                var hasSequenceDiagrams = diagramsForTest.Length > 0;
                var hasWholeTestFlow = wholeTestContent is not null;

                // Span count warning for outliers (>= 10x median)
                var spanWarning = "";
                if (hasWholeTestFlow && medianSpanCount > 0 && wholeTestContent!.Value.SpanCount >= medianSpanCount * 10)
                {
                    var count = wholeTestContent.Value.SpanCount;
                    spanWarning = $"<span class=\"span-count-warning\">(Warning: {count:N0} spans. This might indicate a problem/recursive loop in your test.)</span>";
                }

                if (hasSequenceDiagrams || hasWholeTestFlow)
                {
                    body.Append("<details class=\"example-diagrams\" open>");

                    if (hasWholeTestFlow && hasSequenceDiagrams)
                    {
                        body.Append("<summary class=\"h4\">Diagrams</summary>");
                        body.Append("<div class=\"diagram-toggle\">");
                        body.Append("<button class=\"diagram-toggle-btn diagram-toggle-active\" data-dtype=\"seq\">Sequence Diagrams</button>");
                        if (!string.IsNullOrEmpty(wholeTestContent!.Value.ActivityHtml))
                            body.Append("<button class=\"diagram-toggle-btn\" data-dtype=\"activity\">Activity Diagrams</button>");
                        if (!string.IsNullOrEmpty(wholeTestContent!.Value.FlameHtml))
                            body.Append("<button class=\"diagram-toggle-btn\" data-dtype=\"flame\">Flame Chart</button>");
                        body.Append(spanWarning);
                        if (isPlantUmlBrowser)
                            body.Append("<span class=\"diagram-toggle-spacer\"></span><span class=\"details-radio\"><span class=\"details-radio-label\">Details:</span><button class=\"details-radio-btn details-active\" data-state=\"expanded\" onclick=\"window._setAllNotes(this,'expanded')\">Expanded</button><button class=\"details-radio-btn\" data-state=\"collapsed\" onclick=\"window._setAllNotes(this,'collapsed')\">Collapsed</button><button class=\"details-radio-btn\" data-state=\"truncated\" onclick=\"window._setAllNotes(this,'truncated')\">Truncated</button><select class=\"truncate-lines-select\" disabled onchange=\"window._setScenarioTruncateLines(this)\"><option value=\"3\">3</option><option value=\"4\">4</option><option value=\"5\">5</option><option value=\"10\">10</option><option value=\"15\">15</option><option value=\"20\" selected>20</option><option value=\"25\">25</option><option value=\"30\">30</option><option value=\"35\">35</option><option value=\"40\">40</option></select><span class=\"truncate-lines-label\">lines</span></span><span class=\"headers-radio\"><span class=\"details-radio-label\">Headers:</span><button class=\"details-radio-btn headers-radio-btn details-active\" data-hstate=\"shown\" onclick=\"window._setScenarioHeaders(this,'shown')\">Shown</button><button class=\"details-radio-btn headers-radio-btn\" data-hstate=\"hidden\" onclick=\"window._setScenarioHeaders(this,'hidden')\">Hidden</button></span>");
                        body.Append("</div>");
                    }
                    else if (hasSequenceDiagrams)
                    {
                        body.Append("<summary class=\"h4\">Sequence Diagrams</summary>");
                        if (isPlantUmlBrowser)
                        {
                            body.Append("<div class=\"diagram-toggle\">");
                            body.Append("<span class=\"diagram-toggle-spacer\"></span><span class=\"details-radio\"><span class=\"details-radio-label\">Details:</span><button class=\"details-radio-btn details-active\" data-state=\"expanded\" onclick=\"window._setAllNotes(this,'expanded')\">Expanded</button><button class=\"details-radio-btn\" data-state=\"collapsed\" onclick=\"window._setAllNotes(this,'collapsed')\">Collapsed</button><button class=\"details-radio-btn\" data-state=\"truncated\" onclick=\"window._setAllNotes(this,'truncated')\">Truncated</button><select class=\"truncate-lines-select\" disabled onchange=\"window._setScenarioTruncateLines(this)\"><option value=\"3\">3</option><option value=\"4\">4</option><option value=\"5\">5</option><option value=\"10\">10</option><option value=\"15\">15</option><option value=\"20\" selected>20</option><option value=\"25\">25</option><option value=\"30\">30</option><option value=\"35\">35</option><option value=\"40\">40</option></select><span class=\"truncate-lines-label\">lines</span></span><span class=\"headers-radio\"><span class=\"details-radio-label\">Headers:</span><button class=\"details-radio-btn headers-radio-btn details-active\" data-hstate=\"shown\" onclick=\"window._setScenarioHeaders(this,'shown')\">Shown</button><button class=\"details-radio-btn headers-radio-btn\" data-hstate=\"hidden\" onclick=\"window._setScenarioHeaders(this,'hidden')\">Hidden</button></span>");
                            body.Append("</div>");
                        }
                    }
                    else
                    {
                        // Only whole-test-flow, no sequence diagrams
                        var hasActivity = !string.IsNullOrEmpty(wholeTestContent!.Value.ActivityHtml);
                        var hasFlame = !string.IsNullOrEmpty(wholeTestContent!.Value.FlameHtml);
                        if (hasActivity && hasFlame)
                        {
                            body.Append("<summary class=\"h4\">Diagrams</summary>");
                            body.Append("<div class=\"diagram-toggle\">");
                            body.Append("<button class=\"diagram-toggle-btn diagram-toggle-active\" data-dtype=\"activity\">Activity Diagrams</button>");
                            body.Append("<button class=\"diagram-toggle-btn\" data-dtype=\"flame\">Flame Chart</button>");
                            body.Append(spanWarning);
                            if (isPlantUmlBrowser)
                                body.Append("<span class=\"diagram-toggle-spacer\"></span><span class=\"details-radio\"><span class=\"details-radio-label\">Details:</span><button class=\"details-radio-btn details-active\" data-state=\"expanded\" onclick=\"window._setAllNotes(this,'expanded')\">Expanded</button><button class=\"details-radio-btn\" data-state=\"collapsed\" onclick=\"window._setAllNotes(this,'collapsed')\">Collapsed</button><button class=\"details-radio-btn\" data-state=\"truncated\" onclick=\"window._setAllNotes(this,'truncated')\">Truncated</button><select class=\"truncate-lines-select\" disabled onchange=\"window._setScenarioTruncateLines(this)\"><option value=\"3\">3</option><option value=\"4\">4</option><option value=\"5\">5</option><option value=\"10\">10</option><option value=\"15\">15</option><option value=\"20\" selected>20</option><option value=\"25\">25</option><option value=\"30\">30</option><option value=\"35\">35</option><option value=\"40\">40</option></select><span class=\"truncate-lines-label\">lines</span></span><span class=\"headers-radio\"><span class=\"details-radio-label\">Headers:</span><button class=\"details-radio-btn headers-radio-btn details-active\" data-hstate=\"shown\" onclick=\"window._setScenarioHeaders(this,'shown')\">Shown</button><button class=\"details-radio-btn headers-radio-btn\" data-hstate=\"hidden\" onclick=\"window._setScenarioHeaders(this,'hidden')\">Hidden</button></span>");
                            body.Append("</div>");
                        }
                        else if (hasActivity)
                        {
                            body.Append("<summary class=\"h4\">Activity Diagrams</summary>");
                        }
                        else
                        {
                            body.Append("<summary class=\"h4\">Flame Chart</summary>");
                        }
                    }

                    if (hasSequenceDiagrams)
                    {
                        var seqWrap = hasWholeTestFlow;
                        if (seqWrap) body.Append("<div class=\"diagram-view diagram-view-seq\">");

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
                                var compressed = InternalFlowHtmlGenerator.CompressToBase64(diagram.CodeBehind);
                                body.Append($"""
                                         <div class="plantuml-browser" id="{diagramId}" data-plantuml-z="{compressed}" data-diagram-type="plantuml">Loading diagram...</div>
                                         """);
                            }
                            else if (isInlineSvg)
                            {
                                var sourceCompressed = InternalFlowHtmlGenerator.CompressToBase64(diagram.CodeBehind);
                                body.Append($"""
                                         <div class="plantuml-inline-svg" data-plantuml-z="{sourceCompressed}" data-diagram-type="plantuml">{diagram.ImgSrc}</div>
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

                        if (seqWrap) body.Append("</div>");
                    }

                    if (hasWholeTestFlow)
                    {
                        var wtf = wholeTestContent!.Value;
                        var hideActivity = hasSequenceDiagrams; // hidden when seq is default
                        var hideFlame = hasSequenceDiagrams || (!string.IsNullOrEmpty(wtf.ActivityHtml) && !hasSequenceDiagrams);

                        if (!string.IsNullOrEmpty(wtf.ActivityHtml))
                            body.Append($"<div class=\"diagram-view diagram-view-activity\"{(hideActivity ? " style=\"display:none\"" : "")}>{wtf.ActivityHtml}</div>");
                        if (!string.IsNullOrEmpty(wtf.FlameHtml))
                            body.Append($"<div class=\"diagram-view diagram-view-flame\"{(hideFlame ? " style=\"display:none\"" : "")}>{wtf.FlameHtml}</div>");
                    }

                    body.Append("</details>");
                }

                body.Append("</details>");
            }
            body.Append("</details>");
        }
        body.Append("</div>");

        // Jump-to-failure button (only when there are failures)
        if (hasFailures)
        {
            body.Append($"""<button class="jump-to-failure" onclick="jump_to_next_failure()">Next Failure <span class="failure-counter" id="failure-counter">(0/{failureCount})</span></button>""");
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

            if (feature.Description is not null)
                yml.Append("    Description: " + feature.Description.SanitiseForYml() + "\n");

            if (feature.Labels is { Length: > 0 })
            {
                yml.Append("    Labels:\n");
                foreach (var label in feature.Labels)
                    yml.Append("      - " + label.SanitiseForYml() + "\n");
            }

            yml.Append("    Scenarios:\n");

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName);
            foreach (var scenario in orderedScenarios)
            {
                yml.Append("      - Scenario: " + scenario.DisplayName.SanitiseForYml() + "\n");
                yml.Append("        IsHappyPath: " + scenario.IsHappyPath.ToString().ToLower() + "\n");

                if (scenario.Labels is { Length: > 0 })
                {
                    yml.Append("        Labels:\n");
                    foreach (var label in scenario.Labels)
                        yml.Append("          - " + label.SanitiseForYml() + "\n");
                }

                if (scenario.Categories is { Length: > 0 })
                {
                    yml.Append("        Categories:\n");
                    foreach (var cat in scenario.Categories)
                        yml.Append("          - " + cat.SanitiseForYml() + "\n");
                }

                if (scenario.Steps is { Length: > 0 })
                {
                    yml.Append("        Steps:\n");
                    foreach (var step in scenario.Steps)
                        AppendYamlStep(yml, step, "          ");
                }

                yml.Append("\n");
            }
        }

        return WriteFile(yml.ToString(), fileName);
    }

    private static void AppendYamlStep(StringBuilder yml, ScenarioStep step, string indent)
    {
        var text = step.Keyword is not null ? $"{step.Keyword} {step.Text}" : step.Text;
        yml.Append(indent + "- " + text.SanitiseForYml() + "\n");

        if (step.SubSteps is { Length: > 0 })
        {
            foreach (var sub in step.SubSteps)
                AppendYamlStep(yml, sub, indent + "  ");
        }
    }

    private static int CountStepsRecursive(ScenarioStep[] steps)
    {
        var count = steps.Length;
        foreach (var step in steps)
        {
            if (step.SubSteps is { Length: > 0 })
                count += CountStepsRecursive(step.SubSteps);
        }
        return count;
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

    private static void RenderStep(StringBuilder body, ScenarioStep step)
    {
        var statusClass = step.Status switch
        {
            ScenarioResult.Passed => "passed",
            ScenarioResult.Failed => "failed",
            ScenarioResult.Skipped => "skipped",
            ScenarioResult.Bypassed => "bypassed",
            ScenarioResult.Ignored => "ignored",
            _ => ""
        };

        var statusIcon = step.Status switch
        {
            ScenarioResult.Passed => "&#10003;",
            ScenarioResult.Failed => "&#10005;",
            ScenarioResult.Skipped => "?",
            ScenarioResult.Bypassed => "~",
            ScenarioResult.Ignored => "!",
            _ => ""
        };

        var hasSubSteps = step.SubSteps is { Length: > 0 };

        if (hasSubSteps)
        {
            body.Append("<details class=\"step step-collapsible\">");
            body.Append("<summary>");
        }
        else
        {
            body.Append("<div class=\"step\">");
        }

        if (step.Status.HasValue)
        {
            body.Append($"<span class=\"step-status {statusClass}\">{statusIcon}</span>");
        }

        if (step.Keyword is not null)
        {
            body.Append($"<span class=\"step-keyword\">{System.Net.WebUtility.HtmlEncode(step.Keyword)}</span> ");
        }

        body.Append($"<span class=\"step-text\">{System.Net.WebUtility.HtmlEncode(step.Text)}</span>");

        if (step.Duration.HasValue)
        {
            body.Append($" <span class=\"step-duration\">({FormatDurationBadge(step.Duration.Value)})</span>");
        }

        if (hasSubSteps)
        {
            body.Append("</summary>");
        }

        if (step.Comments is { Length: > 0 })
        {
            foreach (var comment in step.Comments)
            {
                body.Append($"<div class=\"step-comment\">{System.Net.WebUtility.HtmlEncode(comment)}</div>");
            }
        }

        if (step.Attachments is { Length: > 0 })
        {
            foreach (var attachment in step.Attachments)
            {
                body.Append($"<a class=\"step-attachment\" href=\"{System.Net.WebUtility.HtmlEncode(attachment.RelativePath)}\">{System.Net.WebUtility.HtmlEncode(attachment.Name)}</a>");
            }
        }

        if (step.Parameters is { Length: > 0 })
        {
            foreach (var param in step.Parameters)
            {
                RenderParameter(body, param);
            }
        }

        if (hasSubSteps)
        {
            body.Append("<div class=\"sub-steps\">");
            foreach (var subStep in step.SubSteps!)
            {
                RenderStep(body, subStep);
            }
            body.Append("</div>");
            body.Append("</details>");
        }
        else
        {
            body.Append("</div>");
        }
    }

    private static void RenderParameter(StringBuilder body, StepParameter param)
    {
        switch (param.Kind)
        {
            case StepParameterKind.Inline when param.InlineValue is not null:
                var statusClass = param.InlineValue.Status switch
                {
                    VerificationStatus.Success => "param-success",
                    VerificationStatus.Failure => "param-failure",
                    VerificationStatus.Exception => "param-exception",
                    VerificationStatus.NotProvided => "param-not-provided",
                    _ => "param-na"
                };
                var display = param.InlineValue.Expectation is not null
                    ? $"{System.Net.WebUtility.HtmlEncode(param.InlineValue.Value)}/{System.Net.WebUtility.HtmlEncode(param.InlineValue.Expectation)}"
                    : System.Net.WebUtility.HtmlEncode(param.InlineValue.Value);
                body.Append($"<span class=\"step-param-inline {statusClass}\" title=\"{System.Net.WebUtility.HtmlEncode(param.Name)}\">{display}</span>");
                break;

            case StepParameterKind.Tabular when param.TabularValue is not null:
                body.Append($"<div class=\"step-param-table\"><table><thead><tr><th></th>");
                foreach (var col in param.TabularValue.Columns)
                {
                    body.Append($"<th{(col.IsKey ? " class=\"key\"" : "")}>{System.Net.WebUtility.HtmlEncode(col.Name)}</th>");
                }
                body.Append("</tr></thead><tbody>");
                foreach (var row in param.TabularValue.Rows)
                {
                    var rowIndicator = row.Type switch
                    {
                        TableRowType.Matching => "=",
                        TableRowType.Surplus => "+",
                        TableRowType.Missing => "-",
                        _ => ""
                    };
                    body.Append($"<tr class=\"row-{row.Type.ToString().ToLowerInvariant()}\"><td>{rowIndicator}</td>");
                    foreach (var cell in row.Values)
                    {
                        var cellClass = cell.Status switch
                        {
                            VerificationStatus.Success => "param-success",
                            VerificationStatus.Failure => "param-failure",
                            VerificationStatus.Exception => "param-exception",
                            VerificationStatus.NotProvided => "param-not-provided",
                            _ => ""
                        };
                        var cellDisplay = cell.Expectation is not null && cell.Status == VerificationStatus.Failure
                            ? $"{System.Net.WebUtility.HtmlEncode(cell.Value)}/{System.Net.WebUtility.HtmlEncode(cell.Expectation)}"
                            : System.Net.WebUtility.HtmlEncode(cell.Value);
                        body.Append($"<td class=\"{cellClass}\">{cellDisplay}</td>");
                    }
                    body.Append("</tr>");
                }
                body.Append("</tbody></table></div>");
                break;

            case StepParameterKind.Tree when param.TreeValue is not null:
                body.Append("<div class=\"step-param-tree\">");
                RenderTreeNode(body, param.TreeValue.Root);
                body.Append("</div>");
                break;
        }
    }

    private static void RenderTreeNode(StringBuilder body, TreeNode node)
    {
        var statusClass = node.Status switch
        {
            VerificationStatus.Success => "param-success",
            VerificationStatus.Failure => "param-failure",
            VerificationStatus.Exception => "param-exception",
            VerificationStatus.NotProvided => "param-not-provided",
            _ => ""
        };
        var valueDisplay = node.Expectation is not null && node.Status == VerificationStatus.Failure
            ? $"{System.Net.WebUtility.HtmlEncode(node.Value)}/{System.Net.WebUtility.HtmlEncode(node.Expectation)}"
            : System.Net.WebUtility.HtmlEncode(node.Value);
        body.Append($"<div class=\"tree-node {statusClass}\"><span class=\"tree-node-name\">{System.Net.WebUtility.HtmlEncode(node.Node)}</span>: {valueDisplay}");

        if (node.Children is { Length: > 0 })
        {
            body.Append("<div class=\"tree-children\">");
            foreach (var child in node.Children)
                RenderTreeNode(body, child);
            body.Append("</div>");
        }

        body.Append("</div>");
    }

    internal static string FormatDurationBadge(TimeSpan duration)
    {
        var total = duration.Duration();
        if (total.TotalSeconds < 1)
            return $"{(int)total.TotalMilliseconds}ms";
        if (total.TotalMinutes < 1)
            return $"{total.TotalSeconds:F1}s";
        return $"{(int)total.TotalMinutes}m {total.Seconds}s";
    }

    internal static string GenerateScenarioAnchorId(string displayName)
    {
        // Convert to lowercase, replace non-alphanumeric with hyphens, collapse multiple hyphens
        var slug = System.Text.RegularExpressions.Regex.Replace(displayName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return $"scenario-{slug}";
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