using System.Net;
using FluentAssertions;
using Kronikol.xUnit3;
using Kronikol.xUnit3.Infrastructure;

namespace Kronikol.xUnit3.Scenarios;

[Endpoint("/")]
public class SampleFeature : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Get_root_endpoint_returns_success()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
