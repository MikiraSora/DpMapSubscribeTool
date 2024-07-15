using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DpMapSubscribeTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool
{
    internal class ViewLocator : IDataTemplate
    {
        public Control Build(object d)
        {
            if (d is not ViewModelBase viewModel)
                return null;

            var name = string.Join(".", viewModel.GetType().FullName.Split(".").Select(x =>
            {
                if (x == "ViewModels")
                    return "Views";
                if (x.Length > "ViewModel".Length && x.EndsWith("ViewModel"))
                    return x.Substring(0, x.Length - "Model".Length);
                return x;
            }));
            var type = Type.GetType(name);

            if (type == null)
            {
                var msg = $"<viwe type not found:{name}; model type:{viewModel.GetType().FullName}>";
#if DEBUG
                throw new Exception(msg);
#else
				return new TextBlock { Text = msg };
#endif
            }

            var control = (Control)ActivatorUtilities.CreateInstance((Application.Current as App).RootServiceProvider, type);
            control.Loaded += (a, aa) =>
            {
                control.DataContext = viewModel;
                viewModel.OnViewAfterLoaded(control);
            };
            control.Unloaded += (a, aa) =>
            {
                viewModel.OnViewBeforeUnload(control);
                control.DataContext = null;
            };
            return control;
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
