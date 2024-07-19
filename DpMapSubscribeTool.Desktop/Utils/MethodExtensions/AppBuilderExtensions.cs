using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DesktopNotifications;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;

namespace DpMapSubscribeTool.Desktop.Utils.MethodExtensions;

public static class AppBuilderExtensions
{
    public static AppBuilder SetupDesktopNotifications(this AppBuilder builder, out INotificationManager manager)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var context = WindowsApplicationContext.FromCurrentProcess();
            manager = new WindowsNotificationManager(context);
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var context = FreeDesktopApplicationContext.FromCurrentProcess();
            manager = new FreeDesktopNotificationManager(context);
        }
        else
        {
            //TODO: OSX once implemented/stable
            manager = null;
            return builder;
        }

        //TODO Any better way of doing this?
        manager.Initialize().GetAwaiter().GetResult();

        var manager_ = manager;
        builder.AfterSetup(b =>
        {
            if (b.Instance?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
                lifetime.Exit += (s, e) => { manager_.Dispose(); };
        });

        return builder;
    }
}