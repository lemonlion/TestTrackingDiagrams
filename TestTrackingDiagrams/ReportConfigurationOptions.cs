namespace TestTrackingDiagrams;

public record ReportConfigurationOptions
{
    public string PlantUmlServerBaseUrl { get; set; } = "https://plantuml.com/plantuml";
    public Func<string, string>? RequestResponsePostProcessor { get; set; }
    public string SpecificationsTitle { get; set; } = "Service Specifications";
    public string HtmlSpecificationsFileName { get; set; } = "ComponentSpecificationsWithExamples";
    public string HtmlTestRunReportFileName { get; set; } = "FeaturesReport";
    public string? HtmlSpecificationsCustomStyleSheet { get; set; }
    public string YamlSpecificationsFileName { get; set; } = "ComponentSpecifications";
    public string ReportsFolderPath { get; set; } = "Reports";
    public string[] ExcludedHeaders { get; set; } = [];
    public SetupActionSeparationOptions SetupActionSeparation { get; set; } = new();
}

public record SetupActionSeparationOptions
{
    public SetupActionSeparation SetupActionSeparationType { get; set; } = SetupActionSeparation.SeparateByDiagramSplit;
    public bool DetectActionFromStepNameIfAvailable { get; set; } = true;
    public bool MinimiseSetupDiagramOnLoad { get; set; } = false;
}

public enum SetupActionSeparation
{
    None,
    SeparateByGroup,
    SeparateByDiagramSplit
}