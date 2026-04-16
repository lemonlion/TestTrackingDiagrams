using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
                options.InternalFlowNoDataBehavior,
                options.InternalFlowSpanGranularity,
                options.InternalFlowActivitySources);

            if (options.WholeTestFlowVisualization != WholeTestFlowVisualization.None)
            {
                wholeTestSegments = InternalFlowSegmentBuilder.BuildWholeTestSegments(trackedLogs, spans);
            }
        }

        var ciMetadata = CiMetadataDetector.Detect();

        var specsDataExtension = GetDataFormatExtension(options.SpecificationsDataFormat);
        var testRunDataExtension = GetDataFormatExtension(options.TestRunReportDataFormat);

        var actions = new List<Action>
        {
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, options.HtmlSpecificationsCustomStyleSheet, $"{options.HtmlSpecificationsFileName}.html", options.SpecificationsTitle, false, generateBlankOnFailedTests: true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript, wholeTestSegments: wholeTestSegments, trackedLogs: trackedLogs, wholeTestVisualization: options.WholeTestFlowVisualization, showStepNumbers: options.SpecificationsShowStepNumbers, customCss: options.CustomCss, customFaviconBase64: options.CustomFaviconBase64, customLogoHtml: options.CustomLogoHtml),
            () => GenerateHtmlReport(diagrams, features, startRunTime, endRunTime, null, $"{options.HtmlTestRunReportFileName}.html", GetFeaturesReportTitle(options), true, lazyLoadImages: options.LazyLoadDiagramImages, diagramFormat: options.DiagramFormat, plantUmlRendering: options.PlantUmlRendering, inlineSvgRendering: options.InlineSvgRendering, internalFlowTracking: options.InternalFlowTracking, internalFlowDataScript: internalFlowDataScript, wholeTestSegments: wholeTestSegments, trackedLogs: trackedLogs, wholeTestVisualization: options.WholeTestFlowVisualization, ciMetadata: ciMetadata, showStepNumbers: options.FeaturesReportShowStepNumbers, customCss: options.CustomCss, customFaviconBase64: options.CustomFaviconBase64, customLogoHtml: options.CustomLogoHtml),
            () => GenerateSpecificationsData(features, $"{options.YamlSpecificationsFileName}.{specsDataExtension}", options.SpecificationsTitle, options.SpecificationsDataFormat, true),
            () => GenerateTestRunReportData(features, startRunTime, endRunTime, $"{options.HtmlTestRunReportFileName}.{testRunDataExtension}", options.TestRunReportDataFormat)
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

        var diagnostics = ReportDiagnostics.Analyse(
            RequestResponseLogger.RequestAndResponseLogs, features,
            includeSourceDiscovery: options.ActivitySourceDiscovery);
        foreach (var message in diagnostics)
            Console.WriteLine(message);

        if (options.DiagnosticMode)
            DiagnosticReportGenerator.Generate(RequestResponseLogger.RequestAndResponseLogs, features, options);

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
        }

        if (options.PublishCiArtifacts)
        {
            var ciEnv = CiEnvironmentDetector.Detect();
            var reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            if (Directory.Exists(reportsDirectory))
            {
                var reportFiles = Directory.GetFiles(reportsDirectory)
                    .Where(f => f.EndsWith(".html") || f.EndsWith(".yml") || f.EndsWith(".md") || f.EndsWith(".json") || f.EndsWith(".xml"))
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
        WholeTestFlowVisualization wholeTestVisualization = WholeTestFlowVisualization.None,
        CiMetadata? ciMetadata = null,
        bool showStepNumbers = false,
        string? customCss = null,
        string? customFaviconBase64 = null,
        string? customLogoHtml = null)
    {
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ExecutionResult.Failed)))
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
                                           update_url_hash();
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
                             
                                 // Extract @tag expressions
                                 let tagExpr = null;
                                 let textInput = input;
                                 if (input.indexOf('@') !== -1) {
                                     let tagParts = [];
                                     let textParts = [];
                                     let tokens = input.split(/\s+/);
                                     let inTag = false;
                                     for (let t = 0; t < tokens.length; t++) {
                                         let tok = tokens[t];
                                         if (tok.startsWith('@') || tok === 'and' || tok === 'or' || tok === 'not' || tok === '(' || tok === ')') {
                                             tagParts.push(tok);
                                             inTag = true;
                                         } else if (inTag && (tok === 'and' || tok === 'or' || tok === 'not')) {
                                             tagParts.push(tok);
                                         } else {
                                             textParts.push(tok);
                                             inTag = false;
                                         }
                                     }
                                     if (tagParts.length > 0) {
                                         tagExpr = tagParts.join(' ');
                                         textInput = textParts.join(' ');
                                     }
                                 }

                                 let searchTokens = parseSearchTokensIncludingQuotes(textInput);
                             
                                 if (searchTokens.length === 0 && !tagExpr) {
                                     for (let i = 0; i < c.items.length; i++) {
                                         c.items[i].sr = false;
                                         c.items[i].el.removeAttribute('open');
                                     }
                                     applyVisibility(c);
                                     update_url_hash();
                                     return;
                                 }
                             
                                 // Match at the scenario level
                                 let matchCount = 0;
                                 let singleMatch = null;
                                 for (let i = 0; i < c.items.length; i++) {
                                     let textMatch = true;
                                     if (searchTokens.length > 0) {
                                         let text = c.items[i].searchText;
                                         for (let j = 0; j < searchTokens.length; j++) {
                                             if (!text.includes(searchTokens[j])) {
                                                 textMatch = false;
                                                 break;
                                             }
                                         }
                                     }
                                     let tagMatch = true;
                                     if (tagExpr) {
                                         let cats = (c.items[i].el.getAttribute('data-categories') || '').toLowerCase();
                                         let labels = (c.items[i].el.getAttribute('data-labels') || '').toLowerCase();
                                         let allTags = new Set();
                                         if (cats) cats.split(',').forEach(function(t) { allTags.add(t.trim()); });
                                         if (labels) labels.split(',').forEach(function(t) { allTags.add(t.trim()); });
                                         tagMatch = evaluateTagExpression(tagExpr, allTags);
                                     }
                                     c.items[i].sr = !(textMatch && tagMatch);
                                     if (textMatch && tagMatch) {
                                         matchCount++;
                                         singleMatch = c.items[i].el;
                                     }
                                 }
                             
                                 applyVisibility(c);
                                 update_url_hash();
                             
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

                             function evaluateTagExpression(expr, tags) {
                                 // Tokenize
                                 let tokens = [];
                                 let parts = expr.split(/\s+/);
                                 for (let i = 0; i < parts.length; i++) {
                                     let p = parts[i];
                                     if (p === 'and' || p === 'or' || p === 'not') tokens.push({type: p});
                                     else if (p === '(') tokens.push({type: 'lparen'});
                                     else if (p === ')') tokens.push({type: 'rparen'});
                                     else if (p.startsWith('@')) tokens.push({type: 'tag', value: p.substring(1).toLowerCase()});
                                     else tokens.push({type: 'tag', value: p.toLowerCase()});
                                 }
                                 let pos = {i: 0};
                                 function parseOr() {
                                     let left = parseAnd();
                                     while (pos.i < tokens.length && tokens[pos.i].type === 'or') {
                                         pos.i++;
                                         left = left || parseAnd();
                                     }
                                     return left;
                                 }
                                 function parseAnd() {
                                     let left = parseNot();
                                     while (pos.i < tokens.length && tokens[pos.i].type === 'and') {
                                         pos.i++;
                                         left = left && parseNot();
                                     }
                                     return left;
                                 }
                                 function parseNot() {
                                     if (pos.i < tokens.length && tokens[pos.i].type === 'not') {
                                         pos.i++;
                                         return !parsePrimary();
                                     }
                                     return parsePrimary();
                                 }
                                 function parsePrimary() {
                                     if (pos.i < tokens.length && tokens[pos.i].type === 'lparen') {
                                         pos.i++;
                                         let result = parseOr();
                                         if (pos.i < tokens.length && tokens[pos.i].type === 'rparen') pos.i++;
                                         return result;
                                     }
                                     if (pos.i < tokens.length && tokens[pos.i].type === 'tag') {
                                         let v = tokens[pos.i].value;
                                         pos.i++;
                                         return tags.has(v);
                                     }
                                     return false;
                                 }
                                 return parseOr();
                             }
                             """;

        var dependencyFilterFunction = """
                                       var _depMode = 'AND';
                                       function toggle_dep_mode(btn) {
                                           _depMode = _depMode === 'AND' ? 'OR' : 'AND';
                                           btn.textContent = _depMode;
                                           filter_dependencies();
                                       }
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
                                               update_url_hash();
                                               return;
                                           }
                                       
                                           var activeArr = Array.from(activeSet);
                                           for (var i = 0; i < c.items.length; i++) {
                                               var d = c.items[i];
                                               var match;
                                               if (d.deps.size === 0) {
                                                   match = false;
                                               } else if (_depMode === 'AND') {
                                                   match = true;
                                                   for (var j = 0; j < activeArr.length; j++) {
                                                       if (!d.deps.has(activeArr[j])) { match = false; break; }
                                                   }
                                               } else {
                                                   match = false;
                                                   for (var j = 0; j < activeArr.length; j++) {
                                                       if (d.deps.has(activeArr[j])) { match = true; break; }
                                                   }
                                               }
                                               d.dep = !match;
                                           }
                                           applyVisibility(c);
                                           update_url_hash();
                                       }
                                       """;

        var categoryFilterFunction = """
                                     var _catMode = 'OR';
                                     function toggle_cat_mode(btn) {
                                         _catMode = _catMode === 'OR' ? 'AND' : 'OR';
                                         btn.textContent = _catMode;
                                         filter_categories();
                                     }
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
                                             update_url_hash();
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
                                             } else if (_catMode === 'AND') {
                                                 var allMatch = true;
                                                 activeSet.forEach(function(a) { if (a !== '__uncategorized__' && !cats.has(a)) allMatch = false; });
                                                 c.items[i].cat = !allMatch;
                                             } else {
                                                 var match = false;
                                                 activeSet.forEach(function(a) { if (a !== '__uncategorized__' && cats.has(a)) match = true; });
                                                 c.items[i].cat = !match;
                                             }
                                         }
                                         applyVisibility(c);
                                         update_url_hash();
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
                                           update_url_hash();
                                           return;
                                       }
                                   
                                       for (var i = 0; i < c.items.length; i++) {
                                           var s = c.items[i].status;
                                           if (s === 'SkippedAfterFailure') s = 'Failed';
                                           c.items[i].st = !activeSet.has(s);
                                       }
                                       applyVisibility(c);
                                       update_url_hash();
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

        // Toggle examples detail row
        var toggleExamplesDetailFunction = """
                                           function toggle_examples_detail(row) {
                                               var detail = row.nextElementSibling;
                                               if (detail && detail.classList.contains('examples-detail-row')) {
                                                   detail.style.display = detail.style.display === 'none' ? '' : 'none';
                                                   row.classList.toggle('examples-row-expanded');
                                               }
                                           }
                                           """;

        // Toggle timeline
        var toggleTimelineFunction = """
                                     function toggle_timeline(btn) {
                                         var tl = document.getElementById('scenario-timeline');
                                         if (!tl) return;
                                         var hidden = tl.style.display === 'none';
                                         tl.style.display = hidden ? '' : 'none';
                                         btn.classList.toggle('timeline-toggle-active', hidden);
                                     }
                                     """;

        // Jump to failure
        var hasFailures = features.SelectMany(f => f.Scenarios).Any(s => s.Result == ExecutionResult.Failed);
        var failureCount = features.SelectMany(f => f.Scenarios).Count(s => s.Result == ExecutionResult.Failed);
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
                                             if (!c.items[i].el.hasAttribute('data-duration-ms')) { c.items[i].dur = true; continue; }
                                             var ms = parseFloat(c.items[i].el.getAttribute('data-duration-ms'));
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
                                             btn.classList.add('percentile-active');
                                             var ms = parseFloat(btn.getAttribute('data-threshold-ms'));
                                             if (input) { input.value = (ms / 1000).toFixed(1); filter_duration(); }
                                         }
                                     }
                                     """;

        // Export filtered view
        var exportFunction = """
                             function clear_all_filters() {
                                 var c = fc();
                                 // Clear search
                                 var sb = document.getElementById('searchbar');
                                 if (sb) { sb.value = ''; }
                                 for (var i = 0; i < c.items.length; i++) c.items[i].sr = false;
                                 // Clear status
                                 document.querySelectorAll('.status-toggle.status-active').forEach(function(b) { b.classList.remove('status-active'); });
                                 for (var i = 0; i < c.items.length; i++) c.items[i].st = false;
                                 // Clear happy paths
                                 var hp = document.querySelector('.happy-path-toggle.happy-path-active');
                                 if (hp) hp.classList.remove('happy-path-active');
                                 for (var i = 0; i < c.items.length; i++) c.items[i].hp = false;
                                 // Clear duration
                                 document.querySelectorAll('.percentile-btn.percentile-active').forEach(function(b) { b.classList.remove('percentile-active'); });
                                 var dur = document.getElementById('duration-threshold');
                                 if (dur) dur.value = '';
                                 var cw = document.getElementById('custom-duration-wrap');
                                 if (cw) cw.style.display = 'none';
                                 for (var i = 0; i < c.items.length; i++) c.items[i].dur = false;
                                 // Clear dependencies
                                 document.querySelectorAll('.dependency-toggle.dependency-active').forEach(function(b) { b.classList.remove('dependency-active'); });
                                 _depMode = 'AND';
                                 var depModeBtn = document.querySelector('.dep-mode-toggle');
                                 if (depModeBtn) depModeBtn.textContent = 'AND';
                                 for (var i = 0; i < c.items.length; i++) c.items[i].dep = false;
                                 // Clear categories
                                 document.querySelectorAll('.category-toggle.category-active').forEach(function(b) { b.classList.remove('category-active'); });
                                 var allCatBtn = document.querySelector('.category-toggle[data-category=""]');
                                 if (allCatBtn) allCatBtn.classList.add('category-active');
                                 if (typeof _catMode !== 'undefined') _catMode = 'OR';
                                 var catModeBtn = document.querySelector('.cat-mode-toggle');
                                 if (catModeBtn) catModeBtn.textContent = 'OR';
                                 for (var i = 0; i < c.items.length; i++) c.items[i].cat = false;
                                 // Apply and clear URL
                                 applyVisibility(c);
                                 history.replaceState(null, '', window.location.pathname + window.location.search);
                             }
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
                                  if (_depMode !== 'AND') parts.push('depmode=' + _depMode);
                                  if (typeof _catMode !== 'undefined' && _catMode !== 'OR') parts.push('catmode=' + _catMode);
                                  if (document.querySelector('.happy-path-toggle.happy-path-active')) parts.push('hp=1');
                                  var cats = [];
                                  if (!document.querySelector('.category-toggle.category-active[data-category=""]')) {
                                      document.querySelectorAll('.category-toggle.category-active').forEach(function(b) { cats.push(b.getAttribute('data-category')); });
                                  }
                                  if (cats.length > 0) parts.push('cats=' + encodeURIComponent(cats.join(',')));
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
                                  if (params.depmode === 'OR') {
                                      _depMode = 'OR';
                                      var modeBtn = document.querySelector('.dep-mode-toggle');
                                      if (modeBtn) modeBtn.textContent = 'OR';
                                  }
                                  if (params.catmode === 'AND') {
                                      _catMode = 'AND';
                                      var catModeBtn = document.querySelector('.cat-mode-toggle');
                                      if (catModeBtn) catModeBtn.textContent = 'AND';
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
                                  if (params.cats) {
                                      var allBtn = document.querySelector('.category-toggle[data-category=""]');
                                      if (allBtn) allBtn.classList.remove('category-active');
                                      params.cats.split(',').forEach(function(c) {
                                          var btn = document.querySelector('.category-toggle[data-category="' + c + '"]');
                                          if (btn) btn.classList.add('category-active');
                                      });
                                      filter_categories();
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

        var isPlantUmlBrowser = plantUmlRendering == PlantUmlRendering.BrowserJs;
        var isInlineSvg = !isPlantUmlBrowser && inlineSvgRendering;
        var hasInteractiveDiagrams = isPlantUmlBrowser || isInlineSvg;
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

        var customCssBlock = customCss is not null ? $"<style>{customCss}</style>" : "";
        var faviconLink = customFaviconBase64 is not null ? $"<link rel=\"icon\" href=\"{customFaviconBase64}\">" : "";

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
                            {{customCssBlock}}
                            {{faviconLink}}
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
                                {{toggleExamplesDetailFunction}}
                                {{toggleTimelineFunction}}
                                {{jumpToFailureFunction}}
                                {{durationFilterFunction}}
                                {{exportFunction}}
                                {{persistentFilterFunction}}
                                {{urlHashFunction}}
                                {{keyboardNavigationFunction}}
                                {{initScript}}
                            </script>
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
        if (customLogoHtml is not null)
            body.Append($"<div class=\"custom-logo\">{customLogoHtml}</div>");
        body.Append($"<h1>{title}</h1>");

        if (includeTestRunData)
        {
            var numberOfFeatures = features.Length;
            var scenarios = features.SelectMany(x => x.Scenarios).ToArray();
            var passedScenarios = scenarios.Where(x => x.Result == ExecutionResult.Passed).ToArray();
            var skippedScenarios = scenarios.Where(x => x.Result == ExecutionResult.Skipped).ToArray();
            var failedScenarios = scenarios.Where(x => x.Result == ExecutionResult.Failed).ToArray();
            var overallStatus = failedScenarios.Any() ? "Failed" : "Passed";

            // Feature summary table (collapsible, above execution summary)
            var hasAnySteps = features.Any(f => f.Scenarios.Any(s => s.Steps is { Length: > 0 }));
            var hasAnyDurations = features.Any(f => f.Scenarios.Any(s => s.Duration.HasValue));
            var nextCol = 5;
            body.Append("<details class=\"features-summary-details\"><summary class=\"h2\">Features Summary</summary>");
            body.Append("<div class=\"features-summary-table-wrapper\">");
            body.Append("<table class=\"feature-summary-table\"><thead><tr>");
            body.Append("<th onclick=\"sort_table(0)\">Feature</th>");
            body.Append("<th onclick=\"sort_table(1)\">Scenarios</th>");
            body.Append("<th onclick=\"sort_table(2)\">Passed</th>");
            body.Append("<th onclick=\"sort_table(3)\">Failed</th>");
            body.Append("<th onclick=\"sort_table(4)\">Skipped</th>");
            if (hasAnySteps)
            {
                body.Append($"<th onclick=\"sort_table({nextCol++})\">Steps</th>");
                body.Append($"<th class=\"step-status-header\" onclick=\"sort_table({nextCol++})\">Passed</th>");
                body.Append($"<th class=\"step-status-header\" onclick=\"sort_table({nextCol++})\">Failed</th>");
                body.Append($"<th class=\"step-status-header\" onclick=\"sort_table({nextCol++})\">Skipped</th>");
            }
            if (hasAnyDurations)
            {
                body.Append($"<th onclick=\"sort_table({nextCol++})\">Duration</th>");
                body.Append($"<th onclick=\"sort_table({nextCol++})\">Avg</th>");
                body.Append($"<th onclick=\"sort_table({nextCol})\">Longest</th>");
            }
            body.Append("</tr></thead><tbody>");

            foreach (var feature in features)
            {
                var totalSc = feature.Scenarios.Length;
                var passedSc = feature.Scenarios.Count(s => s.Result == ExecutionResult.Passed);
                var failedSc = feature.Scenarios.Count(s => s.Result == ExecutionResult.Failed);
                var skippedSc = feature.Scenarios.Count(s => s.Result is ExecutionResult.Skipped or ExecutionResult.Bypassed or ExecutionResult.SkippedAfterFailure);
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
                    var stepStatusCounts = CountStepsByStatusRecursive(allSteps);
                    body.Append($"<td>{stepCount}</td>");
                    body.Append($"<td>{stepStatusCounts.Passed}</td>");
                    body.Append($"<td>{stepStatusCounts.Failed}</td>");
                    body.Append($"<td>{stepStatusCounts.Skipped}</td>");
                }

                if (hasAnyDurations)
                {
                    var durations = feature.Scenarios.Where(s => s.Duration.HasValue).Select(s => s.Duration!.Value).ToArray();
                    var totalDuration = durations.Length > 0 ? durations.Aggregate(TimeSpan.Zero, (a, b) => a + b) : TimeSpan.Zero;
                    var avgDuration = durations.Length > 0 ? totalDuration / durations.Length : TimeSpan.Zero;
                    var maxDuration = durations.Length > 0 ? durations.Max() : TimeSpan.Zero;
                    body.Append($"<td>{FormatDuration(totalDuration)}</td>");
                    body.Append($"<td>{FormatDuration(avgDuration)}</td>");
                    body.Append($"<td>{FormatDuration(maxDuration)}</td>");
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

            if (ciMetadata is not null)
            {
                body.Append("<div class=\"ci-metadata\"><table>");
                body.Append($"<tr><td colspan=\"2\" class=\"column-header\">CI ({ciMetadata.Provider})</td></tr>");
                if (ciMetadata.BuildNumber is not null)
                    body.Append($"<tr><td>Build #:</td><td>{System.Net.WebUtility.HtmlEncode(ciMetadata.BuildNumber)}</td></tr>");
                if (ciMetadata.Branch is not null)
                    body.Append($"<tr><td>Branch:</td><td>{System.Net.WebUtility.HtmlEncode(ciMetadata.Branch)}</td></tr>");
                if (ciMetadata.CommitSha is not null)
                {
                    var shortSha = ciMetadata.CommitSha.Length > 7 ? ciMetadata.CommitSha[..7] : ciMetadata.CommitSha;
                    body.Append($"<tr><td>Commit:</td><td><code title=\"{System.Net.WebUtility.HtmlEncode(ciMetadata.CommitSha)}\">{System.Net.WebUtility.HtmlEncode(shortSha)}</code></td></tr>");
                }
                if (ciMetadata.PipelineUrl is not null)
                    body.Append($"<tr><td>Pipeline:</td><td><a href=\"{System.Net.WebUtility.HtmlEncode(ciMetadata.PipelineUrl)}\" target=\"_blank\" rel=\"noopener noreferrer\">View Run</a></td></tr>");
                if (ciMetadata.Repository is not null)
                    body.Append($"<tr><td>Repository:</td><td>{System.Net.WebUtility.HtmlEncode(ciMetadata.Repository)}</td></tr>");
                body.Append("</table></div>");
            }

            var bypassedScenarios = scenarios.Where(x => x.Result == ExecutionResult.Bypassed).ToArray();
            body.Append(GeneratePieChartSvg(passedScenarios.Length, failedScenarios.Length, skippedScenarios.Length, bypassedScenarios.Length));
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
                    <div class="filtering-box-header"><h2>Filtering</h2><div class="filtering-box-export"><button class="export-btn" onclick="clear_all_filters()">Clear All</button><button class="export-btn" onclick="export_html()">Export Filtered HTML</button><button class="export-btn" onclick="export_csv()">Export Filtered CSV</button></div></div>
                    <div class="filters">
                    <div class="filter-search"><input id="searchbar" placeholder="Search (use @tag for tag filtering)" onkeyup="search_scenarios()" /></div>
                    <div class="filter-row">
                 """);

        // Status filter toggles
        {
            body.Append("""<div class="status-filters"><span class="status-filters-label">Status:</span>""");
            foreach (var status in Enum.GetValues<ExecutionResult>().OrderBy(s => s))
            {
                if (status == ExecutionResult.SkippedAfterFailure) continue;
                var statusName = status.ToString();
                body.Append($"""<button class="status-toggle" data-status="{statusName}" onclick="toggle_status(this)">{statusName}</button>""");
            }
            body.Append("</div>");
        }

        body.Append("""
                    <div class="happy-path-filters"><span class="happy-path-filters-label">Happy Paths:</span><button class="happy-path-toggle" onclick="toggle_happy_paths(this)">Happy Paths Only</button></div>
                 """);

        body.Append("</div>"); // close filter-row

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

        if (allDependencies.Count > 0)
        {
            body.Append("""<div class="dependency-filters"><span class="dependency-filters-label">Dependencies:</span><button class="dep-mode-toggle" title="AND: show scenarios matching ALL selected dependencies. OR: show scenarios matching ANY selected dependency. Click to toggle." onclick="toggle_dep_mode(this)">AND</button>""");
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
            body.Append("""<div class="category-filters"><span class="category-filters-label">Categories:</span><button class="cat-mode-toggle" title="OR: show scenarios matching ANY selected category. AND: show scenarios matching ALL selected categories. Click to toggle." onclick="toggle_cat_mode(this)">OR</button>""");
            body.Append("""<button class="category-toggle category-active" data-category="" onclick="toggle_category(this)">All</button>""");
            foreach (var cat in allCategories)
            {
                body.Append($"""<button class="category-toggle" data-category="{System.Net.WebUtility.HtmlEncode(cat)}" onclick="toggle_category(this)">{System.Net.WebUtility.HtmlEncode(cat)}</button>""");
            }
            body.Append("""<button class="category-toggle" data-category="__uncategorized__" onclick="toggle_category(this)">Uncategorized</button>""");
            body.Append("</div>");
        }

        body.Append("</div>"); // close filters
        body.Append("</div>"); // close filtering-box
        if (includeTestRunData)
            body.Append("</div>"); // close header-row

        // Toolbar row: expand buttons left, Details/Headers right
        body.Append("""<div class="toolbar-row">""");
        body.Append("""<div class="toolbar-left"><button class="collapse-expand-all" onclick="toggle_expand_collapse(this, 'details.feature', 'Expand All Features', 'Collapse All Features')">Expand All Features</button><button class="collapse-expand-all" onclick="toggle_expand_collapse(this, 'details.scenario', 'Expand All Scenarios', 'Collapse All Scenarios')">Expand All Scenarios</button>""");
        if (hasDurations)
            body.Append("""<button class="timeline-toggle" onclick="toggle_timeline(this)">Scenario Timeline</button>""");
        body.Append("</div>");
        body.Append("""<div class="toolbar-right">""");
        if (isPlantUmlBrowser)
        {
            body.Append("""<span class="details-radio"><span class="details-radio-label">Details:</span><button class="details-radio-btn" data-state="expanded" onclick="window._setReportDetails('expanded')">Expanded</button><button class="details-radio-btn" data-state="collapsed" onclick="window._setReportDetails('collapsed')">Collapsed</button><button class="details-radio-btn details-active" data-state="truncated" onclick="window._setReportDetails('truncated')">Truncated</button><select class="truncate-lines-select" onchange="window._setTruncateLines(this)"><option value="3">3</option><option value="4">4</option><option value="5">5</option><option value="10">10</option><option value="15">15</option><option value="20">20</option><option value="25">25</option><option value="30">30</option><option value="35">35</option><option value="40" selected>40</option><option value="50">50</option><option value="60">60</option><option value="80">80</option><option value="100">100</option></select><span class="truncate-lines-label">lines</span></span>""");
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

        // Failure clusters
        var allScenarios = features.SelectMany(f => f.Scenarios).ToArray();
        var clusters = FailureClusterer.Cluster(allScenarios);
        if (clusters.Length > 0)
        {
            // Build scenario-to-feature lookup for display
            var scenarioFeatureLookup = new Dictionary<string, string>();
            foreach (var feature in features)
            foreach (var scenario in feature.Scenarios)
                scenarioFeatureLookup[scenario.Id] = feature.DisplayName;

            body.Append("<details class=\"failure-clusters\" open>");
            body.Append($"<summary>Failure Clusters ({clusters.Length} root cause{(clusters.Length == 1 ? "" : "s")})</summary>");
            foreach (var cluster in clusters)
            {
                var anchorLinks = string.Join("", cluster.Scenarios.Select(s =>
                {
                    var anchorId = "scenario-" + s.Id.Replace(" ", "-");
                    var featureName = scenarioFeatureLookup.GetValueOrDefault(s.Id, "");
                    var prefix = featureName.Length > 0 ? $"<span style=\"color:rgb(100,100,100);font-size:0.85em\">{System.Net.WebUtility.HtmlEncode(featureName)} &rsaquo;</span> " : "";
                    return $"<li>{prefix}<a class=\"failure-cluster-scenario-link\" href=\"#{anchorId}\" onclick=\"var el=document.getElementById('{anchorId}');if(el){{var f=el.closest('details.feature');if(f)f.setAttribute('open','');el.setAttribute('open','');}}\">{System.Net.WebUtility.HtmlEncode(s.DisplayName)}</a></li>";
                }));
                body.Append($"<details class=\"failure-cluster\"><summary>{System.Net.WebUtility.HtmlEncode(cluster.ClusterKey)}<span class=\"failure-cluster-count\">{cluster.Scenarios.Length} scenarios</span></summary>");
                body.Append($"<ul class=\"failure-cluster-scenarios\">{anchorLinks}</ul></details>");
            }
            body.Append("</details>");
        }

        // Scenario timeline / Gantt (hidden by default)
        if (hasDurations)
        {
            var timelineScenarios = features
                .SelectMany(f => f.Scenarios.Select(s => (Feature: f.DisplayName, Scenario: s)))
                .Where(x => x.Scenario.Duration.HasValue)
                .OrderByDescending(x => x.Scenario.Duration!.Value)
                .ToArray();

            if (timelineScenarios.Length > 0)
            {
                var maxDuration = timelineScenarios.Max(x => x.Scenario.Duration!.Value.TotalMilliseconds);
                body.Append("<div id=\"scenario-timeline\" class=\"scenario-timeline\" style=\"display:none\">");
                body.Append("<div class=\"timeline-header\">Scenario Timeline</div>");
                foreach (var (featureName, scenario) in timelineScenarios)
                {
                    var durationMs = scenario.Duration!.Value.TotalMilliseconds;
                    var widthPercent = maxDuration > 0 ? (durationMs / maxDuration * 100) : 0;
                    var statusClass = scenario.Result switch
                    {
                        ExecutionResult.Failed => "timeline-bar-failed",
                        ExecutionResult.Skipped or ExecutionResult.SkippedAfterFailure => "timeline-bar-skipped",
                        ExecutionResult.Bypassed => "timeline-bar-bypassed",
                        _ => "timeline-bar-passed"
                    };
                    body.Append($"<div class=\"timeline-row\">");
                    body.Append($"<div class=\"timeline-label\" title=\"{System.Net.WebUtility.HtmlEncode(scenario.DisplayName)}\">{System.Net.WebUtility.HtmlEncode(scenario.DisplayName)}</div>");
                    body.Append($"<div class=\"timeline-track\"><div class=\"timeline-bar {statusClass}\" style=\"width:{widthPercent:F1}%\" title=\"{FormatDurationBadge(scenario.Duration.Value)}\"></div></div>");
                    body.Append($"<div class=\"timeline-duration\">{FormatDurationBadge(scenario.Duration.Value)}</div>");
                    body.Append("</div>");
                }
                body.Append("</div>");
            }
        }

        body.Append("<div id=\"report-content\">");
        foreach (var feature in features)
        {
            var featureHasFailures = feature.Scenarios.Any(s => s.Result == ExecutionResult.Failed);
            var featureAllSkipped = !featureHasFailures && feature.Scenarios.All(s => s.Result == ExecutionResult.Skipped);
            body.Append($"""
                     <details class="feature">
                        <summary class="h2{(featureHasFailures ? " failed" : featureAllSkipped ? " skipped" : "")}">{feature.DisplayName}{(feature.Endpoint is null ? "" : $" <div class=\"endpoint\">{feature.Endpoint}</div>")}{(feature.Labels is { Length: > 0 } fl ? string.Concat(fl.Select(l => $" <span class=\"label\">{System.Net.WebUtility.HtmlEncode(l)}</span>")) : "")}</summary>
                     """);

            if (feature.Description is not null)
            {
                body.Append($"""<div class="feature-description">{System.Net.WebUtility.HtmlEncode(feature.Description)}</div>""");
            }

            var orderedScenarios = feature.Scenarios.OrderByDescending(x => x.IsHappyPath).ThenBy(x => x.DisplayName).ToArray();

            // Pre-group scenarios by OutlineId for examples tables
            var outlineGroups = orderedScenarios
                .Where(s => s.OutlineId is not null)
                .GroupBy(s => s.OutlineId!)
                .ToDictionary(g => g.Key, g => g.ToArray());
            var renderedOutlineIds = new HashSet<string>();

            // Group by Rule for rendering
            string? currentRule = "__NOTSET__";
            var ruleOpen = false;
            foreach (var scenario in orderedScenarios)
            {
                // Skip if this scenario belongs to an outline group already rendered
                if (scenario.OutlineId is not null && renderedOutlineIds.Contains(scenario.OutlineId))
                    continue;

                if (scenario.Rule != currentRule)
                {
                    if (ruleOpen)
                    {
                        body.Append("</details>"); // close previous rule
                    }
                    currentRule = scenario.Rule;
                    if (currentRule is not null)
                    {
                        body.Append($"<details class=\"rule\" open><summary class=\"h2-5\">{System.Net.WebUtility.HtmlEncode(currentRule)}</summary>");
                        ruleOpen = true;
                    }
                    else
                    {
                        ruleOpen = false;
                    }
                }

                // Render as examples table if this is an outline group
                if (scenario.OutlineId is not null && outlineGroups.TryGetValue(scenario.OutlineId, out var outlineScenarios))
                {
                    renderedOutlineIds.Add(scenario.OutlineId);
                    var allKeys = outlineScenarios
                        .Where(s => s.ExampleValues is not null)
                        .SelectMany(s => s.ExampleValues!.Keys)
                        .Distinct()
                        .ToArray();
                    var outlineHasFailure = outlineScenarios.Any(s => s.Result == ExecutionResult.Failed);
                    var outlineHasSkipped = outlineScenarios.Any(s => s.Result == ExecutionResult.Skipped);
                    var outlineOverallStatus = outlineHasFailure ? ExecutionResult.Failed
                        : outlineHasSkipped ? ExecutionResult.Skipped
                        : outlineScenarios.Any(s => s.Result == ExecutionResult.Bypassed) ? ExecutionResult.Bypassed
                        : ExecutionResult.Passed;

                    // Build search text from all outline scenario names + error messages
                    var outlineSearchParts = new List<string> { scenario.OutlineId };
                    foreach (var os in outlineScenarios)
                    {
                        outlineSearchParts.Add(os.DisplayName);
                        if (os.ErrorMessage is not null) outlineSearchParts.Add(os.ErrorMessage);
                    }
                    var outlineSearchAttr = $" data-search=\"{System.Net.WebUtility.HtmlEncode(string.Join(" ", outlineSearchParts).ToLowerInvariant())}\"";

                    // Aggregate categories and labels from all outline scenarios
                    var outlineCategories = outlineScenarios
                        .Where(s => s.Categories is { Length: > 0 })
                        .SelectMany(s => s.Categories!)
                        .Distinct()
                        .ToArray();
                    var outlineCategoriesAttr = outlineCategories.Length > 0
                        ? $" data-categories=\"{System.Net.WebUtility.HtmlEncode(string.Join(",", outlineCategories))}\""
                        : "";
                    var outlineLabels = outlineScenarios
                        .Where(s => s.Labels is { Length: > 0 })
                        .SelectMany(s => s.Labels!)
                        .Distinct()
                        .ToArray();
                    var outlineLabelsAttr = outlineLabels.Length > 0
                        ? $" data-labels=\"{System.Net.WebUtility.HtmlEncode(string.Join(",", outlineLabels))}\""
                        : "";

                    // Total duration
                    var outlineTotalDuration = outlineScenarios
                        .Where(s => s.Duration.HasValue)
                        .Select(s => s.Duration!.Value)
                        .Aggregate(TimeSpan.Zero, (acc, d) => acc + d);
                    var outlineDurationAttr = outlineTotalDuration > TimeSpan.Zero
                        ? $" data-duration-ms=\"{outlineTotalDuration.TotalMilliseconds:F0}\""
                        : "";
                    var outlineDurationBadge = outlineTotalDuration > TimeSpan.Zero
                        ? $" <span class=\"duration-badge {(outlineTotalDuration.TotalMilliseconds < 2000 ? "duration-fast" : outlineTotalDuration.TotalMilliseconds < 5000 ? "duration-moderate" : "duration-slow")}\">{FormatDurationBadge(outlineTotalDuration)}</span>"
                        : "";

                    body.Append($"<details class=\"scenario scenario-outline\" data-status=\"{outlineOverallStatus}\"{outlineSearchAttr}{outlineDurationAttr}{outlineCategoriesAttr}{outlineLabelsAttr}>");
                    body.Append($"<summary class=\"h3{(outlineHasFailure ? " failed" : outlineHasSkipped ? " skipped" : "")}\">Scenario Outline: {System.Net.WebUtility.HtmlEncode(scenario.OutlineId)}{outlineDurationBadge}</summary>");
                    body.Append("<table class=\"examples-table\"><thead><tr><th>Status</th><th>Scenario</th>");
                    foreach (var key in allKeys)
                        body.Append($"<th>{System.Net.WebUtility.HtmlEncode(key)}</th>");
                    body.Append("<th>Duration</th></tr></thead><tbody>");
                    foreach (var os in outlineScenarios)
                    {
                        var rowStatusIcon = os.Result switch
                        {
                            ExecutionResult.Passed => "&#10003;",
                            ExecutionResult.Failed => "&#10005;",
                            ExecutionResult.Skipped => "&#216;",
                            ExecutionResult.Bypassed => "&#8631;",
                            _ => ""
                        };
                        var rowStatusClass = $"examples-row-{os.Result.ToString().ToLowerInvariant()}";
                        var hasDetail = os.Result == ExecutionResult.Failed;
                        var expandIcon = hasDetail ? " class=\"examples-row-expandable\" onclick=\"toggle_examples_detail(this)\"" : "";
                        body.Append($"<tr class=\"{rowStatusClass}\" data-status=\"{os.Result}\"{expandIcon}><td>{rowStatusIcon}</td>");
                        body.Append($"<td>{System.Net.WebUtility.HtmlEncode(os.DisplayName)}</td>");
                        foreach (var key in allKeys)
                        {
                            var val = os.ExampleValues?.GetValueOrDefault(key, "");
                            body.Append($"<td>{System.Net.WebUtility.HtmlEncode(val ?? "")}</td>");
                        }
                        var durationText = os.Duration.HasValue ? FormatDurationBadge(os.Duration.Value) : "";
                        body.Append($"<td>{durationText}</td>");
                        body.Append("</tr>");

                        // Expandable detail row for failed scenarios
                        if (hasDetail)
                        {
                            var colSpan = 3 + allKeys.Length; // Status + Scenario + keys + Duration
                            body.Append($"<tr class=\"examples-detail-row\" style=\"display:none\"><td colspan=\"{colSpan}\">");
                            if (os.ErrorMessage is not null || os.ErrorStackTrace is not null)
                            {
                                var osDiffHtml = "";
                                var osDiffResult = ErrorDiffParser.TryParseExpectedActual(os.ErrorMessage);
                                if (osDiffResult is not null)
                                    osDiffHtml = ErrorDiffParser.GenerateDiffHtml(osDiffResult.Expected, osDiffResult.Actual);
                                body.Append("<details class=\"failure-result\" open><summary class=\"h4\">Failure Result</summary><pre>");
                                if (os.ErrorMessage is not null)
                                    body.Append($"Failure Cause: {os.ErrorMessage}\n\n");
                                if (os.ErrorStackTrace is not null)
                                    body.Append(os.ErrorStackTrace);
                                body.Append($"</pre>{osDiffHtml}</details>");
                            }
                            if (os.Steps is { Length: > 0 })
                            {
                                body.Append("<div class=\"scenario-steps\">");
                                for (var si = 0; si < os.Steps.Length; si++)
                                {
                                    var numberPrefix = showStepNumbers ? $"{si + 1}." : null;
                                    RenderStep(body, os.Steps[si], numberPrefix);
                                }
                                body.Append("</div>");
                            }
                            body.Append("</td></tr>");
                        }
                    }
                    body.Append("</tbody></table></details>");
                    continue;
                }

                var failed = scenario.Result == ExecutionResult.Failed;
                var skipped = scenario.Result == ExecutionResult.Skipped;
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

                var labelsAttr = scenario.Labels is { Length: > 0 }
                    ? $" data-labels=\"{System.Net.WebUtility.HtmlEncode(string.Join(",", scenario.Labels))}\""
                    : "";

                var encodedName = System.Net.WebUtility.HtmlEncode(scenario.DisplayName);
                var scenarioLabelsHtml = scenario.Labels is { Length: > 0 }
                    ? string.Concat(scenario.Labels
                        .Where(l => !scenario.IsHappyPath || !l.Equals("Happy Path", StringComparison.OrdinalIgnoreCase))
                        .Select(l => $" <span class=\"label\">{System.Net.WebUtility.HtmlEncode(l)}</span>"))
                    : "";

                var scenarioTooltip = scenario.Result switch
                {
                    ExecutionResult.Passed => "Passed — all assertions passed",
                    ExecutionResult.Failed => "Failed — an assertion or runtime failure occurred",
                    ExecutionResult.Skipped => "Skipped — either the entire test did not run (e.g. a skip attribute or filter excluded it), or a step was skipped at runtime which also prevented all subsequent steps from executing",
                    ExecutionResult.Bypassed => "Bypassed — some or all of the logic in a step was intentionally skipped over at runtime without preventing execution of subsequent steps",
                    ExecutionResult.SkippedAfterFailure => "Skipped after failure — this scenario was never reached because an earlier step failed",
                    _ => ""
                };

                body.Append($"""
                         <details class="scenario{(scenario.IsHappyPath ? " happy-path" : "")}"{depsAttr}{statusAttr}{searchAttr}{durationAttr}{categoriesAttr}{labelsAttr} id="{anchorId}" tabindex="0">
                            <summary class="h3{(failed ? " failed" : skipped ? " skipped" : "")}" title="{scenarioTooltip}">{scenario.DisplayName}{(scenario.IsHappyPath ? " <span class=\"label\">Happy Path</span>" : "")}{scenarioLabelsHtml}{durationBadge}<button class="copy-scenario-name" title="Copy scenario name" data-scenario-name="{encodedName}" onclick="copy_scenario_name(this, event)">&#128203;</button><a class="scenario-link" href="#{anchorId}" title="Link to this scenario" onclick="event.stopPropagation()">&#128279;</a></summary>
                         """);

                if (failed)
                {
                    var diffHtml = "";
                    var diffResult = ErrorDiffParser.TryParseExpectedActual(scenario.ErrorMessage);
                    if (diffResult is not null)
                        diffHtml = ErrorDiffParser.GenerateDiffHtml(diffResult.Expected, diffResult.Actual);

                    body.Append($"""
                              <details class="failure-result" open>
                                 <summary class="h4">Failure Result</summary>
                                 <pre>
                              Failure Cause: {scenario.ErrorMessage}
                              
                              {scenario.ErrorStackTrace}
                                 </pre>
                                 {diffHtml}
                              </details>
                              """);
                }

                if (scenario.Steps is { Length: > 0 })
                {
                    body.Append("<div class=\"scenario-steps\">");
                    for (var si = 0; si < scenario.Steps.Length; si++)
                    {
                        var numberPrefix = showStepNumbers ? $"{si + 1}." : null;
                        RenderStep(body, scenario.Steps[si], numberPrefix);
                    }
                    body.Append("</div>");

                    RenderCombinedTabularParameters(body, scenario.Steps);
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
                            body.Append("<span class=\"diagram-toggle-spacer\"></span><span class=\"details-radio\"><span class=\"details-radio-label\">Details:</span><button class=\"details-radio-btn\" data-state=\"expanded\" onclick=\"window._setAllNotes(this,'expanded')\">Expanded</button><button class=\"details-radio-btn\" data-state=\"collapsed\" onclick=\"window._setAllNotes(this,'collapsed')\">Collapsed</button><button class=\"details-radio-btn details-active\" data-state=\"truncated\" onclick=\"window._setAllNotes(this,'truncated')\">Truncated</button><select class=\"truncate-lines-select\" onchange=\"window._setScenarioTruncateLines(this)\"><option value=\"3\">3</option><option value=\"4\">4</option><option value=\"5\">5</option><option value=\"10\">10</option><option value=\"15\">15</option><option value=\"20\">20</option><option value=\"25\">25</option><option value=\"30\">30</option><option value=\"35\">35</option><option value=\"40\" selected>40</option><option value=\"50\">50</option><option value=\"60\">60</option><option value=\"80\">80</option><option value=\"100\">100</option></select><span class=\"truncate-lines-label\">lines</span></span><span class=\"headers-radio\"><span class=\"details-radio-label\">Headers:</span><button class=\"details-radio-btn headers-radio-btn details-active\" data-hstate=\"shown\" onclick=\"window._setScenarioHeaders(this,'shown')\">Shown</button><button class=\"details-radio-btn headers-radio-btn\" data-hstate=\"hidden\" onclick=\"window._setScenarioHeaders(this,'hidden')\">Hidden</button></span>");
                        body.Append("</div>");
                    }
                    else if (hasSequenceDiagrams)
                    {
                        body.Append("<summary class=\"h4\">Sequence Diagrams</summary>");
                        if (isPlantUmlBrowser)
                        {
                            body.Append("<div class=\"diagram-toggle\">");
                            body.Append("<span class=\"diagram-toggle-spacer\"></span><span class=\"details-radio\"><span class=\"details-radio-label\">Details:</span><button class=\"details-radio-btn\" data-state=\"expanded\" onclick=\"window._setAllNotes(this,'expanded')\">Expanded</button><button class=\"details-radio-btn\" data-state=\"collapsed\" onclick=\"window._setAllNotes(this,'collapsed')\">Collapsed</button><button class=\"details-radio-btn details-active\" data-state=\"truncated\" onclick=\"window._setAllNotes(this,'truncated')\">Truncated</button><select class=\"truncate-lines-select\" onchange=\"window._setScenarioTruncateLines(this)\"><option value=\"3\">3</option><option value=\"4\">4</option><option value=\"5\">5</option><option value=\"10\">10</option><option value=\"15\">15</option><option value=\"20\">20</option><option value=\"25\">25</option><option value=\"30\">30</option><option value=\"35\">35</option><option value=\"40\" selected>40</option><option value=\"50\">50</option><option value=\"60\">60</option><option value=\"80\">80</option><option value=\"100\">100</option></select><span class=\"truncate-lines-label\">lines</span></span><span class=\"headers-radio\"><span class=\"details-radio-label\">Headers:</span><button class=\"details-radio-btn headers-radio-btn details-active\" data-hstate=\"shown\" onclick=\"window._setScenarioHeaders(this,'shown')\">Shown</button><button class=\"details-radio-btn headers-radio-btn\" data-hstate=\"hidden\" onclick=\"window._setScenarioHeaders(this,'hidden')\">Hidden</button></span>");
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
                                body.Append("<span class=\"diagram-toggle-spacer\"></span><span class=\"details-radio\"><span class=\"details-radio-label\">Details:</span><button class=\"details-radio-btn\" data-state=\"expanded\" onclick=\"window._setAllNotes(this,'expanded')\">Expanded</button><button class=\"details-radio-btn\" data-state=\"collapsed\" onclick=\"window._setAllNotes(this,'collapsed')\">Collapsed</button><button class=\"details-radio-btn details-active\" data-state=\"truncated\" onclick=\"window._setAllNotes(this,'truncated')\">Truncated</button><select class=\"truncate-lines-select\" onchange=\"window._setScenarioTruncateLines(this)\"><option value=\"3\">3</option><option value=\"4\">4</option><option value=\"5\">5</option><option value=\"10\">10</option><option value=\"15\">15</option><option value=\"20\">20</option><option value=\"25\">25</option><option value=\"30\">30</option><option value=\"35\">35</option><option value=\"40\" selected>40</option><option value=\"50\">50</option><option value=\"60\">60</option><option value=\"80\">80</option><option value=\"100\">100</option></select><span class=\"truncate-lines-label\">lines</span></span><span class=\"headers-radio\"><span class=\"details-radio-label\">Headers:</span><button class=\"details-radio-btn headers-radio-btn details-active\" data-hstate=\"shown\" onclick=\"window._setScenarioHeaders(this,'shown')\">Shown</button><button class=\"details-radio-btn headers-radio-btn\" data-hstate=\"hidden\" onclick=\"window._setScenarioHeaders(this,'hidden')\">Hidden</button></span>");
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
                        var rawLabel = "Raw Plant UML";
                        foreach (var diagram in diagramsForTest)
                        {
                            if (isPlantUmlBrowser)
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
            if (ruleOpen)
            {
                body.Append("</details>"); // close last rule
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
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ExecutionResult.Failed)))
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

    private static (int Passed, int Failed, int Skipped) CountStepsByStatusRecursive(ScenarioStep[] steps)
    {
        var passed = 0;
        var failed = 0;
        var skipped = 0;
        foreach (var step in steps)
        {
            switch (step.Status)
            {
                case ExecutionResult.Passed: passed++; break;
                case ExecutionResult.Failed: failed++; break;
                case ExecutionResult.Skipped or ExecutionResult.Bypassed or ExecutionResult.SkippedAfterFailure: skipped++; break;
                default: skipped++; break;
            }
            if (step.SubSteps is { Length: > 0 })
            {
                var sub = CountStepsByStatusRecursive(step.SubSteps);
                passed += sub.Passed;
                failed += sub.Failed;
                skipped += sub.Skipped;
            }
        }
        return (passed, failed, skipped);
    }

    internal static string GeneratePieChartSvg(int passed, int failed, int skipped, int bypassed)
    {
        var total = passed + failed + skipped + bypassed;
        if (total == 0) return "";

        var passRate = (int)Math.Round(100.0 * passed / total);
        var segments = new List<(double pct, string color, string label, int count)>();
        if (passed > 0) segments.Add((100.0 * passed / total, "#1daf26", "Passed", passed));
        if (failed > 0) segments.Add((100.0 * failed / total, "#cc0000", "Failed", failed));
        if (skipped > 0) segments.Add((100.0 * skipped / total, "#949494", "Skipped", skipped));
        if (bypassed > 0) segments.Add((100.0 * bypassed / total, "#2e7bff", "Bypassed", bypassed));

        const double radius = 40;
        const double circumference = 2 * Math.PI * radius;
        var sb = new StringBuilder();
        sb.Append("<div class=\"summary-chart\">");
        sb.Append("<svg viewBox=\"0 0 100 100\">");

        var offset = 0.0;
        foreach (var (pct, color, label, count) in segments)
        {
            var dash = circumference * pct / 100.0;
            var gap = circumference - dash;
            var dashOffset = -offset * circumference / 100.0;
            sb.Append($"<circle cx=\"50\" cy=\"50\" r=\"{radius:F1}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"12\" " +
                      $"stroke-dasharray=\"{dash:F2} {gap:F2}\" stroke-dashoffset=\"{dashOffset:F2}\" transform=\"rotate(-90 50 50)\">" +
                      $"<title>{label}: {count} ({pct:F0}%)</title></circle>");
            offset += pct;
        }

        sb.Append($"<text x=\"50\" y=\"50\" text-anchor=\"middle\" dominant-baseline=\"central\" font-size=\"16\" font-weight=\"bold\" fill=\"#333\">{passRate}%</text>");
        sb.Append("</svg></div>");
        return sb.ToString();
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

    private static bool HasAnyBypassed(ScenarioStep step)
    {
        if (step.SubSteps is not { Length: > 0 }) return false;
        foreach (var sub in step.SubSteps)
        {
            if (sub.Status == ExecutionResult.Bypassed) return true;
            if (HasAnyBypassed(sub)) return true;
        }
        return false;
    }

    private static bool HasAnySkipped(ScenarioStep step)
    {
        if (step.SubSteps is not { Length: > 0 }) return false;
        foreach (var sub in step.SubSteps)
        {
            if (sub.Status == ExecutionResult.Skipped) return true;
            if (HasAnySkipped(sub)) return true;
        }
        return false;
    }

    private static void RenderStep(StringBuilder body, ScenarioStep step, string? numberPrefix = null)
    {
        var statusClass = step.Status switch
        {
            ExecutionResult.Passed => HasAnySkipped(step) ? "passed-skipped" : HasAnyBypassed(step) ? "passed-bypassed" : "passed",
            ExecutionResult.Failed => "failed",
            ExecutionResult.Skipped => "skipped",
            ExecutionResult.Bypassed => "bypassed",
            ExecutionResult.SkippedAfterFailure => "skipped-after-failure",
            _ => ""
        };

        var statusIcon = step.Status switch
        {
            ExecutionResult.Passed => "&#10003;",
            ExecutionResult.Failed => "&#10005;",
            ExecutionResult.Skipped => "&#216;",
            ExecutionResult.Bypassed => "&#8631;",
            ExecutionResult.SkippedAfterFailure => "!",
            _ => ""
        };

        var statusTooltip = step.Status switch
        {
            ExecutionResult.Passed => HasAnySkipped(step)
                ? "Passed (with skipped sub-steps) — all assertions passed, but one or more sub-steps were skipped. Skipped steps did not execute and also prevented execution of subsequent steps"
                : HasAnyBypassed(step)
                ? "Passed (with bypassed sub-steps) — all assertions passed, but one or more sub-steps were bypassed (intentionally skipped over without preventing execution of subsequent steps)"
                : "Passed — all assertions in this step passed",
            ExecutionResult.Failed => "Failed — this step threw an exception or an assertion failed",
            ExecutionResult.Skipped => "Skipped — this step did not execute because it was intentionally skipped, either at the scenario level, or at the step level. In the latter case the skip also prevented execution of subsequent steps",
            ExecutionResult.Bypassed => "Bypassed — some or all of the logic in this step was intentionally skipped over without preventing execution of subsequent steps",
            ExecutionResult.SkippedAfterFailure => "Skipped after failure — this step was never reached because an earlier step failed",
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

        if (numberPrefix is not null)
        {
            body.Append($"<span class=\"step-number\">{numberPrefix}</span>");
        }

        if (step.Status.HasValue)
        {
            body.Append($"<span class=\"step-status {statusClass}\" title=\"{statusTooltip}\">{statusIcon}</span>");
        }

        if (step.Keyword is not null)
        {
            body.Append($"<span class=\"step-keyword\">{System.Net.WebUtility.HtmlEncode(step.Keyword)}</span> ");
        }

        var stepText = step.Text;

        // Strip tabular parameter reference suffixes like [paramName: "<$paramName>"] from step text
        if (step.Parameters?.Any(p => p.Kind == StepParameterKind.Tabular) == true)
            stepText = StripTabularParamSuffixRegex().Replace(stepText, "").TrimEnd();

        body.Append($"<span class=\"step-text\">{System.Net.WebUtility.HtmlEncode(stepText)}</span>");

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
                if (param.Kind == StepParameterKind.Tabular) continue; // Rendered as combined table at scenario level
                RenderParameter(body, param);
            }
        }

        if (step.DocString is not null)
        {
            var codeClassAttr = step.DocStringMediaType is not null
                ? $" class=\"language-{System.Net.WebUtility.HtmlEncode(step.DocStringMediaType)}\""
                : "";
            body.Append($"<pre class=\"step-docstring\"><code{codeClassAttr}>{System.Net.WebUtility.HtmlEncode(step.DocString)}</code></pre>");
        }

        if (hasSubSteps)
        {
            body.Append("<div class=\"sub-steps\">");
            for (var ssi = 0; ssi < step.SubSteps!.Length; ssi++)
            {
                var subPrefix = numberPrefix is not null ? $"{numberPrefix}{ssi + 1}." : null;
                RenderStep(body, step.SubSteps[ssi], subPrefix);
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

    private static void RenderCombinedTabularParameters(StringBuilder body, ScenarioStep[] steps)
    {
        var tabularParams = steps
            .Where(s => s.Parameters is { Length: > 0 })
            .SelectMany(s => s.Parameters!)
            .Where(p => p.Kind == StepParameterKind.Tabular && p.TabularValue is not null)
            .Select(p => p.TabularValue!)
            .ToArray();

        if (tabularParams.Length == 0) return;

        var hasSeparator = tabularParams.Length > 1;

        body.Append("<div class=\"step-param-combined-table\"><table><thead><tr><th></th>");

        // Input columns (all tables except the last)
        var inputTables = tabularParams.Length > 1 ? tabularParams[..^1] : tabularParams;
        var outputTable = tabularParams.Length > 1 ? tabularParams[^1] : null;

        foreach (var table in inputTables)
        {
            foreach (var col in table.Columns)
                body.Append($"<th{(col.IsKey ? " class=\"key\"" : "")}>{System.Net.WebUtility.HtmlEncode(col.Name)}</th>");
        }

        if (hasSeparator)
        {
            body.Append("<th class=\"combined-separator\">=</th>");

            foreach (var col in outputTable!.Columns)
                body.Append($"<th{(col.IsKey ? " class=\"key\"" : "")}>{System.Net.WebUtility.HtmlEncode(col.Name)}</th>");
        }

        body.Append("</tr></thead><tbody>");

        // Determine row count from the table with the most rows
        var maxRows = tabularParams.Max(t => t.Rows.Length);

        for (var ri = 0; ri < maxRows; ri++)
        {
            // Row type is determined by the output (last) table if it has this row, otherwise input
            var rowType = outputTable is not null && ri < outputTable.Rows.Length
                ? outputTable.Rows[ri].Type
                : inputTables[0].Rows.Length > ri
                    ? inputTables[0].Rows[ri].Type
                    : TableRowType.Matching;

            var rowIndicator = rowType switch
            {
                TableRowType.Matching => "=",
                TableRowType.Surplus => "+",
                TableRowType.Missing => "-",
                _ => ""
            };

            body.Append($"<tr class=\"row-{rowType.ToString().ToLowerInvariant()}\"><td>{rowIndicator}</td>");

            // Input cells
            foreach (var table in inputTables)
            {
                if (ri < table.Rows.Length)
                {
                    foreach (var cell in table.Rows[ri].Values)
                        RenderCell(body, cell);
                }
                else
                {
                    for (var ci = 0; ci < table.Columns.Length; ci++)
                        body.Append("<td></td>");
                }
            }

            if (hasSeparator)
            {
                body.Append("<td class=\"combined-separator\"></td>");

                // Output cells
                if (ri < outputTable!.Rows.Length)
                {
                    foreach (var cell in outputTable.Rows[ri].Values)
                        RenderCell(body, cell);
                }
                else
                {
                    for (var ci = 0; ci < outputTable.Columns.Length; ci++)
                        body.Append("<td></td>");
                }
            }

            body.Append("</tr>");
        }

        body.Append("</tbody></table></div>");
    }

    private static void RenderCell(StringBuilder body, TabularCell cell)
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

    private static readonly Regex StripTabularParamSuffixCompiledRegex = new(@"\s*\[[a-zA-Z_]\w*:\s*""<\$[a-zA-Z_]\w*>""\]", RegexOptions.Compiled);
    private static Regex StripTabularParamSuffixRegex() => StripTabularParamSuffixCompiledRegex;

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

    public static string GenerateTestRunReportData(Feature[] features, DateTime startTime, DateTime endTime, string fileName, DataFormat format)
    {
        return format switch
        {
            DataFormat.Json => WriteFile(GenerateTestRunReportJson(features, startTime, endTime), fileName),
            DataFormat.Xml => WriteFile(GenerateTestRunReportXml(features, startTime, endTime), fileName),
            DataFormat.Yaml => WriteFile(GenerateTestRunReportYaml(features, startTime, endTime), fileName),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static string GenerateTestRunReportJson(Feature[] features, DateTime startTime, DateTime endTime)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        var data = new
        {
            StartTime = startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            EndTime = endTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Features = features.OrderBy(f => f.DisplayName).Select(f => new
            {
                Name = f.DisplayName,
                f.Endpoint,
                f.Description,
                Labels = f.Labels ?? [],
                Scenarios = f.Scenarios.Select(s => new
                {
                    s.Id,
                    Name = s.DisplayName,
                    Result = s.Result.ToString(),
                    DurationSeconds = s.Duration?.TotalSeconds ?? 0.0,
                    s.IsHappyPath,
                    s.ErrorMessage,
                    s.ErrorStackTrace,
                    Labels = s.Labels ?? [],
                    Categories = s.Categories ?? [],
                    Steps = (s.Steps ?? []).Select(MapStepJson).ToArray()
                }).ToArray()
            }).ToArray()
        };
        return JsonSerializer.Serialize(data, options);
    }

    private static object MapStepJson(ScenarioStep step) => new
    {
        step.Keyword,
        step.Text,
        Status = step.Status?.ToString(),
        DurationSeconds = step.Duration?.TotalSeconds,
        SubSteps = (step.SubSteps ?? []).Select(MapStepJson).ToArray()
    };

    private static string GenerateTestRunReportXml(Feature[] features, DateTime startTime, DateTime endTime)
    {
        var doc = new XDocument(
            new XElement("TestRunReport",
                new XElement("StartTime", startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")),
                new XElement("EndTime", endTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")),
                new XElement("Features",
                    features.OrderBy(f => f.DisplayName).Select(f =>
                        new XElement("Feature",
                            new XElement("Name", f.DisplayName),
                            f.Endpoint != null ? new XElement("Endpoint", f.Endpoint) : null,
                            f.Description != null ? new XElement("Description", f.Description) : null,
                            (f.Labels is { Length: > 0 }) ? new XElement("Labels", f.Labels.Select(l => new XElement("Label", l))) : null,
                            new XElement("Scenarios",
                                f.Scenarios.Select(s =>
                                    new XElement("Scenario",
                                        new XElement("Id", s.Id),
                                        new XElement("Name", s.DisplayName),
                                        new XElement("Result", s.Result.ToString()),
                                        new XElement("DurationSeconds", (s.Duration?.TotalSeconds ?? 0.0).ToString("F3")),
                                        new XElement("IsHappyPath", s.IsHappyPath.ToString().ToLower()),
                                        s.ErrorMessage != null ? new XElement("ErrorMessage", s.ErrorMessage) : null,
                                        s.ErrorStackTrace != null ? new XElement("ErrorStackTrace", s.ErrorStackTrace) : null,
                                        (s.Labels is { Length: > 0 }) ? new XElement("Labels", s.Labels.Select(l => new XElement("Label", l))) : null,
                                        (s.Categories is { Length: > 0 }) ? new XElement("Categories", s.Categories.Select(c => new XElement("Category", c))) : null,
                                        (s.Steps is { Length: > 0 }) ? new XElement("Steps", s.Steps.Select(MapStepXml)) : null
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );
        return doc.ToString();
    }

    private static XElement MapStepXml(ScenarioStep step) =>
        new("Step",
            step.Keyword != null ? new XElement("Keyword", step.Keyword) : null,
            new XElement("Text", step.Text),
            step.Status != null ? new XElement("Status", step.Status.ToString()) : null,
            step.Duration != null ? new XElement("DurationSeconds", step.Duration.Value.TotalSeconds.ToString("F3")) : null,
            (step.SubSteps is { Length: > 0 }) ? new XElement("SubSteps", step.SubSteps.Select(MapStepXml)) : null
        );

    private static string GenerateTestRunReportYaml(Feature[] features, DateTime startTime, DateTime endTime)
    {
        var yml = new StringBuilder();
        yml.Append("StartTime: " + startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") + "\n");
        yml.Append("EndTime: " + endTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") + "\n");
        yml.Append("Features:\n");

        foreach (var feature in features.OrderBy(f => f.DisplayName))
        {
            yml.Append("  - Name: " + feature.DisplayName.SanitiseForYml() + "\n");

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
            foreach (var scenario in feature.Scenarios)
            {
                yml.Append("      - Name: " + scenario.DisplayName.SanitiseForYml() + "\n");
                yml.Append("        Result: " + scenario.Result + "\n");
                yml.Append("        DurationSeconds: " + (scenario.Duration?.TotalSeconds ?? 0.0).ToString("F3") + "\n");
                yml.Append("        IsHappyPath: " + scenario.IsHappyPath.ToString().ToLower() + "\n");

                if (scenario.ErrorMessage is not null)
                    yml.Append("        ErrorMessage: " + scenario.ErrorMessage.SanitiseForYml() + "\n");

                if (scenario.ErrorStackTrace is not null)
                    yml.Append("        ErrorStackTrace: " + scenario.ErrorStackTrace.SanitiseForYml() + "\n");

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
                        AppendTestRunYamlStep(yml, step, "          ");
                }
            }
        }

        return yml.ToString();
    }

    private static void AppendTestRunYamlStep(StringBuilder yml, ScenarioStep step, string indent)
    {
        yml.Append(indent + "- Keyword: " + (step.Keyword ?? "").SanitiseForYml() + "\n");
        yml.Append(indent + "  Text: " + step.Text.SanitiseForYml() + "\n");
        yml.Append(indent + "  Status: " + (step.Status?.ToString() ?? "") + "\n");
        if (step.Duration != null)
            yml.Append(indent + "  DurationSeconds: " + step.Duration.Value.TotalSeconds.ToString("F3") + "\n");

        if (step.SubSteps is { Length: > 0 })
        {
            yml.Append(indent + "  SubSteps:\n");
            foreach (var sub in step.SubSteps)
                AppendTestRunYamlStep(yml, sub, indent + "    ");
        }
    }

    public static string GenerateSpecificationsData(Feature[] features, string fileName, string title, DataFormat format, bool generateBlankOnFailedTests = false)
    {
        if (generateBlankOnFailedTests && features.Any(x => x.Scenarios.Any(y => y.Result == ExecutionResult.Failed)))
            return WriteFile(string.Empty, fileName);

        return format switch
        {
            DataFormat.Yaml => WriteFile(GenerateSpecificationsYaml(features, title), fileName),
            DataFormat.Json => WriteFile(GenerateSpecificationsJson(features, title), fileName),
            DataFormat.Xml => WriteFile(GenerateSpecificationsXml(features, title), fileName),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static string GenerateSpecificationsYaml(Feature[] features, string title)
    {
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

        return yml.ToString();
    }

    private static string GenerateSpecificationsJson(Feature[] features, string title)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        var data = new
        {
            Title = title,
            Features = features.OrderBy(f => f.DisplayName).Select(f => new
            {
                Name = f.DisplayName,
                f.Endpoint,
                f.Description,
                Labels = f.Labels ?? [],
                Scenarios = f.Scenarios.OrderByDescending(s => s.IsHappyPath).ThenBy(s => s.DisplayName).Select(s => new
                {
                    Name = s.DisplayName,
                    s.IsHappyPath,
                    Labels = s.Labels ?? [],
                    Categories = s.Categories ?? [],
                    Steps = (s.Steps ?? []).Select(MapSpecStepJson).ToArray()
                }).ToArray()
            }).ToArray()
        };
        return JsonSerializer.Serialize(data, options);
    }

    private static string MapSpecStepJson(ScenarioStep step)
    {
        var text = step.Keyword is not null ? $"{step.Keyword} {step.Text}" : step.Text;
        // Specifications steps are text-only (matching YAML format)
        // SubSteps are not included as separate entries per the YAML spec format
        return text;
    }

    private static string GenerateSpecificationsXml(Feature[] features, string title)
    {
        var doc = new XDocument(
            new XElement("Specifications",
                new XElement("Title", title),
                new XElement("Features",
                    features.OrderBy(f => f.DisplayName).Select(f =>
                        new XElement("Feature",
                            new XElement("Name", f.DisplayName),
                            f.Endpoint != null ? new XElement("Endpoint", f.Endpoint) : null,
                            f.Description != null ? new XElement("Description", f.Description) : null,
                            (f.Labels is { Length: > 0 }) ? new XElement("Labels", f.Labels.Select(l => new XElement("Label", l))) : null,
                            new XElement("Scenarios",
                                f.Scenarios.OrderByDescending(s => s.IsHappyPath).ThenBy(s => s.DisplayName).Select(s =>
                                    new XElement("Scenario",
                                        new XElement("Name", s.DisplayName),
                                        new XElement("IsHappyPath", s.IsHappyPath.ToString().ToLower()),
                                        (s.Labels is { Length: > 0 }) ? new XElement("Labels", s.Labels.Select(l => new XElement("Label", l))) : null,
                                        (s.Categories is { Length: > 0 }) ? new XElement("Categories", s.Categories.Select(c => new XElement("Category", c))) : null,
                                        (s.Steps is { Length: > 0 }) ? new XElement("Steps", s.Steps.Select(MapSpecStepXml)) : null
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );
        return doc.ToString();
    }

    private static XElement MapSpecStepXml(ScenarioStep step)
    {
        var text = step.Keyword is not null ? $"{step.Keyword} {step.Text}" : step.Text;
        var element = new XElement("Step", text);
        if (step.SubSteps is { Length: > 0 })
        {
            foreach (var sub in step.SubSteps)
                element.Add(MapSpecStepXml(sub));
        }
        return element;
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

    internal static HashSet<string> ExtractDependencies(string codeBehind, DiagramFormat format)
    {
        var deps = new HashSet<string>();
        if (string.IsNullOrEmpty(codeBehind)) return deps;

        foreach (var line in codeBehind.Split('\n'))
        {
            var trimmed = line.Trim();

            // Match: entity "ServiceName" as alias  OR  participant "ServiceName" as alias
            // Skip: actor "Caller" as caller (these are the test caller, not a dependency)
            var match = System.Text.RegularExpressions.Regex.Match(trimmed,
                @"^(?:entity|participant)\s+""([^""]+)""\s+as\s+");
            if (match.Success)
                deps.Add(match.Groups[1].Value);
        }

        return deps;
    }

    private static string GetDataFormatExtension(DataFormat format) => format switch
    {
        DataFormat.Json => "json",
        DataFormat.Xml => "xml",
        DataFormat.Yaml => "yml",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}