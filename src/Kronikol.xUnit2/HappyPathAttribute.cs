using Xunit;
using Xunit.Sdk;

namespace Kronikol.xUnit2;

/// <summary>
/// Marks a test method as a happy-path scenario for report filtering.
/// Also exposes this as an xUnit v2 trait for test filtering.
/// </summary>
[TraitDiscoverer("Kronikol.xUnit2.HappyPathTraitDiscoverer", "Kronikol.xUnit2")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class HappyPathAttribute : Attribute, ITraitAttribute
{
    public const string HappyPathTraitKey = "Happy Path";
}
