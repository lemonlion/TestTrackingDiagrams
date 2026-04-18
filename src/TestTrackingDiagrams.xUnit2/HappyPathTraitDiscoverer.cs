using Xunit.Abstractions;
using Xunit.Sdk;

namespace TestTrackingDiagrams.xUnit2;

public class HappyPathTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>(HappyPathAttribute.HappyPathTraitKey, HappyPathAttribute.HappyPathTraitKey);
    }
}
