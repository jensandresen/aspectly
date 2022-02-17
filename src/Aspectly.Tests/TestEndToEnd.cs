using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspectly.Tests;

public class TestEndToEnd
{
    [Fact]
    public async Task decorates_target_with_expected_aspect_when_single_trigger_has_been_annotated()
    {
        var spy = new Spy();
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(spy);
                services.AddTransient<ITargetService, TargetService>();
                
                services.RewireWithAspects(options =>
                {
                    options.Register<ReportToSpyAttribute, SpyAspect>();
                });
            })
            .Build();

        var foo = host.Services.GetRequiredService<ITargetService>();
        await foo.Bar();

        Assert.Equal(
            expected: new[]
            {
                "Before",
                "foo-bar",
                "After"
            },
            actual: spy.Items
        );
    }

    [Fact]
    public async Task does_not_invoke_aspect_when_no_rewiring_as_been_made()
    {
        var spy = new Spy();
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(spy);
                services.AddTransient<ITargetService, TargetService>();
            })
            .Build();

        var foo = host.Services.GetRequiredService<ITargetService>();
        await foo.Bar();

        Assert.Equal(
            expected: new[]
            {
                "foo-bar",
            },
            actual: spy.Items
        );
    }

    [Fact]
    public async Task aspects_are_invoked_in_order_of_their_triggers()
    {
        var spy = new Spy();
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(spy);
                services.AddTransient<ITargetService, TargetService>();
                
                services.RewireWithAspects(options =>
                {
                    options.Register<ColorAttribute, ColorAspect>();
                });
            })
            .Build();

        var foo = host.Services.GetRequiredService<ITargetService>();
        await foo.Colors();

        Assert.Equal(
            expected: new[]
            {
                "red",
                "blue",
                "green",
                "transparent",
            },
            actual: spy.Items
        );
    }

    [Fact]
    public async Task return_values_from_decorated_method_is_supported()
    {
        var spy = new Spy();
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(spy);
                services.AddTransient<ITargetService, TargetService>();
                services.RewireWithAspects(options =>
                {
                    options.Register<ColorAttribute, ColorAspect>();
                });
            })
            .Build();

        var foo = host.Services.GetRequiredService<ITargetService>();
        var result = await foo.GetText();

        Assert.Equal("foo", result);
    }
}

public class Spy
{
    public List<string> Items { get; set; } = new List<string>();
}

public interface ITargetService
{
    Task Bar();
    Task Colors();
    Task<string> GetText();
}

public class TargetService : ITargetService
{
    private readonly Spy _spy;

    public TargetService(Spy spy)
    {
        _spy = spy;
    }
    
    [ReportToSpy]
    public Task Bar()
    {
        _spy.Items.Add("foo-bar");
        return Task.CompletedTask;
    }

    [Color(Name = "red")]
    [Color(Name = "blue")]
    [Color(Name = "green")]
    public Task Colors()
    {
        _spy.Items.Add("transparent");
        return Task.CompletedTask;
    }

    [Color]
    public Task<string> GetText()
    {
        return Task.FromResult("foo");
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ColorAttribute : Attribute
{
    public string? Name { get; set; }
}

public class ReportToSpyAttribute : Attribute
{
    
}

public class ColorAspect : IAspect
{
    private readonly Spy _spy;

    public ColorAspect(Spy spy)
    {
        _spy = spy;
    }
    
    public async Task Invoke(AspectContext context, AspectDelegate next)
    {
        if (context.TriggerAttribute is ColorAttribute color)
        {
            _spy.Items.Add(color.Name);
        }
        
        await next();
    }
}

public class SpyAspect : IAspect
{
    private readonly Spy _spy;

    public SpyAspect(Spy spy)
    {
        _spy = spy;
    }

    public async Task Invoke(AspectContext context, AspectDelegate next)
    {
        _spy.Items.Add("Before");
        await next();
        _spy.Items.Add("After");
    }
}