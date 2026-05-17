using TUnit.Core;

namespace Kronikol.TUnit;

/// <summary>
/// TUnit category attribute that marks a test method as a happy-path scenario for report classification.
/// </summary>
public class HappyPathAttribute() : CategoryAttribute(HappyPathCategoryKey)
{
    public const string HappyPathCategoryKey = "Happy Path";
}