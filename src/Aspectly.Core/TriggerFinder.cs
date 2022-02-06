using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aspectly.Core;

public class TriggerFinder
{
    private readonly Type[] _triggerAttributes;

    public TriggerFinder(Type[] triggerAttributes)
    {
        _triggerAttributes = triggerAttributes;
    }

    public IEnumerable<TriggerTarget> GetTargetsOf<T>() => GetTargetsOf(typeof(T));

    public IEnumerable<TriggerTarget> GetTargetsOf(Type type)
    {
        foreach (var methodInfo in type.GetMethods())
        {
            foreach (var attribute in methodInfo.GetCustomAttributes())
            {
                if (_triggerAttributes.Contains(attribute.GetType()))
                {
                    yield return new TriggerTarget(type, attribute, methodInfo);
                }
            }
        }
    }

    public class TriggerTarget
    {
        public TriggerTarget(Type hostType, Attribute triggerAttribute, MethodInfo method)
        {
            HostType = hostType;
            TriggerAttribute = triggerAttribute;
            Method = method;
        }

        public Type HostType { get; private set; }
        public Attribute TriggerAttribute { get; private set; }
        public MethodInfo Method { get; private set; }
    }
}