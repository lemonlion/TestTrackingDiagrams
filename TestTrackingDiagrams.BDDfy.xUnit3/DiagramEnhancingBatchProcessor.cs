using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TestStack.BDDfy;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public class DiagramEnhancingBatchProcessor : IBatchProcessor
{
    private readonly DiagramsFetcherOptions? _fetcherOptions;

    public DiagramEnhancingBatchProcessor(DiagramsFetcherOptions? fetcherOptions = null)
    {
        _fetcherOptions = fetcherOptions;
    }

    public void Process(IEnumerable<Story> stories)
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

        // Inject diagram CSS before </head>
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

        // Inject diagram images after each scenario's steps (identified by id="{scenarioId}")
        foreach (var (scenarioId, scenarioDiagrams) in bddifyIdToDiagrams)
        {
            var diagramHtml = new StringBuilder();
            diagramHtml.Append("<div class=\"tracking-diagram\"><h4>Sequence Diagrams</h4>");
            foreach (var diagram in scenarioDiagrams)
            {
                diagramHtml.Append($"<details><summary><img src=\"{diagram.ImgSrc}\"></summary>");
                diagramHtml.Append($"<h4>Raw PlantUML</h4><pre>{WebUtility.HtmlEncode(diagram.CodeBehind)}</pre>");
                diagramHtml.Append("</details>");
            }
            diagramHtml.Append("</div>");

            // BDDfy's HTML structure: <ul class='steps' id='scenario-1'>...</ul>
            // Insert diagram HTML after the closing </ul> tag for this scenario
            var pattern = $@"(<ul[^>]*\bid\s*=\s*['""]?{Regex.Escape(scenarioId)}['""]?[^>]*>.*?</ul>)";
            html = Regex.Replace(html, pattern, $"$1{diagramHtml}", RegexOptions.Singleline);
        }

        File.WriteAllText(bddifyReportPath, html);
    }
}
