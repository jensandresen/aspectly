using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Aspectly;

internal class AspectFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AspectRegistry _aspectRegistry;

    public AspectFactory(IServiceProvider serviceProvider, AspectRegistry aspectRegistry)
    {
        _serviceProvider = serviceProvider;
        _aspectRegistry = aspectRegistry;
    }
    
    public IEnumerable<IAspect> GetAspectsFor(MethodInfo method)
    {
        return _aspectRegistry
            .GetAspectTypesFor(method) 
            .Select(x => (IAspect)_serviceProvider.GetRequiredService(x));
    }
}