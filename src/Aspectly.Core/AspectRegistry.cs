using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aspectly;

internal class AspectRegistry
{
    private readonly List<AspectTrigger> _aspectTriggers = new List<AspectTrigger>();
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
        
        _aspectTriggers.Add(new AspectTrigger(triggerAttributeType, aspectType));
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
                if (RegisteredTriggerAttributeTypes.Contains(attribute.GetType()))
                {
                    registration.AddTriggerType(attribute.GetType());
                }
            }

            if (registration.HasTriggers)
            {
                _registrations.Add(registration.Method, registration);
            }
        }
    }

    public IEnumerable<Type> GetAspectTypesFor(MethodInfo method)
    {
        if (!_registrations.TryGetValue(method, out var registration))
        {
            yield break;
        }

        foreach (var triggerType in registration.TriggerTypes)
        {
            var aspectTypes = _aspectTriggers
                .Where(x => x.TriggerAttributeType == triggerType)
                .Select(x => x.AspectType);

            foreach (var aspectType in aspectTypes)
            {
                yield return aspectType;
            }
        }
    }
    
    #region private helpers

    private class AspectTrigger
    {
        public AspectTrigger(Type triggerAttributeType, Type aspectType)
        {
            TriggerAttributeType = triggerAttributeType;
            AspectType = aspectType;
        }

        public Type TriggerAttributeType { get; private set; }
        public Type AspectType { get; private set; }
    }

    #endregion
}

internal class AnnotatedServiceRegistration
{
    private readonly LinkedList<Type> _triggerTypes = new();

    public AnnotatedServiceRegistration(MethodInfo method)
    {
        Method = method;
    }

    public MethodInfo Method { get; }
    public IEnumerable<Type> TriggerTypes => _triggerTypes;
    public bool HasTriggers => _triggerTypes.Any();
        
    public void AddTriggerType(Type triggerType)
    {
        _triggerTypes.AddLast(triggerType);
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