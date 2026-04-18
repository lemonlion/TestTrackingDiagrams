using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

public class EndpointAttribute(string endpoint) : PropertyAttribute(EndpointPropertyKey, endpoint)
{
    public const string EndpointPropertyKey = "Endpoint";
}
