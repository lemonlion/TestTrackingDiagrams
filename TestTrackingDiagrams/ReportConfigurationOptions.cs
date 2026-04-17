using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams;

public record ReportConfigurationOptions
{
    public ComponentDiagramOptions? ComponentDiagramOptions { get; set; }
    public string PlantUmlServerBaseUrl { get; set; } = "https://plantuml.com/plantuml";
    public Func<string, string>? RequestResponsePostProcessor { get; set; }
    public Func<string, string>? RequestResponseMidProcessor { get; set; }
    public string SpecificationsTitle { get; set; } = "Service Specifications";
    public string HtmlSpecificationsFileName { get; set; } = "Specifications";
    public string HtmlTestRunReportFileName { get; set; } = "TestRunReport";
    public string? HtmlSpecificationsCustomStyleSheet { get; set; } = Stylesheets.VioletThemeStyleSheet;
    public string YamlSpecificationsFileName { get; set; } = "Specifications";
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
    public PlantUmlRendering PlantUmlRendering { get; set; } = PlantUmlRendering.BrowserJs;
    public bool InlineSvgRendering { get; set; }
    public bool InternalFlowTracking { get; set; } = true;
    public InternalFlowDisplay InternalFlowDisplay { get; set; } = InternalFlowDisplay.Popup;
    public InternalFlowTrigger InternalFlowTrigger { get; set; } = InternalFlowTrigger.Click;
    public InternalFlowDiagramStyle InternalFlowDiagramStyle { get; set; } = InternalFlowDiagramStyle.ActivityDiagram;
    public InternalFlowSpanGranularity InternalFlowSpanGranularity { get; set; } = InternalFlowSpanGranularity.AutoInstrumentation;
    public string[]? InternalFlowActivitySources { get; set; }
    public InternalFlowNoDataBehavior InternalFlowNoDataBehavior { get; set; } = InternalFlowNoDataBehavior.HideLink;
    public InternalFlowHasDataBehavior InternalFlowHasDataBehavior { get; set; } = InternalFlowHasDataBehavior.ShowLinkOnHover;
    public bool InternalFlowShowFlameChart { get; set; } = true;
    public InternalFlowFlameChartPosition InternalFlowFlameChartPosition { get; set; } = InternalFlowFlameChartPosition.BehindWithToggle;
    public InternalFlowContentStrategy InternalFlowContentStrategy { get; set; } = InternalFlowContentStrategy.Embedded;
    public string InternalFlowFragmentsFolderName { get; set; } = "spans";
    public string? InternalFlowPopupCustomStyleSheet { get; set; }
    public WholeTestFlowVisualization WholeTestFlowVisualization { get; set; } = WholeTestFlowVisualization.Both;
    public bool GenerateComponentDiagram { get; set; } = true;
    public bool WriteCiSummary { get; set; }
    public int MaxCiSummaryDiagrams { get; set; } = 10;
    public bool PublishCiArtifacts { get; set; }
    public string CiArtifactName { get; set; } = "TestReports";
    public int CiArtifactRetentionDays { get; set; } = 1;
    public string? FixedNameForReceivingService { get; set; }
    public bool SpecificationsShowStepNumbers { get; set; } = true;
    public bool FeaturesReportShowStepNumbers { get; set; }
    public string? CustomCss { get; set; }
    public string? CustomFaviconBase64 { get; set; }
    public string? CustomLogoHtml { get; set; }
    public DataFormat TestRunReportDataFormat { get; set; } = DataFormat.Json;
    public DataFormat SpecificationsDataFormat { get; set; } = DataFormat.Yaml;
    public bool ActivitySourceDiscovery { get; set; }
    public bool DiagnosticMode { get; set; }
    public bool GroupParameterizedTests { get; set; } = true;
    public int MaxParameterColumns { get; set; } = 10;
}