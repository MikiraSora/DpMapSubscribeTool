using System;
using DpMapSubscribeTool.Utils.Injections;
using Avalonia;

namespace DpMapSubscribeTool.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .AppendDependencyInject(collection => collection.AddInjectsByAttributes(typeof(Program).Assembly))
            .LogToTrace();

}
