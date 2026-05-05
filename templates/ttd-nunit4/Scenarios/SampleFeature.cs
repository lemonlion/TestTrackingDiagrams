using System.Net;
using FluentAssertions;
using TestTrackingDiagrams.NUnit4;
using TTD.NUnit4.Infrastructure;

namespace TTD.NUnit4.Scenarios;

[TestFixture]
[Endpoint("/")]
public class SampleFeature : BaseFixture
{
    [Test]
    [HappyPath]
    public async Task Get_root_endpoint_returns_success()
    {
        var response = await Client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
