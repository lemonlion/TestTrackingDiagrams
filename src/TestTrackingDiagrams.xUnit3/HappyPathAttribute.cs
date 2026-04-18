using Xunit.v3;

namespace TestTrackingDiagrams.xUnit3;

public class HappyPathAttribute : Attribute, ITraitAttribute
{
    public const string HappyPathTraitKey = "Happy Path";

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [ new KeyValuePair<string, string>(HappyPathTraitKey, HappyPathTraitKey) ];
    }
}