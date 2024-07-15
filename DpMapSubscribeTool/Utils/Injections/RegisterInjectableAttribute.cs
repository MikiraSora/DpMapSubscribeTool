using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Utils.Injections
{
    public class RegisterInjectableAttribute : Attribute
    {
        private readonly Type type;

        public RegisterInjectableAttribute(Type targetType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            type = targetType;
            ServiceLifetime = serviceLifetime;
        }

        public Type TargetInjectType => type;

        public ServiceLifetime ServiceLifetime { get; }
    }
}
