using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.MediatR;

public static class MediatorTrackingExtensions
{
    /// <summary>
    /// Replaces the <see cref="IMediator"/> and <see cref="ISender"/> registrations with
    /// tracking proxies that record Send, Publish, and CreateStream calls for diagrams.
    /// </summary>
    public static IServiceCollection TrackMediatorForDiagrams(
        this IServiceCollection services,
        MediatorTrackingOptions options)
    {
        var proxyOptions = new TrackingProxyOptions
        {
            ServiceName = options.ServiceName,
            CallingServiceName = options.CallingServiceName,
            ActivitySourceName = options.ActivitySourceName,
            LogMode = options.LogMode,
            CurrentTestInfoFetcher = options.CurrentTestInfoFetcher,
            UriScheme = "mock",
            SerializerOptions = options.SerializerOptions,
            TrackDuringSetup = options.TrackDuringSetup,
            TrackDuringAction = options.TrackDuringAction
        };

        // Store the options so we can resolve the real mediator later and wrap it
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        if (descriptor is null) return services;

        services.AddSingleton(new MediatorTrackingRegistration(proxyOptions, descriptor));

        // Replace IMediator
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(IMediator))
                services.RemoveAt(i);
        }

        services.AddSingleton<IMediator>(sp =>
        {
            var reg = sp.GetRequiredService<MediatorTrackingRegistration>();
            var realMediator = ResolveInstance<IMediator>(sp, reg.OriginalDescriptor);
            var opts = reg.ProxyOptions with { HttpContextAccessor = sp.GetService<IHttpContextAccessor>() };
            return TrackingProxy<IMediator>.Create(realMediator, opts);
        });

        // Also replace ISender if registered separately
        var senderDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISender));
        if (senderDescriptor is not null)
        {
            for (var i = services.Count - 1; i >= 0; i--)
            {
                if (services[i].ServiceType == typeof(ISender))
                    services.RemoveAt(i);
            }

            services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());
        }

        return services;
    }

    private static T ResolveInstance<T>(IServiceProvider sp, ServiceDescriptor descriptor) where T : class
    {
        if (descriptor.ImplementationInstance is T instance)
            return instance;
        if (descriptor.ImplementationFactory is not null)
            return (T)descriptor.ImplementationFactory(sp);
        if (descriptor.ImplementationType is not null)
            return (T)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
        throw new InvalidOperationException($"Cannot resolve {typeof(T).Name} from the original ServiceDescriptor.");
    }

    private record MediatorTrackingRegistration(
        TrackingProxyOptions ProxyOptions,
        ServiceDescriptor OriginalDescriptor);
}
