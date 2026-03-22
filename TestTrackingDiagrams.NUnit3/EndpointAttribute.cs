using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit3;

[AttributeUsage(AttributeTargets.Class)]
public class EndpointAttribute(string endpoint) : PropertyAttribute(EndpointPropertyKey, endpoint)
{
    public const string EndpointPropertyKey = "Endpoint";
}