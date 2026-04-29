namespace TestTrackingDiagrams.MSTest;

[AttributeUsage(AttributeTargets.Method)]
/// <summary>
/// Marks an MSTest method as a happy-path scenario for report classification.
/// </summary>
public class HappyPathAttribute : Attribute
{
    public const string HappyPathPropertyKey = "Happy Path";
}