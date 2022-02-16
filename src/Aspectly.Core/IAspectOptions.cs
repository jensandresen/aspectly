using System;
using Aspectly;

namespace Microsoft.Extensions.DependencyInjection;

public interface IAspectOptions
{
    void Register<TAttribute, TAspect>()
        where TAttribute : Attribute
        where TAspect : class, IAspect;
}