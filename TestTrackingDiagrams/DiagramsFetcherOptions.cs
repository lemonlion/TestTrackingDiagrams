namespace TestTrackingDiagrams;

public record DiagramsFetcherOptions
{
    public string PlantUmlServerBaseUrl { get; set; } = "https://plantuml.com/plantuml";
    public Func<string, string>? RequestPostFormattingProcessor { get; set; }
    public Func<string, string>? RequestPreFormattingProcessor { get; set; }
    public Func<string, string>? RequestMidFormattingProcessor { get; set; }
    public Func<string, string>? ResponsePostFormattingProcessor { get; set; }
    public Func<string, string>? ResponsePreFormattingProcessor { get; set; }
    public Func<string, string>? ResponseMidFormattingProcessor { get; set; }
    public IEnumerable<string> ExcludedHeaders { get; set; } = [];
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
    public PlantUmlRendering PlantUmlRendering { get; set; } = PlantUmlRendering.Server;
}