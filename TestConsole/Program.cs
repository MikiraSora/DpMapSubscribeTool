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


await using var fs =
    File.Open(@"F:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\console.log",
        FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
var reader = new StreamReader(fs);
var buffer = new char[102400];
var sb = new StringBuilder();
fs.Seek(0, SeekOrigin.End);

while (true)
{
    if (fs.Position> fs.Length)
    {
        //log is recreated, just reset position.
        fs.Seek(0, SeekOrigin.Begin);
    }
    
    var read = await reader.ReadAsync(buffer);
    var prevIndex = 0;
    for (var i = 0; i < read; i++)
    {
        var ch = buffer[i];
        if (ch == '\n')
        {
            sb.Clear();
            sb.Append(buffer[prevIndex.. (i-1)]);
            var line = sb.ToString();
            prevIndex = i + 1;
            Console.WriteLine($"Line: {line}");
        }
    }
}