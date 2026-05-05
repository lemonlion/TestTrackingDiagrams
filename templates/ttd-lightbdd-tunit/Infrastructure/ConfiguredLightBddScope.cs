using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using LightBDD.TUnit;
using TestTrackingDiagrams;
using TestTrackingDiagrams.LightBDD.TUnit;
using TTD.LightBDD.TUnit.Infrastructure;

namespace TTD.LightBDD.TUnit.Infrastructure;

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
