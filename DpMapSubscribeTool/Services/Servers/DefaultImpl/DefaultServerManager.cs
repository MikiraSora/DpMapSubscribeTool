using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Notifications;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamQuery;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl;

[RegisterInjectable(typeof(IServerManager), ServiceLifetime.Singleton)]
public partial class DefaultServerManager : ObservableObject, IServerManager
{
    private readonly IApplicationNotification applicationNotification;
    private readonly IDialogManager dialogManager;
    private readonly ILogger<DefaultServerManager> logger;

    private readonly IPersistence persistence;

    private readonly Dictionary<string, IServerActionExecutor> serverExecutors;
    private readonly Dictionary<string, IServerInfoSearcher> serverSearchers;
    private readonly Dictionary<string, IServerStateUpdater> serverUpdaters;
    private readonly Dictionary<string, IServerSqueezeJoinRunner> squeezeJoinRunners;

    private ApplicationSettings applicationSetting;

    private TimeSpan autoPingTimeInterval;
    private TimeSpan autoRefreshTimeInterval;

    [ObservableProperty]
    private bool isDataReady;

    [ObservableProperty]
    private ObservableCollection<Server> servers = new();

    private CancellationTokenSource squeezeCancellationTokenSource;

    [ObservableProperty]
    private ObservableCollection<Server> subscribeServers = new();

    public DefaultServerManager(
        ILogger<DefaultServerManager> logger,
        IEnumerable<IServerInfoSearcher> serverSearchers,
        IEnumerable<IServerStateUpdater> serverUpdaters,
        IEnumerable<IServerSqueezeJoinRunner> squeezeJoinRunners,
        IPersistence persistence,
        IDialogManager dialogManager,
        IApplicationNotification applicationNotification,
        IEnumerable<IServerActionExecutor> serverExecutors)
    {
        this.logger = logger;
        this.persistence = persistence;
        this.dialogManager = dialogManager;
        this.applicationNotification = applicationNotification;
        this.serverUpdaters = serverUpdaters.ToDictionary(x => x.ServerGroup, x => x);
        this.serverExecutors = serverExecutors.ToDictionary(x => x.ServerGroup, x => x);
        this.serverSearchers = serverSearchers.ToDictionary(x => x.ServerGroup, x => x);
        this.squeezeJoinRunners = squeezeJoinRunners.ToDictionary(x => x.ServerGroup, x => x);

        Initialize();
    }

    public async Task PrepareData()
    {
        await RefreshServers();
        await PingAllServers();
        IsDataReady = true;
    }

    public async Task UpdateServer(Server server)
    {
        if (PickServiceService(serverUpdaters, server) is not IServerStateUpdater updater)
        {
            logger.LogError("No updater to process :{server}");
            return;
        }

        await updater.UpdateServer(server);
    }

    public async Task PingServer(Server server)
    {
        var ping = new Ping();
        var reply = await ping.SendPingAsync(server.Info.Host, 2000);

        if (reply.Status == IPStatus.Success)
            server.Delay = (int) reply.RoundtripTime;
        else
            server.Delay = -1;
    }

    public async Task JoinServer(ServerInfo serverInfo)
    {
        if (PickServiceService(serverExecutors, serverInfo) is not IServerActionExecutor executor)
        {
            logger.LogError("No executor to process :{serverInfo}");
            return;
        }

        await executor.Join(serverInfo);
    }

