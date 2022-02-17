using System;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Aspectly;

internal class InterceptionContext
{
    public InterceptionContext(MethodInfo method)
    {
        Method = method;
    }

    public MethodInfo Method { get; }
}

internal class MethodInterceptor : AsyncInterceptorBase
{
    private readonly PipelineStepFactory _pipelineStepFactory;

    public MethodInterceptor(PipelineStepFactory pipelineStepFactory)
    {
        _pipelineStepFactory = pipelineStepFactory;
    }
    
    protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        var interceptedMethod = invocation.MethodInvocationTarget;
        var interceptionContext = new InterceptionContext(interceptedMethod);

        var pipeline = new InterceptPipeline(
            interceptionContext: interceptionContext,
            inner: () => proceed(invocation, proceedInfo)
        );
        
        var steps = _pipelineStepFactory.GetPipelineStepsFor(interceptedMethod);
        await pipeline.Execute(steps);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        var interceptedMethod = invocation.MethodInvocationTarget;
        var interceptionContext = new InterceptionContext(interceptedMethod);

        TResult? result = default;
        
        var pipeline = new InterceptPipeline(
            interceptionContext: interceptionContext,
            inner: async () =>
            {
                result = await proceed(invocation, proceedInfo);
            });

        var steps = _pipelineStepFactory.GetPipelineStepsFor(interceptedMethod);
        await pipeline.Execute(steps);

        return result!;
    }
}