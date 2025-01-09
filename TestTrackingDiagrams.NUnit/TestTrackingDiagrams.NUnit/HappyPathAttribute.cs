using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit;

[AttributeUsage(AttributeTargets.Method)]
public class HappyPathAttribute() : PropertyAttribute(HappyPathPropertyKey, HappyPathPropertyKey)
{
    public const string HappyPathPropertyKey = "Happy Path";
}