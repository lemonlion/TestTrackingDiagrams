using LightBDD.Framework.Scenarios;
using LightBDD.TUnit;
using TestTrackingDiagrams.LightBDD;
using TTD.LightBDD.TUnit.Infrastructure;

namespace TTD.LightBDD.TUnit.Scenarios;

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
