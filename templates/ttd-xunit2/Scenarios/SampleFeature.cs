using System.Net;
using FluentAssertions;
using TestTrackingDiagrams.xUnit2;
using TTD.xUnit2.Infrastructure;

namespace TTD.xUnit2.Scenarios;

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
