using TestStack.BDDfy.Configuration;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public static class BDDfyDiagramsConfigurator
{
    private static bool _configured;

    public static void Configure(DiagramsFetcherOptions? fetcherOptions = null)
    {
        if (_configured) return;
        _configured = true;

        Configurator.Processors.Add(() => new DiagramCapturingProcessor());
        Configurator.BatchProcessors.Add(new DiagramEnhancingBatchProcessor(fetcherOptions));
    }
}
