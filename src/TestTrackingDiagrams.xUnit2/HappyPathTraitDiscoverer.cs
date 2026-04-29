using Xunit.Abstractions;
using Xunit.Sdk;

namespace TestTrackingDiagrams.xUnit2;

/// <summary>
/// Implements xUnit v2 trait discovery for the <c>HappyPath</c> attribute, enabling test classification in reports.
/// </summary>
public class HappyPathTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>(HappyPathAttribute.HappyPathTraitKey, HappyPathAttribute.HappyPathTraitKey);
    }
}