using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

[AttributeUsage(AttributeTargets.Class)]
/// <summary>
/// NUnit property attribute that marks a test class with the API endpoint it tests, used for grouping tests in generated reports.
/// </summary>
public class EndpointAttribute(string endpoint) : PropertyAttribute(EndpointPropertyKey, endpoint)
{
    public const string EndpointPropertyKey = "Endpoint";
}