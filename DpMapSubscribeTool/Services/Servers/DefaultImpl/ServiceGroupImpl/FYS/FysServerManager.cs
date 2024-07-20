using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Networks;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS.Bases;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamQuery;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS;

[RegisterInjectable(typeof(IServerActionExecutor), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerInfoSearcher), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerSqueezeJoinRunner), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerStateUpdater), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IFysServerServiceBase), ServiceLifetime.Singleton)]
public class FysServerManager : IFysServerServiceBase, IServerInfoSearcher, IServerStateUpdater,
    IServerSqueezeJoinRunner,
    IServerActionExecutor
{
    private readonly IDialogManager dialogManager;
    private readonly IApplicationHttpFactory httpFactory;
    private readonly ILogger<FysServerManager> logger;
    private readonly IMapManager mapManager;
    private readonly IPersistence persistence;
    private readonly IServiceProvider provider;
    private Dictionary<string, EventStreamServer> cacheEventStreamServerMap = new();

    private CancellationTokenSource cancellationTokenSource;

    private EventStreamData currentCachedEventStreamData;

    private Task dataReadyTask;
    private FysServerSettings fysServerSettings;

    public FysServerManager(IApplicationHttpFactory httpFactory, ILogger<FysServerManager> logger,
        IServiceProvider provider,
        IMapManager mapManager, IDialogManager dialogManager, IPersistence persistence)
    {
        this.httpFactory = httpFactory;
        this.logger = logger;
        this.provider = provider;
        this.mapManager = mapManager;
        this.dialogManager = dialogManager;
        this.persistence = persistence;

        Initialize();
    }

    public async Task<bool> CheckUserInvaild(string name)
    {
        var isWanjia = name.StartsWith("玩家Z");
        logger.LogInformation($"user name {name} is 玩家哥");
        var regex = new Regex(isWanjia + @"\s*#\(\w+\)");

        async Task<bool> check(ServerInfo serverInfo)
        {
            try
            {
                var gameServer = new GameServer(serverInfo.Host, serverInfo.Port);
                var players = await gameServer.GetPlayersAsync();
                foreach (var playerName in players.Select(x => x.Name))
                {
                    if (playerName == name)
                        return true;
                    if (isWanjia && regex.IsMatch(playerName))
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        var serverInfos = await GetAllAvaliableServerInfo();
        var queryResultTasks = serverInfos.Select(info => check(info)).ToArray();
        var queryResults = await Task.WhenAll(queryResultTasks);
        return queryResults.Any(x => x);
    }

    public async Task Join(ServerInfo serverInfo)
    {
        var ipList = await Dns.GetHostAddressesAsync(serverInfo.Host);
        if (ipList.Length == 0)
        {
            logger.LogWarning($"Can't lookup ip address for host name:{serverInfo.Host}");
            await dialogManager.ShowMessageDialog($"无法进入服务器，无法获取服务器ip地址({serverInfo.Host})", DialogMessageType.Error);
            return;
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

    public async Task<bool> CheckSqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsUserInServer(List<string> playerNameList, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Server> QueryServer(ServerInfo info)
    {
        await CheckOrWaitDataReady();

        if (!cacheEventStreamServerMap.TryGetValue(info.EndPointDescription, out var etServer))
            return default;

        var fysServer = ActivatorUtilities.CreateInstance<FysServer>(provider);
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

    private async void Initialize()
    {
        fysServerSettings = await persistence.Load<FysServerSettings>();
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
        var timeInterval = TimeSpan.FromSeconds(fysServerSettings.EventDataRefreshInterval);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogDebug("event-stream request sent.");
                var resp = await httpFactory.SendAsync("https://fyscs.com/silverwing/system/dashboard", req =>
                {
                    req.Method = HttpMethod.Get;
                    //build header
                    req.Headers.Add("Referer", "https://fyscs.com/dashboard");
                    req.Headers.Add("X-Client", "swallowtail");
                    req.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
                }, cancellationToken);
                logger.LogDebug("event-stream response reached.");

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

            await Task.Delay(timeInterval, cancellationToken);
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
        logger.LogDebug("event-stream response reached.");
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