using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aspectly;

internal class InterceptPipeline
{
    private readonly InterceptionContext _interceptionContext;
    private readonly Func<Task> _inner;

    public InterceptPipeline(InterceptionContext interceptionContext, Func<Task> inner)
    {
        _interceptionContext = interceptionContext;
        _inner = inner;
    }
    
    public async Task Execute(IEnumerable<PipelineStep> steps)
    {
        var temp = new LinkedList<PipelineStep>(steps);
        await ExecuteStep(temp.First);
    }
    
    private async Task ExecuteStep(LinkedListNode<PipelineStep>? current)
    {
        if (current is null)
        {
            await _inner().ConfigureAwait(false);
            return;
        }

        var next = current.Next;
        var step = current.Value;
        var context = new AspectContext(
            method: _interceptionContext.Method,
            triggerAttribute: step.TriggerAttribute
        );
        
        await step.Aspect
            .Invoke(context, () => ExecuteStep(next))
            .ConfigureAwait(false);
    }
}