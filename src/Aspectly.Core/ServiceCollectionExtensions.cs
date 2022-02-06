// ReSharper disable once CheckNamespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aspectly.Core;
using Castle.DynamicProxy;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

    private static ServiceDescriptor[] GetAnnotatedServices(IServiceCollection services, TriggerFinder triggerFinder)
    {
        var list = new LinkedList<ServiceDescriptor>();

        foreach (var serviceDescriptor in services)
        {
            if (serviceDescriptor.ImplementationFactory != null)
            {
                continue;
            }

            if (serviceDescriptor.ImplementationInstance != null)
            {
                continue;
            }
            
            if (serviceDescriptor.ImplementationType == null)
            {
                continue;
            }

            if (triggerFinder.GetTargetsOf(serviceDescriptor.ImplementationType!).Any())
            {
                list.AddLast(serviceDescriptor);
            }
        }

        return list.ToArray();
    }

    private static TriggerFinder CreateTriggerFinder(IEnumerable<AttributeAspectMap> attributeAspectMaps)
    {
        var triggerAttributes = attributeAspectMaps
            .Select(x => x.Attribute)
            .ToArray();

        return new TriggerFinder(triggerAttributes);
    }

    public static void AddAspects(this IServiceCollection services, Action<IAspectOptions>? options = null)
    {
        var optionsBuilder = new AspectOptionsBuilder(services);
        options?.Invoke(optionsBuilder);
        
        foreach (var map in optionsBuilder.Maps)
        {
            services.AddTransient(map.Aspect);
        }

        var triggerFinder = CreateTriggerFinder(optionsBuilder.Maps);
        var descriptors = GetAnnotatedServices(services, triggerFinder).ToArray();
        
        foreach (var serviceDescriptor in descriptors)
        {
            if (!serviceDescriptor.ServiceType.IsInterface)
            {
                continue;
            }
            
            services.Remove(serviceDescriptor);
            services.Add(ServiceDescriptor.Describe(
                serviceType: serviceDescriptor.ImplementationType!,
                implementationType: serviceDescriptor.ImplementationType!,
                lifetime: serviceDescriptor.Lifetime
            ));

            var triggerTargets = triggerFinder
                .GetTargetsOf(serviceDescriptor.ImplementationType!)
                .ToArray();

            services.Add(ServiceDescriptor.Describe(
                serviceType: serviceDescriptor.ServiceType,
                provider =>
                {
                    var myClass = provider.GetRequiredService(serviceDescriptor.ImplementationType!);

                    var temp = triggerTargets
                        .Select(x =>
                        {
                            return new TriggerTargetWithFactory(
                                hostType: x.HostType,
                                triggerAttribute: x.TriggerAttribute,
                                method: x.Method,
                                aspectFactory: requestedType => (IAsyncAspect)provider.GetRequiredService(requestedType)
                            );
                        })
                        .ToArray();
                    
                    var interceptor = new InternalInterceptor(temp, attribute =>
                    {
                        var map = optionsBuilder.Maps.Single(x => x.Attribute == attribute.GetType());
                        return (IAsyncAspect)provider.GetRequiredService(map.Aspect);
                    });

                    return _proxyGenerator.CreateInterfaceProxyWithTargetInterface(
                        interfaceToProxy: serviceDescriptor.ServiceType,
                        target: myClass,
                        new IAsyncInterceptor[] { interceptor }
                    );
                },
                serviceDescriptor.Lifetime
            ));
        }
    }
}

public class TriggerTargetWithFactory : TriggerFinder.TriggerTarget
{
    public TriggerTargetWithFactory(Type hostType, Attribute triggerAttribute, MethodInfo method, Func<Type, IAsyncAspect> aspectFactory) : base(hostType, triggerAttribute, method)
    {
        AspectFactory = aspectFactory;
    }

    public Func<Type, IAsyncAspect> AspectFactory { get; }
}

public interface IAspectOptions
{
    void Register<TAttribute, TAspect>()
        where TAttribute : Attribute
        where TAspect : class, IAsyncAspect;
}

public class AspectOptionsBuilder : IAspectOptions
{
    private readonly IServiceCollection _services;
    private readonly LinkedList<AttributeAspectMap> _maps = new LinkedList<AttributeAspectMap>();

    public AspectOptionsBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public void Register<TAttribute, TAspect>() where TAttribute : Attribute where TAspect : class, IAsyncAspect
    {
        _maps.AddLast(new AttributeAspectMap(typeof(TAttribute), typeof(TAspect)));
    }

    public IEnumerable<AttributeAspectMap> Maps => _maps;
}

public class AttributeAspectMap
{
    public AttributeAspectMap(Type attribute, Type aspect)
    {
        Attribute = attribute;
        Aspect = aspect;
    }

    public Type Attribute { get; private set; }
    public Type Aspect { get; private set; }
}