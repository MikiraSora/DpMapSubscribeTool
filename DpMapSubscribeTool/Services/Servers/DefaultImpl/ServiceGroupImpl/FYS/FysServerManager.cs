using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Networks;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS.Bases;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS;

[RegisterInjectable(typeof(IServerActionExecutor), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerInfoSearcher), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerStateUpdater), ServiceLifetime.Singleton)]
public class FysServerManager : IFysServerServiceBase, IServerInfoSearcher, IServerStateUpdater,
    IServerActionExecutor
{
    private readonly IApplicationHttpFactory httpFactory;
    private readonly ILogger<FysServerManager> logger;
    private readonly IMapManager mapManager;
    private Dictionary<string, EventStreamServer> cacheEventStreamServerMap = new();

    private CancellationTokenSource cancellationTokenSource;

    private EventStreamData currentCachedEventStreamData;

    private Task dataReadyTask;

    public FysServerManager(IApplicationHttpFactory httpFactory, ILogger<FysServerManager> logger,
        IMapManager mapManager)
    {
        this.httpFactory = httpFactory;
        this.logger = logger;
        this.mapManager = mapManager;
    }

    public async Task Join(ServerInfo serverInfo)
    {
        var ipList = await Dns.GetHostAddressesAsync(serverInfo.Host);
        if (ipList.Length == 0)
        {
            logger.LogWarning($"Can't lookup ip address for host name:{serverInfo.Host}");
            return;
            //todo notify user by UI dialog.
        }
        var ipAddr = ipList.First();
        //var steamCmd = $"steam://connect/{ipAddr}:{serverInfo.Port}";
        var steamCmd = $"steam://rungame/730/76561202255233023/+connect%20{ipAddr}:{serverInfo.Port}";
        logger.LogInformation($"execute steamCmd: {steamCmd}");
        Process.Start(new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = true,
            Arguments = $"/C start {steamCmd}",
            CreateNoWindow = true
        });
    }

    public async Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo()
    {
        await CheckOrWaitDataReady();
        return currentCachedEventStreamData.Servers
            .Select(server => new ServerInfo
            {
                ServerGroup = "FYS", Port = server.Port, Host = server.Host, Name = server.Name,
                ServerGroupDisplay = "风云社"
            })
            .ToList();
    }

    public async Task<Server> QueryServer(ServerInfo info)
    {
        await CheckOrWaitDataReady();

        if (!cacheEventStreamServerMap.TryGetValue(info.EndPointDescription, out var etServer))
            return default;

        var fysServer = new FysServer();
        fysServer.UpdateProperties(etServer, info);
        return fysServer;
    }

    public async Task UpdateServer(Server server)
    {
        await CheckOrWaitDataReady();

        if (server is not FysServer fysServer)
        {
            logger.LogError(
                $"Can't update server because param is not FysServer object:({server.GetType().FullName})");
            return;
        }

        if (!cacheEventStreamServerMap.TryGetValue(server.Info.EndPointDescription, out var etServer))
        {
            logger.LogError(
                "Can't update server because related event-stream server is not found");
            return;
        }

        fysServer.UpdateProperties(etServer);
    }

    private Task CheckOrWaitDataReady()
    {
        if (cancellationTokenSource != null)
            return dataReadyTask;

        var dataReadyTaskSource = new TaskCompletionSource();
        cancellationTokenSource = new CancellationTokenSource();
        dataReadyTask = dataReadyTaskSource.Task;
        Task.Run(() => OnPullThread(dataReadyTaskSource, cancellationTokenSource.Token),
            cancellationTokenSource.Token);
        return dataReadyTask;
    }

    private async void OnPullThread(TaskCompletionSource taskCompletionSource,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("event-stream thread start.");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("event-stream request sent.");
                var resp = await httpFactory.SendAsync("https://fyscs.com/silverwing/system/dashboard", req =>
                {
                    req.Method = HttpMethod.Get;
                    //build header
                    req.Headers.Add("Referer", "https://fyscs.com/dashboard");
                    req.Headers.Add("X-Client", "swallowtail");
                    req.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
                }, cancellationToken);
                logger.LogInformation("event-stream response reached.");

                var jsonContent = await resp.Content.ReadAsStringAsync(cancellationToken);
                var eventStreamData = JsonSerializer.Deserialize<EventStreamData>(jsonContent);

                UpdateEventStreamData(eventStreamData);
                //event-stream data is ready
                if (!taskCompletionSource.Task.IsCompleted)
                    taskCompletionSource.SetResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, "deserialize/update event-stream data failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
        }

        logger.LogInformation("event-stream thread end.");
    }

    private async void OnEventStreamThread(TaskCompletionSource taskCompletionSource,
        CancellationToken cancellationToken)
    {
        //https://fyscs.com/silverwing/system/dashboard
        var client = new HttpClient();

        //build header
        client.DefaultRequestHeaders.Add("Accept", "text/event-stream");
        client.DefaultRequestHeaders.Add("Referer", "https://fyscs.com/dashboard");
        client.DefaultRequestHeaders.Add("X-Client", "swallowtail");
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");

        await using var respStream =
            await client.GetStreamAsync("https://fyscs.com/silverwing/EventStream/dashboard", cancellationToken);
        logger.LogInformation("event-stream response reached.");
        using var reader = new StreamReader(respStream);

        while (!reader.EndOfStream)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("stop OnEventStreamThread() cause cancellation requested");
                break;
            }

            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) ||
                !line.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase))
                continue;

            var jsonContent = line.Substring("data:".Length).Trim();
            try
            {
                var eventStreamData = JsonSerializer.Deserialize<EventStreamData>(jsonContent);
                UpdateEventStreamData(eventStreamData);
                //event-stream data is ready
                taskCompletionSource.TrySetResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"deserialize/update event-stream data failed. line= {line}");
            }
        }

        logger.LogInformation("event-stream response end.");
    }

    private void UpdateEventStreamData(EventStreamData eventStreamData)
    {
        currentCachedEventStreamData = eventStreamData;
        logger.LogInformation("event-stream data updated.");

        foreach (var (map, translation) in eventStreamData.Servers.Select(x => (x.Map, x.Translation)))
            mapManager.CacheMapTranslationName(map, translation, false);

        cacheEventStreamServerMap =
            currentCachedEventStreamData.Servers.ToDictionary(x => $"{x.Host}:{x.Port}", x => x);
    }
}