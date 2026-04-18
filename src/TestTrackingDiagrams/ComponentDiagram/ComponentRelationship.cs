namespace TestTrackingDiagrams.ComponentDiagram;

public record ComponentRelationship(
    string Caller,
    string Service,
    string Protocol,
    HashSet<string> Methods,
    int CallCount,
    int TestCount);
