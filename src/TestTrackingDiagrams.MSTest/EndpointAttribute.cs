namespace TestTrackingDiagrams.MSTest;

[AttributeUsage(AttributeTargets.Class)]
/// <summary>
/// Marks an MSTest class with the API endpoint it tests, used for grouping tests in generated reports.
/// </summary>
public class EndpointAttribute(string endpoint) : Attribute
{
    public const string EndpointPropertyKey = "Endpoint";
    public string Endpoint { get; } = endpoint;
}