using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Aspectly;

internal class MethodInterceptor : AsyncInterceptorBase
{
    private readonly AspectFactory _aspectFactory;

    public MethodInterceptor(AspectFactory aspectFactory)
    {
        _aspectFactory = aspectFactory;
    }
    
    protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        var aspects = _aspectFactory.GetAspectsFor(invocation.MethodInvocationTarget);
        var pipeline = new InterceptPipeline(() => proceed(invocation, proceedInfo));
        
        var context = new AspectContext();
        
        await pipeline.Execute(context, aspects);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        TResult? result = default;
        
        var aspects = _aspectFactory.GetAspectsFor(invocation.MethodInvocationTarget);
        var pipeline = new InterceptPipeline(async () =>
        {
            result = await proceed(invocation, proceedInfo);
        });
        
        await pipeline.Execute(new AspectContext(), aspects);

        return result!;
    }
}