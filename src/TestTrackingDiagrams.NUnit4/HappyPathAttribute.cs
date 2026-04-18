using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

[AttributeUsage(AttributeTargets.Method)]
public class HappyPathAttribute() : PropertyAttribute(HappyPathPropertyKey, HappyPathPropertyKey)
{
    public const string HappyPathPropertyKey = "Happy Path";
}