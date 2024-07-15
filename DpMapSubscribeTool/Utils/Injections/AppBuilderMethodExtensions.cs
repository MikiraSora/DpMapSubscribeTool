using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Utils.Injections
{
    public static class AppBuilderMethodExtensions
    {
        public static AppBuilder AppendDependencyInject(this AppBuilder builer, Action<IServiceCollection> injectConfigFunc)
        {
            AppBuilderStatic.injectConfigFunc += injectConfigFunc;
            return builer;
        }

        internal class AppBuilderStatic
        {
            internal static Action<IServiceCollection> injectConfigFunc = serviceCollection => { };
        }
    }
}
