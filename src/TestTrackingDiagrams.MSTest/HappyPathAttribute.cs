namespace TestTrackingDiagrams.MSTest;

[AttributeUsage(AttributeTargets.Method)]
public class HappyPathAttribute : Attribute
{
    public const string HappyPathPropertyKey = "Happy Path";
}
