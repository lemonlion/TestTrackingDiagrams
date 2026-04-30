namespace TestTrackingDiagrams.MSTest;

/// <summary>
/// Marks an MSTest method as a happy-path scenario for report classification.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HappyPathAttribute : Attribute
{
    public const string HappyPathPropertyKey = "Happy Path";
}