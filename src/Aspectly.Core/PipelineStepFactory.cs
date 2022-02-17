using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Aspectly;

internal class PipelineStepFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AspectRegistry _aspectRegistry;

    public PipelineStepFactory(IServiceProvider serviceProvider, AspectRegistry aspectRegistry)
    {
        _serviceProvider = serviceProvider;
        _aspectRegistry = aspectRegistry;
    }

    public AnnotatedServiceRegistration? GetRegistrationFor(MethodInfo method)
    {
        return _aspectRegistry.GetAspectRegistrationFor(method);
    }

    public IEnumerable<PipelineStep> GetPipelineStepsFor(MethodInfo method)
    {
        var registration = GetRegistrationFor(method);
        
        if (registration is null)
        {
            return Enumerable.Empty<PipelineStep>();
        }

        return registration.Duno
            .Select(x => new PipelineStep(
                aspect: CreateAspect(x.AspectType),
                triggerAttribute: x.Attribute
            ));
    }

    public IAspect CreateAspect(Type aspectType) => (IAspect)_serviceProvider.GetRequiredService(aspectType);
}