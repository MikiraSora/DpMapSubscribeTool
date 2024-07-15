using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Utils.Injections
{
    public static class AddInjectAttbuteServiceCollection
    {
        public static IServiceCollection AddInjectsByAttributes(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly.GetTypes().Where(type => type.GetCustomAttributes<RegisterInjectableAttribute>().Any());

            foreach (var type in types)
            {
                foreach (var attr in type.GetCustomAttributes<RegisterInjectableAttribute>())
                {
                    var targetType = attr.TargetInjectType;

                    Func<Type, Type, IServiceCollection> caller = attr.ServiceLifetime switch
                    {
                        ServiceLifetime.Singleton => services.AddSingleton,
                        ServiceLifetime.Transient => services.AddTransient,
                        ServiceLifetime.Scoped => services.AddScoped,
                        _ => default
                    };

                    if (caller != null)
                    {
                        caller(targetType, type);
                    }
                }
            }

            return services;
        }
    }
}
