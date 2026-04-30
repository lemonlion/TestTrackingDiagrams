using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

/// <summary>
/// NUnit property attribute that marks a test class with the API endpoint it tests, used for grouping tests in generated reports.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EndpointAttribute(string endpoint) : PropertyAttribute(EndpointPropertyKey, endpoint)
{
    public const string EndpointPropertyKey = "Endpoint";
}