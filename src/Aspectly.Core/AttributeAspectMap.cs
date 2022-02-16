using System;

namespace Aspectly;

internal class AttributeAspectMap
{
    public AttributeAspectMap(Type attribute, Type aspect)
    {
        Attribute = attribute;
        Aspect = aspect;
    }

    public Type Attribute { get; private set; }
    public Type Aspect { get; private set; }
}