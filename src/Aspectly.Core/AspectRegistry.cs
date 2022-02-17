using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aspectly;

internal class AspectRegistry
{
    private readonly List<AspectTriggerMap> _aspectTriggers = new List<AspectTriggerMap>();
    private readonly Dictionary<MethodInfo, AnnotatedServiceRegistration> _registrations = new Dictionary<MethodInfo,AnnotatedServiceRegistration>();
    
    public IEnumerable<Type> RegisteredAspectTypes => _aspectTriggers.Select(x => x.AspectType);
    public IEnumerable<Type> RegisteredTriggerAttributeTypes => _aspectTriggers.Select(x => x.TriggerAttributeType);

    public void RegisterAspectTrigger(Type triggerAttributeType, Type aspectType)
    {
        if (!typeof(Attribute).IsAssignableFrom(triggerAttributeType))
        {
            throw new InvalidTriggerTypeException($"Type {triggerAttributeType} is an invalid trigger type. It must inherit from {typeof(Attribute)}.");
        }

        if (!typeof(IAspect).IsAssignableFrom(aspectType))
        {
            throw new InvalidAspectTypeException($"Type {aspectType} is an invalid aspect type. It must implement {typeof(IAspect)}.");
        }
        
        _aspectTriggers.Add(new AspectTriggerMap(triggerAttributeType, aspectType));
    }

    public bool HasTriggers(Type implementationType)
    {
        foreach (var methodInfo in implementationType.GetMethods())
        {
            foreach (var attribute in methodInfo.GetCustomAttributes())
            {
                if (RegisteredTriggerAttributeTypes.Contains(attribute.GetType()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void RegisterAnnotatedService(Type implementationType)
    {
        foreach (var methodInfo in implementationType.GetMethods())
        {
            var registration = new AnnotatedServiceRegistration(methodInfo);

            foreach (var attribute in methodInfo.GetCustomAttributes())
            {
                var triggerMap = _aspectTriggers.SingleOrDefault(x => x.TriggerAttributeType == attribute.GetType());
                
                if (triggerMap is not null)
                {
                    var temp = new TriggerInstanceToAspectType(attribute, triggerMap.AspectType);
                    registration.AddTrigger(temp);
                }
            }

            if (registration.HasTriggers)
            {
                _registrations.Add(registration.Method, registration);
            }
        }
    }

    public AnnotatedServiceRegistration? GetAspectRegistrationFor(MethodInfo method)
    {
        _registrations.TryGetValue(method, out var registration);
        return registration;
    }
    
    #region private helpers

    #endregion
}

internal class TriggerInstanceToAspectType
{
    public TriggerInstanceToAspectType(Attribute attribute, Type aspectType)
    {
        Attribute = attribute;
        AspectType = aspectType;
    }

    public Attribute Attribute { get; private set; }
    public Type AspectType { get; private set; }
}

internal class AspectTriggerMap
{
    public AspectTriggerMap(Type triggerAttributeType, Type aspectType)
    {
        TriggerAttributeType = triggerAttributeType;
        AspectType = aspectType;
    }

    public Type TriggerAttributeType { get; }
    public Type AspectType { get; }
}

internal class AnnotatedServiceRegistration
{
    private readonly LinkedList<TriggerInstanceToAspectType> _toAspectTypes = new();

    public AnnotatedServiceRegistration(MethodInfo method)
    {
        Method = method;
    }

    public MethodInfo Method { get; }
    public IEnumerable<TriggerInstanceToAspectType> Duno => _toAspectTypes;

    public bool HasTriggers => _toAspectTypes.Any();
        
    public void AddTrigger(TriggerInstanceToAspectType toAspectType)
    {
        _toAspectTypes.AddLast(toAspectType);
    }
}

public class InvalidTriggerTypeException : Exception
{
    public InvalidTriggerTypeException(string message) : base(message)
    {
        
    }
}

public class InvalidAspectTypeException : Exception
{
    public InvalidAspectTypeException(string message) : base(message)
    {
        
    }
}