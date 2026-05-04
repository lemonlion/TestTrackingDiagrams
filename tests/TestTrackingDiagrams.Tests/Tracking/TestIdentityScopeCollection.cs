using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

[CollectionDefinition("TestIdentityScope")]
public class TestIdentityScopeCollection : ICollectionFixture<TestIdentityScopeFixture>;

public class TestIdentityScopeFixture : IDisposable
{
    public TestIdentityScopeFixture()
    {
        TestIdentityScope.Reset();
        TestIdentityScope.ClearGlobalFallback();
    }

    public void Dispose()
    {
        TestIdentityScope.Reset();
        TestIdentityScope.ClearGlobalFallback();
    }
}
