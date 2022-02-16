// ReSharper disable once CheckNamespace

using System;
using System.Linq;
using Aspectly;
using Castle.DynamicProxy;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

    private static bool IsSupportedServiceDescriptor(ServiceDescriptor serviceDescriptor)
    {
        if (serviceDescriptor.ImplementationFactory != null)
        {
            return false;
        }

        if (serviceDescriptor.ImplementationInstance != null)
        {
            return false;
        }

        if (!serviceDescriptor.ServiceType.IsInterface)
        {
            return false;
        }
        
        if (serviceDescriptor.ImplementationType == null)
        {
            return false;
        }

        return true;
    }

    public static void RewireWithAspects(this IServiceCollection services, Action<IAspectOptions>? options = null)
    {
        var aspectRegistry = new AspectRegistry();
        services.AddSingleton(aspectRegistry);

        var optionsBuilder = new AspectOptionsBuilder();
        options?.Invoke(optionsBuilder);
        
        foreach (var map in optionsBuilder.Maps)
        {
            aspectRegistry.RegisterAspectTrigger(map.Attribute, map.Aspect);
        }
        
        foreach (var aspectType in aspectRegistry.RegisteredAspectTypes)
        {
            services.AddTransient(aspectType);
        }
        
        var descriptorsOfAnnotatedServices = (ServiceDescriptor[])services
            .Where(IsSupportedServiceDescriptor)
            .Where(x => aspectRegistry.HasTriggers(x.ImplementationType!))
            .ToArray();
        
        foreach (var serviceDescriptor in descriptorsOfAnnotatedServices)
        {
            aspectRegistry.RegisterAnnotatedService(serviceDescriptor.ImplementationType!);
        }
        
        // remove current service registrations
        foreach (var serviceDescriptor in descriptorsOfAnnotatedServices)
        {
            services.Remove(serviceDescriptor);
        }
        
        // add service descriptor for implementation types
        foreach (var serviceDescriptor in descriptorsOfAnnotatedServices)
        {
            services.Add(ServiceDescriptor.Describe(
                serviceType: serviceDescriptor.ImplementationType!,
                implementationType: serviceDescriptor.ImplementationType!,
                lifetime: serviceDescriptor.Lifetime
            ));
        }
        
        // add new service descriptor with proxy factory
        foreach (var serviceDescriptor in descriptorsOfAnnotatedServices)
        {
            var implementationType = serviceDescriptor.ImplementationType!;
            var lifetime = serviceDescriptor.Lifetime;
            var serviceType = serviceDescriptor.ServiceType;
            
            services.Add(ServiceDescriptor.Describe(
                serviceType: serviceType,
                provider =>
                {
                    var myClass = provider.GetRequiredService(implementationType);
                    var interceptor = new MethodInterceptor(new AspectFactory(provider, aspectRegistry));

                    return _proxyGenerator.CreateInterfaceProxyWithTargetInterface(
                        interfaceToProxy: serviceType,
                        target: myClass,
                        new IAsyncInterceptor[] { interceptor }
                    );
                },
                lifetime
            ));
        }
    }
}