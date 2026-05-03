using TestStack.BDDfy.Configuration;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

/// <summary>
/// Configures BDDfy to integrate with TestTrackingDiagrams by disabling built-in HTML reporting and registering diagram-capturing processors.
/// </summary>
public static class BDDfyDiagramsConfigurator
{
    private static bool _configured;

    public static void Configure(DiagramsFetcherOptions? fetcherOptions = null)
    {
        if (_configured) return;
        _configured = true;

        // Enable Track.That assertions to resolve the current xUnit v3 test ID.
        Track.TestIdResolver ??= () => Xunit.TestContext.Current.Test?.UniqueID;

        // Disable BDDfy's built-in HTML reporter to prevent crashes when scenarios
        // have duplicate titles (e.g. .BDDfy(nameof(ClassName))). Our batch processor
        // generates the report after fixing scenario titles.
        Configurator.BatchProcessors.HtmlReport.Disable();

        Configurator.Processors.Add(() => new DiagramCapturingProcessor());
        Configurator.BatchProcessors.Add(new DiagramEnhancingBatchProcessor(fetcherOptions));
        Configurator.StepExecutor = new BDDfyStepTrackingExecutor(Configurator.StepExecutor);
    }
}