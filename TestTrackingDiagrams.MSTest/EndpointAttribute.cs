namespace TestTrackingDiagrams.MSTest;

[AttributeUsage(AttributeTargets.Class)]
public class EndpointAttribute(string endpoint) : Attribute
{
    public const string EndpointPropertyKey = "Endpoint";
    public string Endpoint { get; } = endpoint;
}
