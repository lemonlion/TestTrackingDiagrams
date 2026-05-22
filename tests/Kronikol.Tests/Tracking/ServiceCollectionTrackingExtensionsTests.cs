using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Kronikol.Extensions.DispatchProxy;
using Kronikol.InternalFlow;
using Kronikol.Tracking;

namespace Kronikol.Tests.Tracking;

[Collection("PendingLogs")]
public class ServiceCollectionTrackingExtensionsTests
{
    private readonly string _testId = Guid.NewGuid().ToString();
    private const string TestName = "ServiceCollectionTrackingExtensionsTest";

    private RequestResponseLog[] GetLogsForTest()
        => RequestResponseLogger.RequestAndResponseLogs.Where(l => l.TestId == _testId).ToArray();

    [Fact]
    public void Simplified_overload_passes_dependency_category_through()
    {
        var services = new ServiceCollection();
        var implementation = new FakeService();
        services.AddSingleton<IFakeService>(implementation);

        services.ReplaceWithTracked<IFakeService>(
            implementation,
            "SmtpRelay",
            testInfoFetcher: () => (TestName, _testId),
            dependencyCategory: "Email");

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IFakeService>();
        resolved.DoWork();

        var logs = GetLogsForTest();
        Assert.Equal(2, logs.Length);
        Assert.All(logs, l => Assert.Equal("Email", l.DependencyCategory));
    }

    [Fact]
    public void Simplified_overload_dependency_category_defaults_to_null()
    {
        var services = new ServiceCollection();
        var implementation = new FakeService();
        services.AddSingleton<IFakeService>(implementation);

        services.ReplaceWithTracked<IFakeService>(
            implementation,
            "Calculator",
            testInfoFetcher: () => (TestName, _testId));

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IFakeService>();
        resolved.DoWork();

        var logs = GetLogsForTest();
        Assert.Equal(2, logs.Length);
        Assert.All(logs, l => Assert.Null(l.DependencyCategory));
    }

    [Fact]
    public void AddTestTrackingContextPropagation_called_twice_registers_only_one_startup_filter()
    {
        var services = new ServiceCollection();

        services.AddTestTrackingContextPropagation();
        services.AddTestTrackingContextPropagation();

        var count = services.Count(s =>
            s.ServiceType == typeof(IStartupFilter) &&
            s.ImplementationType == typeof(TestTrackingContextStartupFilter));
        Assert.Equal(1, count);
    }

    public interface IFakeService
    {
        string DoWork();
    }

    private class FakeService : IFakeService
    {
        public string DoWork() => "done";
    }
}
