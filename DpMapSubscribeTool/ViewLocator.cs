using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DpMapSubscribeTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DpMapSubscribeTool;

internal class ViewLocator : IDataTemplate
{
    public bool ForceSinglePageNavigation { get; set; } = true;
    public bool UseSinglePageNavigation { get; } = true;

    public Control Build(object d)
    {
        if (d is not ViewModelBase viewModel)
            return null;

        var viewTypeName = GetViewTypeName(viewModel.GetType());
        var viewType = GetViewType(viewTypeName);

        if (viewType == null)
        {
            var msg = $"<viwe type not found:{viewTypeName}; model type:{viewModel.GetType().FullName}>";
#if DEBUG
            throw new Exception(msg);
#else
				return new TextBlock { Text = msg };
#endif
        }

        var control =
            (Control) ActivatorUtilities.CreateInstance((Application.Current as App).RootServiceProvider, viewType);
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