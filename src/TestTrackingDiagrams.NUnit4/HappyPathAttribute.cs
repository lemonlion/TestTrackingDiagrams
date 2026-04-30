using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

/// <summary>
/// NUnit property attribute that marks a test method as a happy-path scenario for report classification.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HappyPathAttribute() : PropertyAttribute(HappyPathPropertyKey, HappyPathPropertyKey)
{
    public const string HappyPathPropertyKey = "Happy Path";
}