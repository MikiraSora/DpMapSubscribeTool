using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using DpMapSubscribeTool.Utils;
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
    private readonly CommonJoinServer commonJoinServer;
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
        IServiceProvider provider, CommonJoinServer commonJoinServer,
        IMapManager mapManager, IDialogManager dialogManager, IPersistence persistence)
    {
        this.httpFactory = httpFactory;
        this.logger = logger;
        this.provider = provider;
        this.commonJoinServer = commonJoinServer;
        this.mapManager = mapManager;
        this.dialogManager = dialogManager;
        this.persistence = persistence;

        Initialize();
    }

    public async Task<bool> CheckUserInvaild(string name)
    {
        var isWanjiaBro = name.StartsWith("玩家Z");
        logger.LogInformationEx($"user name {name} is 玩家哥");
        var regexPattern = name + @"\s*#\(\w+\)";
        var regex = new Regex(regexPattern);
        var cancelToken = DelayCancellationTokenSource.Create(TimeSpan.FromSeconds(2));

        async Task<bool> check(ServerInfo serverInfo)
        {
            try
            {
                using var gameServer = new GameServer(serverInfo.Host, serverInfo.Port);
                logger.LogDebugEx($"begin check server {serverInfo.EndPointDescription}");
                var players = await gameServer.GetPlayersAsync(cancelToken);
                var playerNames = players.Select(x => x.Name).ToArray();
                logger.LogDebugEx(
                    $"server {serverInfo.EndPointDescription} return players:{string.Join(", ", playerNames)}");
                foreach (var playerName in playerNames)
                {
                    if (playerName == name)
                        return true;
                    if (isWanjiaBro && regex.IsMatch(playerName))
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
        var queryResults = await Task.WhenAll(serverInfos.Select(info => check(info)).ToArray());
        var checkResult = queryResults.Any(x => x);
        logger.LogInformationEx($"playerName check result: {checkResult}");
        return checkResult;
    }

    public string ServerGroup => "FYS";
    public string ServerGroupDescription => "风云社";

    public Task Join(ServerInfo serverInfo)
    {
        return commonJoinServer.JoinServer(serverInfo.Host, serverInfo.Port);
    }

    public async Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo()
    {
        await CheckOrWaitDataReady();
        return currentCachedEventStreamData.Servers
            .Select(server => new ServerInfo
            {
                ServerGroup = ServerGroup, Port = server.Port, Host = server.Host, Name = server.Name,
                ServerGroupDisplay = ServerGroupDescription
            })
            .ToList();
    }

    public Task<bool> CheckSqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsUserInServer(List<string> playerNameList, CancellationToken cancellationToken)
    {
        var name = fysServerSettings.PlayerName;
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        var isWanjiaBro = name.StartsWith("玩家Z");
        var regexPattern = name + @"\s*#\(\w+\)";
        var regex = new Regex(regexPattern);

        foreach (var playerName in playerNameList)
        {
            if (playerName == name)
                return Task.FromResult(true);
            if (isWanjiaBro && regex.IsMatch(playerName))
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public async Task<Server> QueryServer(ServerInfo info)
    {
        await CheckOrWaitDataReady();

        if (!cacheEventStreamServerMap.TryGetValue(info.EndPointDescription, out var etServer))
            return default;

        var fysServer = ActivatorUtilities.CreateInstance<FysServer>(provider);
        fysServer.UpdateProperties(etServer, info);
        fysServer.MapTranslationName = mapManager.GetMapTranslationName(ServerGroup, fysServer.Map);
        return fysServer;
    }

    public async Task UpdateServer(Server server)
    {
        await CheckOrWaitDataReady();

        if (server is not FysServer fysServer)
        {
            logger.LogErrorEx(
                $"Can't update server because param is not FysServer object:({server.GetType().FullName})");
            return;
        }

        if (!cacheEventStreamServerMap.TryGetValue(server.Info.EndPointDescription, out var etServer))
        {
            logger.LogErrorEx(
                "Can't update server because related event-stream server is not found");
            return;
        }

        fysServer.UpdateProperties(etServer);
    }

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
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
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        logger.LogInformationEx("event-stream thread start.");
        var timeInterval = TimeSpan.FromSeconds(fysServerSettings.EventDataRefreshInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogDebugEx("event-stream request sent.");
                var resp = await httpFactory.SendAsync("https://fyscs.com/silverwing/system/dashboard", req =>
                {
                    req.Method = HttpMethod.Get;
                    //build header
                    req.Headers.Add("Referer", "https://fyscs.com/dashboard");
                    req.Headers.Add("X-Client", "swallowtail");
                    req.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
                }, cancellationToken);
                logger.LogDebugEx("event-stream response reached.");

                var jsonContent = await resp.Content.ReadAsStringAsync(cancellationToken);
                if (jsonContent.StartsWith("{"))
                {
                    var eventStreamData = JsonSerializer.Deserialize<EventStreamData>(jsonContent);
                    UpdateEventStreamData(eventStreamData);
                }
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "deserialize/update event-stream data failed.");
            }

            //event-stream data is ready(?)
            if (!taskCompletionSource.Task.IsCompleted)
                taskCompletionSource.SetResult();

            await Task.Delay(timeInterval, default);
        }

        logger.LogInformationEx("event-stream thread end.");
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
        logger.LogDebugEx("event-stream response reached.");
        using var reader = new StreamReader(respStream);

        while (!reader.EndOfStream)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformationEx("stop OnEventStreamThread() cause cancellation requested");
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
                logger.LogErrorEx(e, $"deserialize/update event-stream data failed. line= {line}");
            }
        }

        logger.LogInformationEx("event-stream response end.");
    }

    private void UpdateEventStreamData(EventStreamData eventStreamData)
    {
        currentCachedEventStreamData = eventStreamData;
        logger.LogInformationEx("event-stream data updated.");

        foreach (var (map, translation) in eventStreamData.Servers.Select(x => (x.Map, x.Translation)))
            mapManager.CacheMapTranslationName(ServerGroup, map, translation);

        cacheEventStreamServerMap =
            currentCachedEventStreamData.Servers.ToDictionary(x => $"{x.Host}:{x.Port}", x => x);
    }
}