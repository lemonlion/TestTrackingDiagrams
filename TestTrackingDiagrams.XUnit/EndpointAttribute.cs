using Xunit.v3;

namespace TestTrackingDiagrams.XUnit;

public class EndpointAttribute(string endpoint) : Attribute, ITraitAttribute
{
    public const string EndpointTraitKey = "Endpoint";

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [new KeyValuePair<string, string>(EndpointTraitKey, endpoint)];
    }
}