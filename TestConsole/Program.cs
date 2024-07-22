using System.Net.WebSockets;
using System.Text;
using DpMapSubscribeTool;
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
/*
using var websocket = new WebSocket($"wss://ws2.moeub.cn/ws?files=0&appid=730");
await websocket.OpenAsync();
websocket.MessageReceived += (sender, eventArgs) =>
{
    Console.WriteLine(eventArgs.Message);
    Console.WriteLine();
};
websocket.Error += (sender, eventArgs) =>
{

};
websocket.Closed += (sender, eventArgs) =>
{

};
websocket.Opened += (sender, eventArgs) =>
{

};
*/
var client = new ClientWebSocket();
await client.ConnectAsync(new Uri("wss://ws2.moeub.cn/ws?files=0&appid=730"), default);
var recvBuffer = new byte[1024];
var ms = new MemoryStream();
var read = 0;
while (true)
{
    var result = await client.ReceiveAsync(recvBuffer, default);
    await ms.WriteAsync(recvBuffer, 0, result.Count, default);
    read += result.Count;
    if (result.EndOfMessage)
    {
        var str = Encoding.UTF8.GetString(ms.GetBuffer(), 0, read);
        ms.Seek(0, SeekOrigin.Begin);
        read = 0;

        await File.AppendAllTextAsync("F:\\output.txt", str);
        await File.AppendAllTextAsync("F:\\output.txt", "\n");
    }
}

Console.ReadLine();