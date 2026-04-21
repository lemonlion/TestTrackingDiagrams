namespace TestTrackingDiagrams;

public record DapperTrackingOptions
{
    public string ServiceName { get; set; } = "Database";
    public string CallingServiceName { get; set; } = "Caller";
    public DapperTrackingVerbosity Verbosity { get; set; } = DapperTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)?>? CurrentTestInfoFetcher { get; set; }
    public bool LogParameters { get; set; }
    public bool LogSqlText { get; set; } = true;
    public HashSet<DapperOperation> ExcludedOperations { get; set; } = [];
}
