using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit;

[AttributeUsage(AttributeTargets.Class)]
public class EndpointAttribute(string endpoint) : PropertyAttribute(EndpointPropertyKey, endpoint)
{
    public const string EndpointPropertyKey = "Endpoint";
}