using Microsoft.Extensions.Configuration;

namespace Example.Api.Tests.Component.Shared;

public static class ConfigurationBuilderExtensions
{
    public static ComponentTestSettings GetComponentTestSettings(this IConfigurationBuilder builder)
    {
        return builder.GetComponentTestConfiguration().Get<ComponentTestSettings>()!;
    }

    public static IConfiguration GetComponentTestConfiguration(this IConfigurationBuilder builder)
    {
        return builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.componenttests.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
}