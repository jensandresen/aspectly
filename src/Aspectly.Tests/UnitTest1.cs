using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspectly.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspectly.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var spy = new Spy();
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(spy);
                services.AddTransient<IFoo, Foo>();
                
                services.AddAspects(options =>
                {
                    options.Register<ReportToSpyAttribute, SpyAspect>();
                });
            })
            .Build();

        var foo = host.Services.GetRequiredService<IFoo>();
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
}

public class Spy
{
    public List<string> Items { get; set; } = new List<string>();
}

public interface IFoo
{
    Task Bar();
}

public class Foo : IFoo
{
    private readonly Spy _spy;

    public Foo(Spy spy)
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

public class SpyAspect : IAsyncAspect
{
    private readonly Spy _spy;

    public SpyAspect(Spy spy)
    {
        _spy = spy;
    }
    
    public Task Before()
    {
        _spy.Items.Add("Before");
        return Task.CompletedTask;
    }

    public Task After()
    {
        _spy.Items.Add("After");
        return Task.CompletedTask;
    }
}