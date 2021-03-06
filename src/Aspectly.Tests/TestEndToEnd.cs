﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Aspectly.Tests
{
    public class TestEndToEnd
    {
        [Fact]
        public void returned_proxy_implements_expected_contract()
        {
            var sut = new ProxyFactory();
            var result = sut.CreateProxy<IFoo>(new FooImpl());

            Assert.IsAssignableFrom<IFoo>(result);
        }

        [Fact]
        public void inner_instance_is_invoked_as_expected()
        {
            var sut = new ProxyFactory();
            var spy = new FooSpy();

            var result = sut.CreateProxy<IFoo>(spy);

            result.Print();

            Assert.True(spy.WasInvoked);
        }

        [Fact]
        public void pre_hook_is_not_invoked_if_attribute_is_missing()
        {
            var wasInvoked = false;

            var sut = new ProxyFactory();

            var proxy = sut.CreateProxy<IFoo>(
                instance: new FooImpl(),
                hook: new InlineHook<NonExistingAttribute>(
                    onBefore: () => wasInvoked = true
                ));

            proxy.Print();

            Assert.False(wasInvoked);
        }

        [Fact]
        public void pre_hook_is_invoked_if_attribute_is_present()
        {
            var wasInvoked = false;

            var sut = new ProxyFactory();

            var proxy = sut.CreateProxy<IFoo>(
                instance: new FooImpl(),
                hook: new InlineHook<FooAttribute>(
                    onBefore: () => wasInvoked = true
                ));

            proxy.Print();

            Assert.True(wasInvoked);
        }

        [Fact]
        public void post_hook_is_not_invoked_if_attribute_is_missing()
        {
            var wasInvoked = false;

            var sut = new ProxyFactory();

            var proxy = sut.CreateProxy<IFoo>(
                instance: new FooImpl(),
                hook: new InlineHook<NonExistingAttribute>(
                    onAfter: () => wasInvoked = true
                ));

            proxy.Print();

            Assert.False(wasInvoked);
        }

        [Fact]
        public void post_hook_is_invoked_if_attribute_is_present()
        {
            var wasInvoked = false;

            var sut = new ProxyFactory();

            var proxy = sut.CreateProxy<IFoo>(
                instance: new FooImpl(),
                hook: new InlineHook<FooAttribute>(
                    onAfter: () => wasInvoked = true
                ));

            proxy.Print();

            Assert.True(wasInvoked);
        }

        [Fact]
        public void support_for_multiple_attributes()
        {
            var invocations = new LinkedList<string>();

            var sut = new ProxyFactory();

            var fooHook = new InlineHook<FooAttribute>(
                onBefore: () => invocations.AddLast("Foo:Before"),
                onAfter: () => invocations.AddLast("Foo:After")
            );

            var barHook = new InlineHook<BarAttribute>(
                onBefore: () => invocations.AddLast("Bar:Before"),
                onAfter: () => invocations.AddLast("Bar:After")
            );

            var proxy = sut.CreateProxy<IFoo>(
                instance: new MultipleAttributes(),
                hook: new IHook[] {fooHook, barHook}
            );

            proxy.Print();

            Assert.Equal(
                expected: new[] {"Foo:Before", "Bar:Before", "Foo:After", "Bar:After"},
                actual: invocations
            );
        }

        [Fact]
        public void support_for_multiple_hooks_on_same_attribute()
        {
            var invocations = new LinkedList<string>();

            var sut = new ProxyFactory();

            var firstHook = new InlineHook<FooAttribute>(
                onBefore: () => invocations.AddLast("First:Before"),
                onAfter: () => invocations.AddLast("First:After")
            );

            var secondHook = new InlineHook<FooAttribute>(
                onBefore: () => invocations.AddLast("Second:Before"),
                onAfter: () => invocations.AddLast("Second:After")
            );

            var proxy = sut.CreateProxy<IFoo>(
                instance: new FooImpl(),
                hook: new IHook[] {firstHook, secondHook}
            );

            proxy.Print();

            Assert.Equal(
                expected: new[] {"First:Before", "Second:Before", "First:After", "Second:After"},
                actual: invocations
            );
        }

        [Fact]
        public void proxy_returns_expected()
        {
            var expected = "foo bar";

            var sut = new ProxyFactory();

            var emptyHook = new InlineHook<BarAttribute>();
            var proxy = sut.CreateProxy<IBar>(new StubBar(expected), emptyHook);

            var result = proxy.Get();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task supports_methods_that_returns_a_task()
        {
            var expected = "foo bar";

            var sut = new ProxyFactory();

            var hook = new InlineHook<FooAttribute>();
            var proxy = sut.CreateProxy<IAsyncFoo>(new StubAsyncFoo(expected), hook);

            var result = await proxy.Print();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task tasks_are_awaited_before_the_before_hook_is_invoked()
        {
            var list = new LinkedList<string>();

            var sut = new ProxyFactory();

            var hook = new InlineHook<FooAttribute>(
                onBefore: () => list.AddLast("before"),
                onAfter: () => list.AddLast("after")
            );

            var temp = Task
                .Delay(1000)
                .ContinueWith(t => list.AddLast("proxy result"))
                .ContinueWith(t => "proxy result");

            var proxy = sut.CreateProxy<IAsyncFoo>(
                instance: new StubAsyncFoo(temp),
                hook: hook
            );

            var result = await proxy.Print();

            Assert.Equal(
                expected: new[] {"before", "proxy result", "after"},
                actual: list
            );
        }

        public interface IHook
        {
            Type Attribute { get; }
            void OnBefore();
            void OnAfter();
        }

        public abstract class Hook<T> : IHook where T : Attribute
        {
            public Type Attribute => typeof(T);

            public virtual void OnBefore()
            {
            }

            public virtual void OnAfter()
            {
            }
        }

        public class InlineHook<T> : Hook<T> where T : Attribute
        {
            private readonly Action _onBefore;
            private readonly Action _onAfter;

            public InlineHook(Action onBefore = null, Action onAfter = null)
            {
                _onBefore = onBefore ?? (() => { });
                _onAfter = onAfter ?? (() => { });
            }

            public override void OnBefore()
            {
                _onBefore();
            }

            public override void OnAfter()
            {
                _onAfter();
            }
        }

        public class ProxyFactory
        {
            public TProxy CreateProxy<TProxy>(TProxy instance, params IHook[] hook) where TProxy : class
            {
                var proxy = DispatchProxy.Create<TProxy, Interceptor>();
                var interceptor = proxy as Interceptor;
                interceptor?.SetTarget(instance, hook);

                return proxy;
            }
        }

        public class Interceptor : DispatchProxy
        {
            private object _target;
            private IHook[] _hook;

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                var map = _target.GetType().GetInterfaceMap(targetMethod.DeclaringType);
                var index = Array.IndexOf(map.InterfaceMethods, targetMethod);

                var implementationMethodInfo = map.TargetMethods[index];

                var annotations = implementationMethodInfo
                    .GetCustomAttributes()
                    .Select(annotation => annotation.GetType())
                    .ToList();

                var hooks = _hook
                    .Where(hook => annotations.Any(annotation => annotation == hook.Attribute))
                    .ToList();

                hooks.ForEach(x => x.OnBefore());

                var result = InvokeMethodOnTarget(targetMethod, args);

                hooks.ForEach(x => x.OnAfter());

                return result;
            }

            private object InvokeMethodOnTarget(MethodInfo targetMethod, object[] args)
            {
                var returnValue = targetMethod.Invoke(_target, args);                
                var task = returnValue as Task;

                if (task == null)
                {
                    return returnValue;
                }

                if (!task.GetType().IsGenericType)
                {
                    task.GetAwaiter().GetResult();
                    return Task.CompletedTask;
                }

                var realResult = GetResultFromGenericTask((dynamic) task);
                return Task.FromResult(realResult);
            }

            private T GetResultFromGenericTask<T>(Task<T> task)
            {
                return task.GetAwaiter().GetResult();
            }
            
            public void SetTarget(object target, IHook[] hook)
            {
                _hook = hook ?? new IHook[0];
                _target = target;
            }
        }

        public interface IAsyncFoo
        {
            Task<string> Print();
        }

        public class AsyncFooImpl : IAsyncFoo
        {
            [Foo]
            public Task<string> Print()
            {
                return Task.FromResult("foo");
            }
        }

        public class StubAsyncFoo : IAsyncFoo
        {
            private readonly Task<string> _resultTask;

            public StubAsyncFoo(string result) : this(Task.FromResult(result))
            {
            }

            public StubAsyncFoo(Task<string> resultTask)
            {
                _resultTask = resultTask;
            }

            [Foo]
            public Task<string> Print()
            {
                return _resultTask;
            }
        }

        public interface IFoo
        {
            void Print();
        }

        public class FooImpl : IFoo
        {
            [Foo]
            public void Print()
            {
            }
        }

        public class MultipleAttributes : IFoo
        {
            [Foo, Bar]
            public void Print()
            {
            }
        }

        public interface IBar
        {
            string Get();
        }

        public class StubBar : IBar
        {
            private readonly string _result;

            public StubBar(string result)
            {
                _result = result;
            }

            [Bar]
            public string Get()
            {
                return _result;
            }
        }

        public class FooAttribute : Attribute
        {
        }

        public class BarAttribute : Attribute
        {
        }

        public class NonExistingAttribute : Attribute
        {
        }

        public class FooSpy : IFoo
        {
            public bool WasInvoked { get; set; }

            public void Print()
            {
                WasInvoked = true;
            }
        }
    }
}