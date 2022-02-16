using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aspectly;

internal class InterceptPipeline
{
    private readonly Func<Task> _inner;

    public InterceptPipeline(Func<Task> inner)
    {
        _inner = inner;
    }
    
    public async Task Execute(AspectContext context, IEnumerable<IAspect> aspects)
    {
        var temp = new LinkedList<IAspect>(aspects);
        await InnerExecute(context, temp.First);
    }
    
    private async Task InnerExecute(AspectContext context, LinkedListNode<IAspect>? current)
    {
        if (current is null)
        {
            await _inner().ConfigureAwait(false);
            return;
        }

        var next = current.Next;
        await current.Value
            .Invoke(context, () => InnerExecute(context, next))
            .ConfigureAwait(false);
    }
}