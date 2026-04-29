using Xunit.v3;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// xUnit v3 trait attribute that marks a test class with the API endpoint it tests, used for grouping tests in generated reports.
/// </summary>
public class EndpointAttribute(string endpoint) : Attribute, ITraitAttribute
{
    public const string EndpointTraitKey = "Endpoint";

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [new KeyValuePair<string, string>(EndpointTraitKey, endpoint)];
    }
}