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
}

public class Spy
{
    public List<string> Items { get; set; } = new List<string>();
}

public interface ITargetService
{
    Task Bar();
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
}

public class ReportToSpyAttribute : Attribute
{
    
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