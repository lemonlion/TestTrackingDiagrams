using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

[CollectionDefinition("PendingLogs")]
public class PendingLogsCollection : ICollectionFixture<PendingLogsFixture>;

public class PendingLogsFixture : IDisposable
{
    public PendingLogsFixture() => PendingRequestResponseLogs.Clear();
    public void Dispose() => PendingRequestResponseLogs.Clear();
}
