using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Example.Api.Tests.Component.Shared.HttpFakes;

public class WebApplicationFactoryForSpecificUrl<T>(string hostUrl) : WebApplicationFactory<T> where T : class
{
    private string HostUrl { get; } = hostUrl;

    public static WebApplicationFactoryForSpecificUrl<T> Create(string baseUrl)
    {
        PortChecker.AssertPortIsNotInUse(baseUrl);
        var fixture = new WebApplicationFactoryForSpecificUrl<T>(hostUrl: baseUrl);
        _ = fixture.Services; // Trigger EnsureServer/CreateHost to start Kestrel without accessing TestServer.Application
        return fixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseUrls(HostUrl);

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var dummyHost = builder.Build();
        var realHost = builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel()).Build();
        realHost.Start();
        return dummyHost; // You need to return the unconfigured dummyhost otherwise you'll get a cast exception when calling WebApplicationFactory.CreateDefaultClient().
    }
}