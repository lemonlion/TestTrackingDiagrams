namespace Example.Api.Tests.Component.Shared.HttpFakes;

public static class InMemoryFakeHelper
{
    public static WebApplicationFactoryForSpecificUrl<TProgram> Create<TProgram>(string baseUrl) where TProgram : class
    {
        PortChecker.AssertPortIsNotInUse(baseUrl);
        var fixture = new WebApplicationFactoryForSpecificUrl<TProgram>(hostUrl: baseUrl);
        fixture.CreateDefaultClient();
        return fixture;
    }
}