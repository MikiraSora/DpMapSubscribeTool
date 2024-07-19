using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DpMapSubscribeTool.Utils.Injections;

public static class AddInjectAttbuteServiceCollection
{
    public static IServiceCollection AddInjectsByAttributes(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes().Where(type => type.GetCustomAttributes<RegisterInjectableAttribute>().Any());

        var singletonCachedObjects = new Dictionary<Type, object>();

        foreach (var type in types)
        {
            var attrs = type.GetCustomAttributes<RegisterInjectableAttribute>().ToArray();
            foreach (var attr in attrs)
            {
                var targetType = attr.TargetInjectType;

                if (attr.ServiceLifetime == ServiceLifetime.Singleton && attrs.Length > 1)
                {
                    if (attrs.Any(x => x.ServiceLifetime != ServiceLifetime.Singleton))
                        throw new Exception(
                            $"For type {type.FullName}, all [RegisterInjectable] lifetime must be same if contains singleton lifetime");


                    // all [RegisterInjectable] are singleton lifetime.
                    foreach (var attr2 in attrs)
                        services.AddSingleton(attr2.TargetInjectType, provider =>
                        {
                            if (!singletonCachedObjects.TryGetValue(type, out var cacheObj))
                                cacheObj = singletonCachedObjects[type] =
                                    ActivatorUtilities.CreateInstance(provider, type);
                            return cacheObj;
                        });
                    break;
                }

                Func<Type, Type, IServiceCollection> caller = attr.ServiceLifetime switch
                {
                    ServiceLifetime.Singleton => services.AddSingleton,
                    ServiceLifetime.Transient => services.AddTransient,
                    ServiceLifetime.Scoped => services.AddScoped,
                    _ => default
                };
                if (caller != null)
                    caller(targetType, type);
            }
        }

        return services;
    }
}