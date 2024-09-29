using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Networks;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.ZED.Bases;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.ZED;

[RegisterInjectable(typeof(IServerActionExecutor), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerInfoSearcher), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerSqueezeJoinRunner), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerStateUpdater), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IZEDServerServiceBase), ServiceLifetime.Singleton)]
public class ZEDServerManager : IZEDServerServiceBase, IServerInfoSearcher, IServerStateUpdater,
    IServerSqueezeJoinRunner,
    IServerActionExecutor
{
    private readonly CommonJoinServer commonJoinServer;
    private readonly IDialogManager dialogManager;
    private readonly IApplicationHttpFactory httpFactory;
    private readonly ILogger<ZEDServerManager> logger;
    private readonly IMapManager mapManager;

    private readonly IPersistence persistence;

    private readonly Regex serverListRegex = new(@"ServerArray\.push\(""(.+?):(\d+)""\);");

    private CancellationTokenSource cancellationTokenSource;
    private List<ZEDServer> currentServerStatusList;
    private Dictionary<string, ZEDServer> currentServerStatusMap;

    private Task dataReadyTask;

    public ZEDServerManager(IApplicationHttpFactory httpFactory, ILogger<ZEDServerManager> logger,
        CommonJoinServer commonJoinServer,
        IMapManager mapManager, IDialogManager dialogManager, IPersistence persistence)
    {
        this.httpFactory = httpFactory;
        this.logger = logger;
        this.commonJoinServer = commonJoinServer;
        this.mapManager = mapManager;
        this.dialogManager = dialogManager;
        this.persistence = persistence;

        Initialize();
    }

    public Task Join(ServerInfo serverInfo)
    {
        return commonJoinServer.JoinServer(serverInfo.Host, serverInfo.Port);
    }

    public async Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo()
    {
        await CheckOrWaitDataReady();

        return currentServerStatusList.Select(x =>
        {
            var split = x.Ip.Split(":");
            return new ServerInfo
            {
                ServerGroupDisplay = ServerGroupDescription,
                ServerGroup = ServerGroup,
                //simply names
                Name = x.HostName.Replace("多线 ZombiEden.cn", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                    .Trim(),
                Host = split[0],
                Port = int.Parse(split[1])
            };
        });
    }

    public Task<bool> CheckSqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsUserInServer(List<string> playerNameList, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Server> QueryServer(ServerInfo info)
    {
        var exgWrappedServer = new ZEDWrappedServer
        {
            Info = info
        };

        await UpdateServer(exgWrappedServer);

        return exgWrappedServer;
    }

    public async Task UpdateServer(Server server)
    {
        await CheckOrWaitDataReady();

        if (server is not ZEDWrappedServer exgWrappedServer)
        {
            logger.LogErrorEx(
                $"Can't update server because param is not ZEDWrappedServer object:({server.GetType().FullName})");
            return;
        }

        if (currentServerStatusMap.TryGetValue(exgWrappedServer.Info.EndPointDescription, out var zedServer))
        {
            exgWrappedServer.Map = zedServer.Map ?? "<Unknown Map>";
            exgWrappedServer.MapTranslationName = mapManager.GetMapTranslationName(ServerGroup, exgWrappedServer.Map);
            exgWrappedServer.State = string.Empty;
            exgWrappedServer.CurrentPlayerCount = zedServer.Players;
            exgWrappedServer.MaxPlayerCount = zedServer.MaxPlayers;
        }
    }

    public string ServerGroup => "ZED";
    public string ServerGroupDescription => "僵尸乐园";

    private void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
    }

    private Task CheckOrWaitDataReady()
    {
        if (cancellationTokenSource != null)
            return dataReadyTask;

        var dataReadyTaskSource = new TaskCompletionSource();
        cancellationTokenSource = new CancellationTokenSource();
        dataReadyTask = dataReadyTaskSource.Task;
        new Task(() => OnWebsocketThread(dataReadyTaskSource, cancellationTokenSource.Token),
            cancellationTokenSource.Token, TaskCreationOptions.LongRunning).Start();
        return dataReadyTask;
    }

    private async void OnWebsocketThread(TaskCompletionSource taskCompletionSource,
        CancellationToken cancellationToken)
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        logger.LogInformationEx("thread start.");

        var timeInterval = TimeSpan.FromSeconds(10);

        while (!cancellationToken.IsCancellationRequested)
            try
            {
                //get list
                var resp = await httpFactory.SendAsync(
                    "https://zombieden.cn/assets/js/ser.js", default, cancellationToken);
                var listStr = await resp.Content.ReadAsStringAsync(cancellationToken);
                var addrList = new List<(string host, int port)>();
                foreach (Match match in serverListRegex.Matches(listStr))
                    addrList.Add((match.Groups[1].Value, int.Parse(match.Groups[2].Value)));

                var queryServerTasks = addrList.Select(async x =>
                {
                    var (host, port) = x;
                    var queryUrl = $"https://api.zombieden.cn/getserverinfo.php?address={host}:{port}";
                    var str = await httpFactory.GetString(queryUrl, default, cancellationToken).AsTask();
                    return str.StartsWith("{") ? JsonSerializer.Deserialize<ZEDServer>(str) : null;
                });

                var queryServers = await Task.WhenAll(queryServerTasks);

                await UpdateServerStatus(queryServers.OfType<ZEDServer>().ToList());

                if (!taskCompletionSource.Task.IsCompleted)
                    taskCompletionSource.SetResult();

                await Task.Delay(timeInterval, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "update data failed.");
            }

        logger.LogInformationEx("thread end.");
    }

    private Task UpdateServerStatus(List<ZEDServer> serverList)
    {
        foreach (var server in serverList)
            mapManager.CacheMapTranslationName(ServerGroup, server.Map, server.MapChi);
        currentServerStatusList = serverList;
        currentServerStatusMap = serverList.ToDictionary(x => x.Ip, x => x);
        logger.LogDebugEx("server list updated.");
        return Task.CompletedTask;
    }
}