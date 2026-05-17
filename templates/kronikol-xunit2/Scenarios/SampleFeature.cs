using System.Net;
using FluentAssertions;
using Kronikol.xUnit2;
using Kronikol.xUnit2.Infrastructure;

namespace Kronikol.xUnit2.Scenarios;

[Endpoint("/")]
public class SampleFeature : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Get_root_endpoint_returns_success()
    {
        var response = await Client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
