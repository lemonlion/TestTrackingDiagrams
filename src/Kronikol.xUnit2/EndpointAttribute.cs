using Xunit;
using Xunit.Sdk;

namespace Kronikol.xUnit2;

/// <summary>
/// Marks a test class with the API endpoint it covers, for grouping in reports.
/// Also exposes the endpoint as an xUnit v2 trait for test filtering.
/// </summary>
[TraitDiscoverer("Kronikol.xUnit2.EndpointTraitDiscoverer", "Kronikol.xUnit2")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EndpointAttribute(string endpoint) : Attribute, ITraitAttribute
{
    public const string EndpointTraitKey = "Endpoint";

    public string Endpoint { get; } = endpoint;
}
