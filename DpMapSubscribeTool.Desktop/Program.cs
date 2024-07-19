using System;
using Avalonia;
using DesktopNotifications;
using DpMapSubscribeTool.Desktop.Utils.MethodExtensions;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Desktop;

internal class Program
{
    public static INotificationManager NotificationManager = default;
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
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
                    collection.AddLogging(o =>
                    {
                        o.SetMinimumLevel(LogLevel.Debug);
                        o.AddDebug();
                        o.AddConsole();
                    });
                }
            )
            .SetupDesktopNotifications(out NotificationManager)
            .LogToTrace();
    }
}