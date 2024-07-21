using DpMapSubscribeTool;
using DpMapSubscribeTool.Services.Servers;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

ServiceCollection CreateDefaultServiceCollection()
{
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddLogging(o =>
    {
        o.SetMinimumLevel(LogLevel.Debug);
        o.AddConsole();
        o.AddDebug();
    });
    serviceCollection.AddKeyedScoped("AppBuild", (_, o) =>
    {
        if (o is "AppBuild")
            return serviceCollection;
        throw new Exception("Not allow get IServiceCollection objects.");
    });

    serviceCollection.AddInjectsByAttributes(typeof(App).Assembly);
    serviceCollection.AddInjectsByAttributes(typeof(Program).Assembly);
    /*
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", false)
        .Build();
    serviceCollection.AddSingleton(configuration);
    */
    return serviceCollection;
}

var serviceCollection = CreateDefaultServiceCollection();
var serviceProvider = serviceCollection.BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<Program>>();
logger.LogInformationEx("HELLO TEST CONSOLE.");

var serverManager = serviceProvider.GetService<IServerManager>();
await serverManager.RefreshServers();

while (true)
    await Task.Delay(100);