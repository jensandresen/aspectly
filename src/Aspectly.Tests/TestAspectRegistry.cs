using System;
using System.Threading.Tasks;
using Aspectly.Tests.Builders;
using Aspectly.Tests.TestDoubles;
using Xunit;

namespace Aspectly.Tests;

public class TestAspectRegistry
{
    [Fact]
    public void returns_expected_registered_trigger_attribute_types_when_initialized()
    {
        var sut = new AspectRegistryBuilder().Build();
        Assert.Empty(sut.RegisteredTriggerAttributeTypes);
    }

    [Fact]
    public void returns_expected_registered_aspect_types_when_initialized()
    {
        var sut = new AspectRegistryBuilder().Build();
        Assert.Empty(sut.RegisteredAspectTypes);
    }

    [Fact]
    public void returns_expected_registered_trigger_attribute_types_when_registering_single()
    {
        var sut = new AspectRegistryBuilder().Build();
        sut.RegisterAspectTrigger(typeof(TriggerAttribute), Dummy.Of<IAspect>().GetType());

        Assert.Equal(
            expected: new[] { typeof(TriggerAttribute) },
            actual: sut.RegisteredTriggerAttributeTypes
        );
    }

    [Fact]
    public void returns_expected_aspect_types_when_registering_single()
    {
        var sut = new AspectRegistryBuilder().Build();

        var dummy = typeof(Attribute);
        sut.RegisterAspectTrigger(dummy, typeof(StubAspect));

        Assert.Equal(
            expected: new[] { typeof(StubAspect) },
            actual: sut.RegisteredAspectTypes
        );
    }

    [Fact]
    public void returns_expected_registered_trigger_attribute_types_when_registering_multiple()
    {
        var sut = new AspectRegistryBuilder().Build();
        sut.RegisterAspectTrigger(typeof(TriggerAttribute), Dummy.Of<IAspect>().GetType());
        sut.RegisterAspectTrigger(typeof(AnotherTriggerAttribute), Dummy.Of<IAspect>().GetType());

        Assert.Equal(
            expected: new[] { typeof(TriggerAttribute), typeof(AnotherTriggerAttribute) },
            actual: sut.RegisteredTriggerAttributeTypes
        );
    }

    [Fact]
    public void has_triggers_returns_expected_when_nothing_has_been_registered()
    {
        var sut = new AspectRegistryBuilder().Build();
        var result = sut.HasTriggers(typeof(Target));
        
        Assert.False(result);
    }
    
    [Fact]
    public void has_triggers_returns_expected_when_a_different_trigger_has_been_registered()
    {
        var sut = new AspectRegistryBuilder().Build();
        sut.RegisterAspectTrigger(typeof(AnotherTriggerAttribute), typeof(StubAspect));
        var result = sut.HasTriggers(typeof(Target));
        
        Assert.False(result);
    }
    
    [Fact]
    public void has_triggers_returns_expected_when_trigger_has_been_registered_but_target_has_no_triggers()
    {
        var sut = new AspectRegistryBuilder().Build();
        sut.RegisterAspectTrigger(typeof(TriggerAttribute), typeof(StubAspect));
        var result = sut.HasTriggers(typeof(EmptyTarget));
        
        Assert.False(result);
    }
    
    [Fact]
    public void has_triggers_returns_expected_when_trigger_has_been_registered()
    {
        var sut = new AspectRegistryBuilder().Build();
        sut.RegisterAspectTrigger(typeof(TriggerAttribute), typeof(StubAspect));
        var result = sut.HasTriggers(typeof(Target));
        
        Assert.True(result);
    }

    [Fact]
    public void throws_expected_exception_when_registering_trigger_that_is_not_an_attribute()
    {
        var sut = new AspectRegistryBuilder().Build();
        var invalidAttributeType = typeof(object);

        Assert.Throws<InvalidTriggerTypeException>(() => sut.RegisterAspectTrigger(invalidAttributeType, typeof(StubAspect)));
    }
    
    [Fact]
    public void throws_expected_exception_when_registering_invalid_aspect()
    {
        var sut = new AspectRegistryBuilder().Build();
        var invalidAspectType = typeof(object);
        
        Assert.Throws<InvalidAspectTypeException>(() => sut.RegisterAspectTrigger(typeof(Attribute), invalidAspectType));
    }
    
    // test: disallow same trigger to be registered with different aspects
    // test: allow same aspect to be registered with different triggers
    
    #region private helper classes

    public class TriggerAttribute : Attribute { }
    public class AnotherTriggerAttribute : Attribute { }
    
    public class StubAspect : IAspect
    {
        public Task Invoke(AspectContext context, AspectDelegate next) => next();
    }

    public class Target
    {
        [Trigger]
        public Task Execute() => Task.CompletedTask;
    }

    public class AnotherTarget
    {
        [AnotherTrigger]
        public Task Execute() => Task.CompletedTask;
    }

    public class EmptyTarget
    {
        public Task Execute() => Task.CompletedTask;
    }

    #endregion
}