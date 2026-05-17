using System.Net;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kronikol.MSTest;
using Kronikol.MSTest.Infrastructure;

namespace Kronikol.MSTest.Scenarios;

[TestClass]
[Endpoint("/")]
public class SampleFeature : BaseFixture
{
    [TestMethod]
    [HappyPath]
    public async Task Get_root_endpoint_returns_success()
    {
        var response = await Client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
