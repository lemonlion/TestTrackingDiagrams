using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

[AttributeUsage(AttributeTargets.Method)]
/// <summary>
/// NUnit property attribute that marks a test method as a happy-path scenario for report classification.
/// </summary>
public class HappyPathAttribute() : PropertyAttribute(HappyPathPropertyKey, HappyPathPropertyKey)
{
    public const string HappyPathPropertyKey = "Happy Path";
}