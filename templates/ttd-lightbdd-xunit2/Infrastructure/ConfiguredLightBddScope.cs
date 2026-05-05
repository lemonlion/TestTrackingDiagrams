using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using LightBDD.XUnit2;
using TestTrackingDiagrams;
using TestTrackingDiagrams.LightBDD.xUnit2;
using TTD.LightBDD.xUnit2.Infrastructure;

[assembly: LightBddScope(typeof(ConfiguredLightBddScope))]

namespace TTD.LightBDD.xUnit2.Infrastructure;

public class ConfiguredLightBddScope : LightBddScope
{
    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        configuration.CreateStandardReportsWithDiagrams(new ReportConfigurationOptions
        {
            SpecificationsTitle = "SERVICE_NAME Specifications",
            SeparateSetup = true,
        });

        configuration.ProgressNotifierConfiguration().Clear();

        configuration.ExecutionExtensionsConfiguration()
            .RegisterGlobalTearDown("dispose factory", BaseFixture.DisposeFactory);
    }
}
