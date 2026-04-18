namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public record SqlTrackingInterceptorOptions
{
    public string ServiceName { get; set; } = "Database";
    public string CallingServiceName { get; set; } = "Caller";
    public SqlTrackingVerbosity Verbosity { get; set; } = SqlTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
}
