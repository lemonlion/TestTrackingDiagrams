using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TestStack.BDDfy;
using TestStack.BDDfy.Reporters.Html;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public class DiagramEnhancingBatchProcessor : IBatchProcessor
{
    private static readonly FieldInfo? ScenarioTitleField =
        typeof(Scenario).GetField("<Title>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo? ScenarioTitleProperty =
        typeof(Scenario).GetProperty(nameof(Scenario.Title));

    private readonly DiagramsFetcherOptions? _fetcherOptions;

    public DiagramEnhancingBatchProcessor(DiagramsFetcherOptions? fetcherOptions = null)
    {
        _fetcherOptions = fetcherOptions;
    }

    public void Process(IEnumerable<Story> stories)
    {
        var storiesList = stories.ToList();

        FixScenarioTitles(storiesList);
        GenerateBDDfyHtmlReport(storiesList);
        InjectDiagrams(storiesList);
    }

    private static void FixScenarioTitles(List<Story> stories)
    {
        var scenarioInfos = BDDfyScenarioCollector.GetAll();
        var idToTitle = new Dictionary<string, string>();
        foreach (var info in scenarioInfos)
        {
            if (info.BDDfyScenarioId != null)
                idToTitle[info.BDDfyScenarioId] = info.ScenarioTitle;
        }

        if (idToTitle.Count == 0) return;

        foreach (var story in stories)
        {
            foreach (var scenario in story.Scenarios)
            {
                if (idToTitle.TryGetValue(scenario.Id, out var fixedTitle))
                    SetScenarioTitle(scenario, fixedTitle);
            }
        }
    }

    private static void SetScenarioTitle(Scenario scenario, string title)
    {
        if (ScenarioTitleField != null)
            ScenarioTitleField.SetValue(scenario, title);
        else
            ScenarioTitleProperty?.SetValue(scenario, title);
    }

    private static void GenerateBDDfyHtmlReport(List<Story> stories)
    {
        try
        {
            var reporter = new HtmlReporter(
                new DefaultHtmlReportConfiguration(),
                new ClassicReportBuilder());
            reporter.Process(stories);
        }
        catch
        {
            // If report generation fails, continue without the HTML report.
            // The FeaturesReport is the primary output.
        }
    }

    private void InjectDiagrams(List<Story> stories)
    {
        var bddifyReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BDDfy.html");
        if (!File.Exists(bddifyReportPath)) return;

        var diagrams = DefaultDiagramsFetcher.GetDiagramsFetcher(_fetcherOptions)();
        var scenarioInfos = BDDfyScenarioCollector.GetAll();

        // Build mapping: BDDfy scenario ID -> diagram(s)
        var bddifyIdToDiagrams = new Dictionary<string, List<DefaultDiagramsFetcher.DiagramAsCode>>();
        foreach (var info in scenarioInfos)
        {
            if (info.BDDfyScenarioId == null) continue;
            var matchingDiagrams = diagrams.Where(d => d.TestRuntimeId == info.TestId).ToList();
            if (matchingDiagrams.Count > 0)
                bddifyIdToDiagrams[info.BDDfyScenarioId] = matchingDiagrams;
        }

        if (bddifyIdToDiagrams.Count == 0) return;

        var html = File.ReadAllText(bddifyReportPath);

        // BDDfy's ClassicReportBuilder does not HTML-encode step text, so test data
        // containing HTML (e.g. XSS payloads like <script>alert(1)</script>) is rendered
        // as live markup. Sanitise step content by encoding any raw HTML inside step spans.
        html = Regex.Replace(
            html,
            @"(<li\s+class='step[^']*'[^>]*>\s*<span>)(.*?)(</span>\s*</li>)",
            m => m.Groups[1].Value + WebUtility.HtmlEncode(m.Groups[2].Value) + m.Groups[3].Value,
            RegexOptions.Singleline);

        // Also sanitise the scenario title divs which can contain unescaped test data
        html = Regex.Replace(
            html,
            @"(<div\s+class='[^']*canToggle\s+scenarioTitle'[^>]*>)(.*?)(</div>)",
            m => m.Groups[1].Value + WebUtility.HtmlEncode(m.Groups[2].Value) + m.Groups[3].Value,
            RegexOptions.Singleline);

        // Inject diagram CSS and overrides before </head>
        var diagramCss = """
            <style>
            .tracking-diagram { margin: 10px 0 15px 20px; }
            .tracking-diagram img { max-width: 100%; cursor: pointer; }
            .tracking-diagram details { margin: 5px 0; }
            .tracking-diagram pre { background: #f5f5f5; padding: 10px; overflow-x: auto; font-size: 0.85em; }
            .tracking-diagram h4 { margin: 0.5em 0 0.25em; font-size: 0.95em; color: #555; }
            </style>
            """;
        html = html.Replace("</head>", diagramCss + "</head>");

        // Inject diagram images inside each scenario's step list (before the closing </ul>)
        // so that BDDfy's jQuery toggle mechanism shows/hides diagrams together with steps.
        foreach (var (scenarioId, scenarioDiagrams) in bddifyIdToDiagrams)
        {
            var diagramHtml = new StringBuilder();
            diagramHtml.Append("<li class=\"tracking-diagram\"><h4>Sequence Diagrams</h4>");
            foreach (var diagram in scenarioDiagrams)
            {
                var lazyLoadAttr = _fetcherOptions?.LazyLoadDiagramImages ?? true ? " loading=\"lazy\"" : "";
                diagramHtml.Append($"<details><summary><img{lazyLoadAttr} src=\"{diagram.ImgSrc}\"></summary>");
                diagramHtml.Append($"<h4>Raw PlantUML</h4><pre>{WebUtility.HtmlEncode(diagram.CodeBehind)}</pre>");
                diagramHtml.Append("</details>");
            }
            diagramHtml.Append("</li>");

            // BDDfy's HTML structure: <ul class='steps' id='scenario-1'>...</ul>
            // Insert diagram HTML as final <li> inside the <ul>, before its closing tag
            var pattern = $@"(<ul[^>]*\bid\s*=\s*['""]?{Regex.Escape(scenarioId)}['""]?[^>]*>.*?)(</ul>)";
            html = Regex.Replace(html, pattern, $"$1{diagramHtml}$2", RegexOptions.Singleline);
        }

        // BDDfy's jQuery hides all steps on page load and requires clicking the title to
        // expand. Override so that steps are expanded and diagrams are collapsed by default.
        var overrideScript = """
            <script>
            $(function(){
                // Show steps that BDDfy's script hid on load
                $('ul.steps').css('display','');
                // Collapse all diagram details (the inner details, not the step list)
                $('.tracking-diagram details').removeAttr('open');
            });
            </script>
            """;
        html = html.Replace("</body>", overrideScript + "</body>");

        File.WriteAllText(bddifyReportPath, html);
    }
}
