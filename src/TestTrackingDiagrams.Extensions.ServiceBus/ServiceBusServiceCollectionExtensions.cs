using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.ServiceBus;

public static class ServiceBusServiceCollectionExtensions
{
    /// <summary>
    /// Replaces any existing <see cref="ServiceBusClient"/> registration with a
    /// <see cref="TrackingServiceBusClient"/> that logs all send/receive operations
    /// to the test tracking system.
    /// </summary>
    public static IServiceCollection AddServiceBusTestTracking(
        this IServiceCollection services,
        ServiceBusTrackingOptions options)
    {
        services.AddSingleton(options);

        // If a ServiceBusClient is registered, wrap it
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ServiceBusClient));
        if (existingDescriptor is not null)
        {
            services.Remove(existingDescriptor);
            services.AddSingleton(sp =>
            {
                ServiceBusClient innerClient;
                if (existingDescriptor.ImplementationFactory is not null)
                    innerClient = (ServiceBusClient)existingDescriptor.ImplementationFactory(sp);
                else if (existingDescriptor.ImplementationInstance is not null)
                    innerClient = (ServiceBusClient)existingDescriptor.ImplementationInstance;
                else
                    innerClient = (ServiceBusClient)ActivatorUtilities.CreateInstance(sp, existingDescriptor.ImplementationType!);

                options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
                return new TrackingServiceBusClient(innerClient, options);
            });
        }
        else
        {
            // No existing registration; register TrackingServiceBusClient only
            // Consumer must provide ServiceBusClient separately or use the TrackingServiceBusClient ctor
        }

        return services;
    }
}
