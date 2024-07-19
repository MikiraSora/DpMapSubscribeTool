using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.ValueConverters;
using DpMapSubscribeTool.ViewModels;
using DpMapSubscribeTool.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool;

public class App : Application
{
    internal IServiceProvider RootServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        InitServiceProvider();
        var logger = RootServiceProvider.GetService<ILogger<App>>();

        var injectableConverters = RootServiceProvider.GetServices<IInjectableValueConverter>();
        foreach (var converter in injectableConverters)
        {
            var key = converter.GetType().Name;
            logger.LogInformation($"add injectable converter: {key}");
            Resources[key] = converter;
        }

        var mainViewModel = ActivatorUtilities.CreateInstance<MainViewModel>(RootServiceProvider);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            singleViewPlatform.MainView = new MainView
            {
                DataContext = mainViewModel
            };

        base.OnFrameworkInitializationCompleted();
    }

    private void InitServiceProvider()
    {
        if (RootServiceProvider is not null)
            throw new Exception("InitServiceProvider() has been called.");

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(o => { o.SetMinimumLevel(LogLevel.Debug); });
        serviceCollection.AddKeyedScoped("AppBuild", (provider, o) =>
        {
            if (o is string key && key == "AppBuild")
                return serviceCollection;
            throw new Exception("Not allow get IServiceCollection objects.");
        });

        serviceCollection.AddInjectsByAttributes(typeof(App).Assembly);

        //add other DI collection from other assemblies.
        AppBuilderMethodExtensions.AppBuilderStatic.injectConfigFunc?.Invoke(serviceCollection);

        RootServiceProvider = serviceCollection.BuildServiceProvider();
    }
}