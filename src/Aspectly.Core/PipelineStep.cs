using System;

namespace Aspectly;

internal class PipelineStep
{
    public PipelineStep(IAspect aspect, Attribute triggerAttribute)
    {
        Aspect = aspect;
        TriggerAttribute = triggerAttribute;
    }

    public IAspect Aspect { get; private set; }
    public Attribute TriggerAttribute { get; private set; }
}