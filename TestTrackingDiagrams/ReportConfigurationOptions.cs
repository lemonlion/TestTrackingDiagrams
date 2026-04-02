using TestTrackingDiagrams.ComponentDiagram;

namespace TestTrackingDiagrams;

public record ReportConfigurationOptions
{
    public bool GenerateComponentDiagram { get; set; }
    public ComponentDiagramOptions? ComponentDiagramOptions { get; set; }
    public string PlantUmlServerBaseUrl { get; set; } = "https://plantuml.com/plantuml";
    public Func<string, string>? RequestResponsePostProcessor { get; set; }
    public Func<string, string>? RequestResponseMidProcessor { get; set; }
    public string SpecificationsTitle { get; set; } = "Service Specifications";
    public string HtmlSpecificationsFileName { get; set; } = "ComponentSpecificationsWithExamples";
    public string HtmlTestRunReportFileName { get; set; } = "FeaturesReport";
    public string? HtmlSpecificationsCustomStyleSheet { get; set; }
    public string YamlSpecificationsFileName { get; set; } = "ComponentSpecifications";
    public string ReportsFolderPath { get; set; } = "Reports";
    public string[] ExcludedHeaders { get; set; } = [];
    public bool SeparateSetup { get; set; }
    public bool HighlightSetup { get; set; } = true;
    public bool LazyLoadDiagramImages { get; set; } = true;
    public FocusEmphasis FocusEmphasis { get; set; } = FocusEmphasis.Bold;
    public FocusDeEmphasis FocusDeEmphasis { get; set; } = FocusDeEmphasis.LightGray;
    public string? PlantUmlTheme { get; set; }
    public PlantUmlImageFormat PlantUmlImageFormat { get; set; } = PlantUmlImageFormat.Png;
    public Func<string, PlantUmlImageFormat, byte[]>? LocalDiagramRenderer { get; set; }
    public string? LocalDiagramImageDirectory { get; set; }
    public DiagramFormat DiagramFormat { get; set; } = DiagramFormat.PlantUml;
    public bool WriteCiSummary { get; set; }
    public int MaxCiSummaryDiagrams { get; set; } = 10;
    public bool WriteCiSummaryInteractiveHtml { get; set; }
}