using Xunit.v3;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// xUnit v3 trait attribute that marks a test method as a happy-path scenario for report classification.
/// </summary>
public class HappyPathAttribute : Attribute, ITraitAttribute
{
    public const string HappyPathTraitKey = "Happy Path";

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [ new KeyValuePair<string, string>(HappyPathTraitKey, HappyPathTraitKey) ];
    }
}