    public async Task RefreshServers()
    {
        var oldServerMap = Servers.ToDictionary(x => x.Info.EndPointDescription, x => x);
        var serverList = await FetchAllServers();
        var mapChangedList = new List<Server>();
        var insertServerList = new List<Server>();

        //update old or insert new
        foreach (var newServer in serverList)
            if (oldServerMap.TryGetValue(newServer.Info.EndPointDescription, out var oldServer))
            {
                //update to oldServer
                oldServer.MaxPlayerCount = newServer.MaxPlayerCount;
                oldServer.State = newServer.State;
                oldServer.CurrentPlayerCount = newServer.CurrentPlayerCount;
                var oldMap = oldServer.Map;
                oldServer.Map = newServer.Map;
                oldServer.Info = newServer.Info;

                //wait to check subscribe rules and notify user.
                if (oldServer.Map != newServer.Map)
                    mapChangedList.Add(oldServer);
            }
            else
            {
                //mark and insert later
                insertServerList.Add(newServer);
            }

        //FUCK DataGrid
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            //insert new to Servers.
            foreach (var newServer in insertServerList)
            {
                Servers.Add(newServer);
                PingServer(newServer).NoWait();
            }

            UpdateSubscribeServers();
        });

        logger.LogInformation($"RefreshServers() {Servers.Count} servers updated.");

        ProcessMapChangedServers(mapChangedList);
    }

    public async Task SqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option)
    {
        //stop current running
        if (squeezeCancellationTokenSource != null)
        {
            logger.LogInformation("cancel current squeeze-join task.");
            squeezeCancellationTokenSource.Cancel();
            squeezeCancellationTokenSource = default;
        }

        //run new
        squeezeCancellationTokenSource = new CancellationTokenSource();
        await Task.Run(() => OnSqueezeJoinServerTask(serverInfo, option, squeezeCancellationTokenSource.Token));
    }

    private async Task OnSqueezeJoinServerTask(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("OnSqueezeJoinServerTask() started.");
        logger.LogInformation($"option.SqueezeTargetPlayerCountDiff={option.SqueezeTargetPlayerCountDiff}");
        logger.LogInformation($"option.MakeGameForegroundIfSuccess={option.MakeGameForegroundIfSuccess}");
        logger.LogInformation($"option.TryJoinInterval={option.TryJoinInterval}");
        logger.LogInformation($"option.NotifyIfSuccess={option.NotifyIfSuccess}");
        logger.LogInformation($"serverInfo.Name={serverInfo.Name}");
        logger.LogInformation($"serverInfo.EndPointDescription={serverInfo.EndPointDescription}");
        logger.LogInformation($"serverInfo.ServerGroup={serverInfo.ServerGroup}");

        var runner = PickServiceService(squeezeJoinRunners, serverInfo);
        if (runner == null)
        {
            logger.LogError("No squeeze-join runner.");
            await dialogManager.ShowMessageDialog("无法执行挤服功能，应用并未支持此社区服务器", DialogMessageType.Error);
            goto End;
        }

        if (!await runner.CheckSqueezeJoinServer(serverInfo, option, cancellationToken))
        {
            logger.LogError("CheckSqueezeJoinServer() return false.");
            goto End;
        }

        var timeInterval = TimeSpan.FromSeconds(option.TryJoinInterval);

        var ipList = await Dns.GetHostAddressesAsync(serverInfo.Host, cancellationToken);
        if (ipList.Length == 0)
        {
            logger.LogError($"Can't lookup ip address for host name:{serverInfo.Host}");
            await dialogManager.ShowMessageDialog($"无法执行挤服功能，无法获取服务器ip地址({serverInfo.Host})", DialogMessageType.Error);
            goto End;
        }

        var ipAddr = ipList.First();
        var gameServer = new GameServer($"{ipAddr}:{serverInfo.Port}");

        var diff = option.SqueezeTargetPlayerCountDiff;

        while (!cancellationToken.IsCancellationRequested)
        {
            //get current player count
            var info = await gameServer.GetInformationAsync(cancellationToken);
            var currentPlayerCount = info.OnlinePlayers;

            logger.LogInformation(
                $"currentPlayerCount({currentPlayerCount}) <= ({info.MaxPlayers - diff}) maxPlayerCount({info.MaxPlayers}) - diff({diff}).");
            if (currentPlayerCount <= info.MaxPlayers - diff)
            {
                //join
                await JoinServer(serverInfo);
                //wait
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    continue;
                //check
                var players = (await gameServer.GetPlayersAsync(cancellationToken)).Select(x => x.Name).ToList();
                if (await runner.IsUserInServer(players, cancellationToken))
                {
                    logger.LogInformation("squeeze-join server successfully.");
                    break;
                }

                logger.LogInformation("squeeze-join server failed, try again.");
            }

            await Task.Delay(timeInterval, cancellationToken);
        }

        End:
        squeezeCancellationTokenSource = default;
        logger.LogInformation("OnSqueezeJoinServerTask() finished.");
    }

    private async Task<bool> IsUserInServer(GameServer gameServer, CancellationToken cancellationToken)
    {
        return false;
    }

    private void ProcessMapChangedServers(List<Server> mapChangedList)
    {
        var rules = applicationSetting.UserMapSubscribes;
        var filterServers = mapChangedList.AsParallel()
            .Select(x => (x, rules.FirstOrDefault(r => r.CheckRule(x))))
            .Where(x => x.Item2 != null)
            .ToArray();

        if (mapChangedList.Count > 0)
            logger.LogInformation(
                $"ProcessMapChangedServers() there are {filterServers.Length} servers match subscribe rules in same time: {string.Join(",", filterServers.Select(x => x.x.Map))}");

        foreach (var (server, rule) in filterServers)
            applicationNotification.NofityServerForSubscribeMap(server, rule, () => JoinServer(server.Info).NoWait());
    }

    private async Task<Server[]> FetchAllServers()
    {
        var serverInfoTasks = serverSearchers.Values.Select(x => x.GetAllAvaliableServerInfo()).ToArray();
        var serverInfoList = (await Task.WhenAll(serverInfoTasks)).SelectMany(x => x).ToArray();

        var serverTasks = serverInfoList.Select(x =>
        {
            var updater = PickServiceService(serverUpdaters, x);
            return updater.QueryServer(x);
        });
        var serverList = await Task.WhenAll(serverTasks);
        return serverList;
    }

    private async void Initialize()
    {
        applicationSetting = await persistence.Load<ApplicationSettings>();
        autoPingTimeInterval = TimeSpan.FromSeconds(applicationSetting.AutoPingTimeInterval);
        autoRefreshTimeInterval = TimeSpan.FromSeconds(applicationSetting.AutoRefreshTimeInterval);

        //UpdateSubscribeServers();
        Task.Run(OnAutoPingAllServersTask).NoWait();
        Task.Run(OnAutoUpdateCurrentExistServersTask).NoWait();
    }

    private async void UpdateSubscribeServers()
    {
        if (applicationSetting is null)
        {
            logger.LogWarning("applicationSetting is null");
            return;
        }

        var rules = applicationSetting.UserMapSubscribes;
        var filterServers = Servers.AsParallel().Where(x => rules.Any(r => r.CheckRule(x))).ToArray();

        SubscribeServers.Clear();
        foreach (var server in filterServers)
            SubscribeServers.Add(server);

        logger.LogInformation("SubscribedServers updated");
    }

    private async void OnAutoUpdateCurrentExistServersTask()
    {
        logger.LogInformation("OnUpdateServerStateTask() begin");

        while (true)
        {
            await UpdateCurrentExistServers();
            //await RefreshServers();

            logger.LogDebug("updated all servers.");
            await Task.Delay(autoPingTimeInterval);
        }
    }

    private async Task UpdateCurrentExistServers()
    {
        var servers = Servers.ToArray();
        var oldMaps = servers.ToDictionary(x => x.Info.EndPointDescription, x => x.Map);

        var updateTasks = servers.Select(async x =>
        {
            var updater = PickServiceService(serverUpdaters, x);
            var before = x.CurrentPlayerCount;
            await updater?.UpdateServer(x);
            if (before != x.CurrentPlayerCount)
                logger.LogInformation(
                    $"server {x.Info.Name} playercount {before} -> {x.CurrentPlayerCount}");
        }).ToArray();

        await Task.WhenAll(updateTasks);

        var mapChangedServers = servers.Where(x =>
            oldMaps.TryGetValue(x.Info.EndPointDescription, out var oldMapName) && oldMapName != x.Map).ToList();

        await Dispatcher.UIThread.InvokeAsync(UpdateSubscribeServers);
        ProcessMapChangedServers(mapChangedServers);
    }

    private async void OnAutoPingAllServersTask()
    {
        logger.LogInformation("OnAutoPingTask() begin");

        while (true)
        {
            await PingAllServers();
            logger.LogDebug("ping all servers.");
            await Task.Delay(autoPingTimeInterval);
        }
    }

    private Task PingAllServers()
    {
        return Task.WhenAll(Servers.Select(PingServer).ToArray());
    }

    private T PickServiceService<T>(IDictionary<string, T> map, Server server) where T : IServerServiceBase
    {
        return PickServiceService(map, server.Info);
    }

    private T PickServiceService<T>(IDictionary<string, T> map, ServerInfo info) where T : IServerServiceBase
    {
        return map.TryGetValue(info.ServerGroup, out var service) ? service : default;
    }
}