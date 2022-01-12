using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Aspectly.Tests
{
    public class TestGenericTargetStrategy
    {
        private MethodInfo MethodSelector<T>(T instance)
        {
            return instance
                .GetType()
                .GetMethod("MethodToExecute");
        }
        
        [Fact]
        public async Task invokes_target_method()
        {
            var wasInvoked = false;

            var spy = new TargetSpy(() => wasInvoked = true);
            var sut = new GenericTargetStrategy(
                    hooks: new TestEndToEnd.IHook[0],
                    targetInstance: spy,
                    targetMethod: MethodSelector(spy),
                    args: new object[0]
                );

            await sut.Execute();
            
            Assert.True(wasInvoked);
        }

        [Fact]
        public async Task returns_expected_value_from_invoked_target_method()
        {
            var expectedReturnValue = "foo";
            
            var spy = new TargetSpy(returnValue: expectedReturnValue);
            var sut = new GenericTargetStrategy(
                hooks: new TestEndToEnd.IHook[0],
                targetInstance: spy,
                targetMethod: MethodSelector(spy),
                args: new object[0]
            );

            var result = await (Task<string>)sut.Execute();

            Assert.Equal(expectedReturnValue, result);
        }

        [Fact]
        public async Task before_hooks_are_executed()
        {
            var wasInvoked = false;
            
            var stubTarget = new TargetSpy();

            var sut = new GenericTargetStrategy(
                hooks: new TestEndToEnd.IHook[]
                {
                    new SpyHook(
                        beforeFactory: () => Task.CompletedTask.ContinueWith(t => wasInvoked = true)
                    ),
                },
                targetInstance: stubTarget,
                targetMethod: MethodSelector(stubTarget),
                args: new object[0]
            );

            await sut.Execute();
            
            Assert.True(wasInvoked);
        }

        public class SpyHook : TestEndToEnd.IHook
        {
            private readonly Func<Task> _beforeFactory;
            private readonly Func<Task> _afterFactory;

            public SpyHook(Func<Task> beforeFactory = null, Func<Task> afterFactory = null)
            {
                _beforeFactory = beforeFactory ?? (() => Task.CompletedTask);
                _afterFactory = afterFactory ?? (() => Task.CompletedTask);
            }
            
            public Type Attribute => typeof(Attribute);
            
            public Task OnBefore()
            {
                return _beforeFactory();
            }

            public Task OnAfter()
            {
                return _afterFactory();
            }
        }

        private class TargetSpy
        {
            private readonly Func<Task<string>> _taskFactory;

            public TargetSpy(Action callback = null, string returnValue = "hello world")
                : this(Helper(callback, returnValue))
            {
                
            }

            private static Func<Task<string>> Helper(Action callback, string returnValue)
            {
                return () => Task.Run(() =>
                {
                    callback?.Invoke();
                    return returnValue;
                });
            }
            
            public TargetSpy(Func<Task<string>> taskFactory)
            {
                _taskFactory = taskFactory;
            }
            
            public Task<string> MethodToExecute()
            {
                return _taskFactory();
            }
        }
    }

    public class GenericTargetStrategy
    {
        private readonly TestEndToEnd.IHook[] _hooks;
        private readonly object _targetInstance;
        private readonly MethodInfo _targetMethod;
        private readonly object[] _args;

        public GenericTargetStrategy(TestEndToEnd.IHook[] hooks, object targetInstance, MethodInfo targetMethod, object[] args)
        {
            _hooks = hooks;
            _targetInstance = targetInstance;
            _targetMethod = targetMethod;
            _args = args;
        }
        
        public Task Execute()
        {
            return Task
                .WhenAll(_hooks.Select(x => x.OnBefore()))
                .ContinueWith(prevTask =>
                {
                    Task<string> lala; lala.Result
                    var returnedTask = _targetMethod.Invoke(_targetInstance, _args);
                    return returnedTask;
                })
                .ContinueWith(async prevTask =>
                {
                    var task = prevTask.Result as Task;
                    await task;

                    return GrabResult((dynamic) task);
                });
        }

        private T GrabResult<T>(Task<T> task)
        {
            return task.Result;
        }
        
        private async Task<T> MagicAwait<T>(Task<T> task)
        {
            return await task;
        }
    }
}