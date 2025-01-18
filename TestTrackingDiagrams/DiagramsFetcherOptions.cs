namespace TestTrackingDiagrams;

public record DiagramsFetcherOptions
{
    public string PlantUmlServerBaseUrl { get; set; } = "https://plantuml.com/plantuml";
    public Func<string, string>? RequestPostFormattingProcessor { get; set; }
    public Func<string, string>? RequestPreFormattingProcessor { get; set; }
    public Func<string, string>? ResponsePostFormattingProcessor { get; set; }
    public Func<string, string>? ResponsePreFormattingProcessor { get; set; }
    public IEnumerable<string> ExcludedHeaders { get; set; } = [];
}