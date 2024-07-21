using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DpMapSubscribeTool.ViewModels;
using DpMapSubscribeTool.Views.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool;

internal class ViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Control> cachedViewMap = new();
    private readonly ILogger<ViewLocator> logger;

    public ViewLocator(ILogger<ViewLocator> logger)
    {
        this.logger = logger;
    }

    public bool ForceSinglePageNavigation { get; set; } = true;
    public bool UseSinglePageNavigation { get; } = true;

    public Control Build(object d)
    {
        if (d is not ViewModelBase viewModel)
            return null;

        var viewTypeName = GetViewTypeName(viewModel.GetType());
        var viewType = GetViewType(viewTypeName);

        var isCachable = viewType.GetCustomAttribute<CachableViewAttribute>() is not null;

        var control = default(Control);
        if (isCachable && cachedViewMap.TryGetValue(viewType, out var view))
        {
            //from cache
            logger.LogDebugEx($"provide cached view {viewType.Name}");
            control = view;
        }
        else
        {
            if (viewType == null)
            {
                var msg = $"<viwe type not found:{viewTypeName}; model type:{viewModel.GetType().FullName}>";
#if DEBUG
                throw new Exception(msg);
#else
				return new TextBlock { Text = msg };
#endif
            }

            //create new
            control = (Control) ActivatorUtilities.CreateInstance((Application.Current as App).RootServiceProvider,
                viewType);

            control.Loaded += (a, aa) => { viewModel.OnViewAfterLoaded(control); };
            control.Unloaded += (a, aa) =>
            {
                viewModel.OnViewBeforeUnload(control);
                control.DataContext = null;

                if (isCachable)
                {
                    cachedViewMap[viewType] = control;
                    logger.LogDebugEx($"recycle view {viewType.Name} object for ViewLocator");
                }
            };
        }

        control.DataContext = viewModel;
        return control;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Match(object data)
    {
        return data is ViewModelBase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Create(object viewModel)
    {
        return Build(viewModel);
    }

    private string GetViewTypeName(Type viewModelType)
    {
        var name = string.Join(".", viewModelType.FullName.Split(".").Select(x =>
        {
            if (x == "ViewModels")
                return "Views";
            if (x.Length > "ViewModel".Length && x.EndsWith("ViewModel"))
                return x.Substring(0, x.Length - "Model".Length);
            return x;
        }));
        return name;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Type GetViewType(Type viewModelType)
    {
        return GetViewType(GetViewTypeName(viewModelType));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Type GetViewType(string viewModelTypeName)
    {
        return Type.GetType(viewModelTypeName);
    }
}