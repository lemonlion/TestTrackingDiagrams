using LightBDD.Framework;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Marks a LightBDD scenario as a happy-path test for report classification.
/// </summary>
public class HappyPathAttribute : LabelAttribute
{
    public HappyPathAttribute() : base("Happy Path") { }
}