using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Aspectly.Core;

public interface IAsyncAspect
{
    Task Before();
    Task After();
}

public class InternalInterceptor : IAsyncInterceptor
{
    private readonly IEnumerable<TriggerTargetWithFactory> _triggerTargets;
    private readonly Func<Attribute, IAsyncAspect> _factory;

    public InternalInterceptor(IEnumerable<TriggerTargetWithFactory> triggerTargets, Func<Attribute, IAsyncAspect> factory)
    {
        _triggerTargets = triggerTargets;
        _factory = factory;
    }

    public void InterceptSynchronous(IInvocation invocation)
    {
        // Step 1. Do something prior to invocation.

        invocation.Proceed();

        // Step 2. Do something after invocation.
    }

    public void InterceptAsynchronous(IInvocation invocation)
    {
        invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
    }

    private async Task InternalInterceptAsynchronous(IInvocation invocation)
    {
        // Step 1. Do something prior to invocation.

        IAsyncAspect? aspect = null;
        
        var target = _triggerTargets.SingleOrDefault(x => x.Method == invocation.MethodInvocationTarget);
        if (target != null)
        {
            aspect = _factory(target.TriggerAttribute);
        }

        if (aspect is not null)
        {
            await aspect.Before().ConfigureAwait(false);
        }
        
        invocation.Proceed();
        var task = (Task)invocation.ReturnValue;
        await task;

        // Step 2. Do something after invocation.
        if (aspect is not null)
        {
            await aspect.After();
        }
        
    }
    
    public void InterceptAsynchronous<TResult>(IInvocation invocation)
    {
        invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
    }

    private async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
    {
        // Step 1. Do something prior to invocation.

        invocation.Proceed();
        var task = (Task<TResult>)invocation.ReturnValue;
        TResult result = await task;

        // Step 2. Do something after invocation.

        return result;
    }
}