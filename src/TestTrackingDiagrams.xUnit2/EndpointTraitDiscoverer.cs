using Xunit.Abstractions;
using Xunit.Sdk;

namespace TestTrackingDiagrams.xUnit2;

/// <summary>
/// Implements xUnit v2 trait discovery for the <c>Endpoint</c> attribute, enabling test grouping by API endpoint.
/// </summary>
public class EndpointTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var args = traitAttribute.GetConstructorArguments().ToArray();
        var endpoint = args.Length > 0 ? args[0]?.ToString() ?? "" : "";
        yield return new KeyValuePair<string, string>(EndpointAttribute.EndpointTraitKey, endpoint);
    }
}