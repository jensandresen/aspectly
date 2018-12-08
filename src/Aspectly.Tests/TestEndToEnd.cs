using System;
using System.Reflection;
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
                hooks: new InlineHooks<NonExistingAttribute>(
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
                hooks: new InlineHooks<FooAttribute>(
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
                hooks: new InlineHooks<NonExistingAttribute>(
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
                hooks: new InlineHooks<FooAttribute>(
                    onAfter: () => wasInvoked = true
                ));
            
            proxy.Print();
            
            Assert.True(wasInvoked);
        }

        public interface IHooks
        {
            Type Attribute { get; }
            void OnBefore();
            void OnAfter();
        }
        
        public class NullHook : IHooks
        {
            public Type Attribute => typeof(Attribute);
            
            public void OnBefore()
            {
                
            }

            public void OnAfter()
            {
                
            }
        }

        public abstract class Hooks<T> : IHooks where T : Attribute
        {            
            public Type Attribute => typeof(T);
            
            public virtual void OnBefore()
            {
                
            }

            public virtual void OnAfter()
            {
                
            }
        }

        public class InlineHooks<T> : Hooks<T> where T : Attribute
        {
            private readonly Action _onBefore;
            private readonly Action _onAfter;

            public InlineHooks(Action onBefore = null, Action onAfter = null)
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
            public TProxy CreateProxy<TProxy>(TProxy instance, IHooks hooks = null) where TProxy : class
            {
                var proxy = DispatchProxy.Create<TProxy, Interceptor>();
                var interceptor = proxy as Interceptor;
                interceptor?.SetTarget(instance, hooks ?? new NullHook());
                
                return proxy;
            }
        }
        
        public class Interceptor : DispatchProxy
        {
            private object _target;
            private IHooks _hooks;

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                var map = _target.GetType().GetInterfaceMap(targetMethod.DeclaringType);
                var index = Array.IndexOf(map.InterfaceMethods, targetMethod);
                
                var implementationMethodInfo = map.TargetMethods[index];

                var annotation = implementationMethodInfo.GetCustomAttribute(_hooks.Attribute);

                if (annotation != null)
                {
                    _hooks.OnBefore();
                }
                
                var result = targetMethod.Invoke(_target, args);

                if (annotation != null)
                {
                    _hooks.OnAfter();
                }
                
                return result;
            }

            public void SetTarget(object target, IHooks hooks)
            {
                _hooks = hooks;
                _target = target;
            }
        }

        public class InterceptionOptions
        {
            
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

        public class FooAttribute : Attribute
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
