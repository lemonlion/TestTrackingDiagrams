using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;
using TestTrackingDiagrams.LightBDD;
using TTD.LightBDD.xUnit2.Infrastructure;

namespace TTD.LightBDD.xUnit2.Scenarios;

[FeatureDescription("/")]
public partial class SampleFeature : BaseFixture
{
    [HappyPath]
    [Scenario]
    public async Task Get_root_endpoint_returns_success()
    {
        await Runner.RunScenarioAsync(
            given => The_service_is_running(),
            when => A_GET_request_is_sent_to_the_root_endpoint(),
            then => The_response_should_be_successful());
    }
}
