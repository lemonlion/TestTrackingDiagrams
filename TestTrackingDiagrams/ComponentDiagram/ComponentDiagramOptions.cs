using TestTrackingDiagrams.InternalFlow;

namespace TestTrackingDiagrams.ComponentDiagram;

public record ComponentDiagramOptions
{
    public string FileName { get; set; } = "ComponentDiagram";
    public bool EmbedInFeaturesReport { get; set; } = true;
    public string Title { get; set; } = "Component Diagram";
    public string? PlantUmlTheme { get; set; }
    public Func<string, bool>? ParticipantFilter { get; set; }
    public Func<ComponentRelationship, string>? RelationshipLabelFormatter { get; set; }
    public bool ShowRelationshipFlows { get; set; }
    public InternalFlowDiagramStyle RelationshipFlowStyle { get; set; } = InternalFlowDiagramStyle.ActivityDiagram;
    public bool ShowSystemFlameChart { get; set; }
}
