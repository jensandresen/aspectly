using System;
using System.Linq;
using System.Threading.Tasks;
using Aspectly.Core;
using Xunit;

namespace Aspectly.Tests;

public class TestTriggerFinder
{
    [Fact]
    public void returns_expected_method_when_annotated()
    {
        var triggers = new[] { typeof(FooTriggerAttribute) };
        var sut = new TriggerFinder(triggers);

        var result = sut.GetTargetsOf<FooTarget>();
        
        Assert.Equal(
                expected: new[]{"Target"},
                actual: result.Select(x => x.Method.Name)
            );
    }

    [Fact]
    public void returns_expected_method_when_not_annotated()
    {
        var triggers = new[] { typeof(BarTriggerAttribute) };
        var sut = new TriggerFinder(triggers);

        var result = sut.GetTargetsOf<FooTarget>();
        
        Assert.Empty(result);
    }

    [Fact]
    public void returns_expected_when_same_method_is_annotated_multiple_times()
    {
        var triggers = new[] { typeof(FooTriggerAttribute), typeof(BarTriggerAttribute) };
        var sut = new TriggerFinder(triggers);

        var result = sut.GetTargetsOf<BothFooBarTarget>().ToArray();
        
        Assert.Equal(
            expected: new[]{"Target", "Target"},
            actual: result.Select(x => x.Method.Name).ToArray()
        );

        Assert.Equal(
            expected: new[]{ typeof(FooTriggerAttribute), typeof(BarTriggerAttribute) },
            actual: result.Select(x => x.TriggerAttribute.GetType()).ToArray()
        );
    }

    [Fact]
    public void returns_expected_when_multiple_methods_are_annotated()
    {
        var triggers = new[] { typeof(FooTriggerAttribute), typeof(BarTriggerAttribute) };
        var sut = new TriggerFinder(triggers);

        var result = sut.GetTargetsOf<FooBarTarget>();
        
        Assert.Equal(
            expected: new[]{"FooTarget", "BarTarget"},
            actual: result.Select(x => x.Method.Name).ToArray()
        );
        
        Assert.Equal(
            expected: new[]{ typeof(FooTriggerAttribute), typeof(BarTriggerAttribute) },
            actual: result.Select(x => x.TriggerAttribute.GetType()).ToArray()
        );
    }

    #region private helper classes

    private class FooTriggerAttribute : Attribute { }
    private class BarTriggerAttribute : Attribute { }

    private class FooTarget
    {
        [FooTrigger]
        public Task Target() => Task.CompletedTask;
        
        public Task NotATarget() => Task.CompletedTask;
    }

    private class FooBarTarget
    {
        [FooTrigger]
        public Task FooTarget() => Task.CompletedTask;

        [BarTrigger]
        public Task BarTarget() => Task.CompletedTask;
    }

    private class BothFooBarTarget
    {
        [FooTrigger, BarTrigger]
        public Task Target() => Task.CompletedTask;
    }

    #endregion
}