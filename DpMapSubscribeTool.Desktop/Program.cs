using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DesktopNotifications;
using DpMapSubscribeTool.Desktop.Utils.Logging;
using DpMapSubscribeTool.Desktop.Utils.MethodExtensions;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Application = Avalonia.Application;

namespace DpMapSubscribeTool.Desktop;

internal class Program
{
    public static INotificationManager NotificationManager;
    private static bool exceptionHandling;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
#if !DEBUG
        AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
        {
            ProcessException(sender, e.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
        };
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            ProcessException(sender, e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
#endif
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args, lifetime => lifetime.Exit += OnExit);
    }

    private static void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (Application.Current is App app)
            app.RootServiceProvider.GetService<ILogger<Program>>()?.LogInformation("BYE.");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .AppendDependencyInject(collection =>
                {
                    collection.AddInjectsByAttributes(typeof(Program).Assembly);
#if DEBUG
                    if (DesignModeHelper.IsDesignMode)
                        return;
#endif
                    collection.AddLogging(o =>
                    {
                        o.SetMinimumLevel(LogLevel.Debug);
                        o.AddProvider(new FileLoggerProvider());
                        o.AddDebug();
                        o.AddConsole();
                    });
                }
            )
            .SetupDesktopNotifications(out NotificationManager)
            .LogToTrace();
    }

    private static void ProcessException(object sender, Exception exception, string trigSource)
    {
        if (exceptionHandling)
            return;
        exceptionHandling = true;

        var app = Application.Current as App;
        var logger = app?.RootServiceProvider?.GetService<ILogger<Program>>();
        logger?.LogInformationEx($"trigged by {trigSource}");

        try
        {
            if (app != null)
            {
                var windows = (app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                    ?.Windows;
                if (windows != null)
                    foreach (var window in windows)
                        window.Hide();
            }
        }
        catch
        {
            // ignored
        }

        var innerMessage = TravalInnerExceptionMessage(exception) ?? "<NO EXCEPTION>";

        MessageBox.Show($"程序遇到致命错误，即将关闭，相关日志已保存。\n错误原因:{innerMessage}");

        Environment.Exit(-1);

        exceptionHandling = false;

        string TravalInnerExceptionMessage(Exception e)
        {
            return e is null ? null : TravalInnerExceptionMessage(e.InnerException) ?? e.Message;
        }
    }
}