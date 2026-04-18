using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

public class HappyPathAttribute() : CategoryAttribute(HappyPathCategoryKey)
{
    public const string HappyPathCategoryKey = "Happy Path";
}
