namespace TTD.BDDfy.xUnit3.Infrastructure;

public abstract class BaseFixture : IDisposable
{
    protected HttpClient Client { get; }

    protected BaseFixture()
    {
        Client = BDDfyTestSetup.CreateTrackingClient();
    }

    public void Dispose() => Client.Dispose();
}
