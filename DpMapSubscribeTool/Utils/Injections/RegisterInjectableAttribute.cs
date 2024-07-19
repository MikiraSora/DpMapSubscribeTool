using System;
using Microsoft.Extensions.DependencyInjection;

namespace DpMapSubscribeTool.Utils.Injections;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RegisterInjectableAttribute : Attribute
{
    public RegisterInjectableAttribute(Type targetType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        TargetInjectType = targetType;
        ServiceLifetime = serviceLifetime;
    }

    public Type TargetInjectType { get; }

    public ServiceLifetime ServiceLifetime { get; }
}