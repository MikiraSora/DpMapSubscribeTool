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
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamQuery;
using SteamQuery.Models;

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

    private ServerInfomationDetail currentServerInfoDetail;

    [ObservableProperty]
    private ServerListFilterOptions currentServerListFilterOptions;

    [ObservableProperty]
    private SqueezeJoinTaskStatus currentSqueezeJoinTaskStatus;

    [ObservableProperty]
    private ObservableCollection<Server> filterServers = new();

    private CancellationTokenSource infoQueryCancellationTokenSource;

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
            logger.LogErrorEx("No updater to process :{server}");
            return;
        }

        await updater.UpdateServer(server);
    }

    public async Task PingServer(Server server)
    {
        try
        {
            var ping = new Ping();
            var reply = await ping.SendPingAsync(server.Info.Host, 2000);

            if (reply.Status == IPStatus.Success)
                server.Delay = (int) reply.RoundtripTime;
            else
                server.Delay = -1;
        }
        catch (Exception e)
        {
            server.Delay = -1;
        }
    }

    public async Task JoinServer(ServerInfo serverInfo)
    {
        if (PickServiceService(serverExecutors, serverInfo) is not IServerActionExecutor executor)
        {
            logger.LogErrorEx("No executor to process :{serverInfo}");
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
            RefreshFilterServers();
        });

        logger.LogInformationEx($"RefreshServers() {Servers.Count} servers updated.");

        ProcessMapChangedServers(mapChangedList);
    }

    public async Task SqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option)
    {
        //stop current running
        if (squeezeCancellationTokenSource != null)
        {
            if (!await dialogManager.ShowComfirmDialog("已存在挤服任务，是否取消当前挤服任务并重新执行新的？", "是", "否"))
                return;

            await StopSqueezeJoinServerTask();
        }

        //run new
        squeezeCancellationTokenSource = new CancellationTokenSource();
        await Task.Run(() => OnSqueezeJoinServerTask(serverInfo, option, squeezeCancellationTokenSource.Token));
    }

    public Task StopSqueezeJoinServerTask()
    {
        if (squeezeCancellationTokenSource != null)
        {
            logger.LogInformationEx("cancel current squeeze-join task.");
            squeezeCancellationTokenSource.Cancel();
            ClearSqueezeJoinServer();
        }

        return Task.CompletedTask;
    }

    public Task RefreshFilterServers()
    {
        FilterServers.Clear();

        if (CurrentServerListFilterOptions.IsEnable)
        {
            var servers = Servers.AsEnumerable();
            if (CurrentServerListFilterOptions.IsEnableDelayFilter)
                servers = servers.Where(x => x.Delay <= CurrentServerListFilterOptions.FilterDelay);
            if (CurrentServerListFilterOptions.IsEnablePlayerCountRemainFilter)
                servers = servers.Where(x =>
                    x.MaxPlayerCount - x.CurrentPlayerCount >=
                    CurrentServerListFilterOptions.FilterPlayerCountRemain);
            if (CurrentServerListFilterOptions.IsEnableKeywordFilter)
                servers = servers.Where(x =>
                {
                    var keywordContent = string.Join(" ", x.Map);
                    return keywordContent.Contains(CurrentServerListFilterOptions.FilterKeyword,
                        StringComparison.InvariantCultureIgnoreCase);
                });

            if (CurrentServerListFilterOptions.IsEnableServerGroupFilter)
            {
                var enableSet = CurrentServerListFilterOptions.ServerGroupFilters
                    .Where(x => x.IsEnable)
                    .Select(x => x.ServiceGroup)
                    .ToHashSet();
                servers = servers.Where(x => enableSet.Contains(x.Info.ServerGroup));
            }

            var filterResult = servers.ToArray();
            foreach (var server in filterResult)
                FilterServers.Add(server);
        }

        logger.LogInformationEx("filter server list has been refresh.");
        return Task.CompletedTask;
    }

    public Task ResetServerListFilterOptions()
    {
        var newOption = new ServerListFilterOptions();

        foreach (var (serverGroup, serverGroupDescription) in serverSearchers.Values.Select(x =>
                     (x.ServerGroup, x.ServerGroupDescription)))
            newOption.ServerGroupFilters.Add(new ServerGroupFilter
            {
                ServiceGroupDescription = serverGroupDescription,
                ServiceGroup = serverGroup,
                IsEnable = true
            });

        CurrentServerListFilterOptions = newOption;
        logger.LogInformationEx("current server list filter option is reset.");
        return Task.CompletedTask;
    }

    public async Task<ServerInfomationDetail> BeginServerInfomationDetailQuery(ServerInfo serverInfo)
    {
        if (currentServerInfoDetail != null)
        {
            logger.LogWarningEx("there is a detail object is running, stop now.");
            await StopServerInfomationDetailQuery();
        }

        var cr = new ServerInfomationDetail();
        if (Servers.FirstOrDefault(x => x.Info.EndPointDescription == serverInfo.EndPointDescription) is not Server
            server)
        {
            logger.LogErrorEx($"Can't find related Server object for info: {serverInfo.Name}");
            await dialogManager.ShowMessageDialog("", DialogMessageType.Error);
            return null;
        }

        infoQueryCancellationTokenSource = new CancellationTokenSource();
        cr.Server = server;
        Task.Run(() => OnAutoUpdateServerInfomationTask(cr, infoQueryCancellationTokenSource.Token),
            infoQueryCancellationTokenSource.Token).NoWait();
        return currentServerInfoDetail = cr;
    }

    public Task StopServerInfomationDetailQuery()
    {
        infoQueryCancellationTokenSource?.Cancel();
        infoQueryCancellationTokenSource?.Dispose();
        infoQueryCancellationTokenSource = default;

        if (currentServerInfoDetail is not null)
        {
            logger.LogInformationEx(
                $"server {currentServerInfoDetail.Server.Info.Name} infomation query task had been stopped.");
            currentServerInfoDetail = default;
        }

        return Task.CompletedTask;
    }

    private async void OnAutoUpdateServerInfomationTask(ServerInfomationDetail detail, CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(60);

        using var gameServer = new GameServer(detail.Server.Info.Host, detail.Server.Info.Port);

        while (!ct.IsCancellationRequested)
            try
            {
                var info = await gameServer.GetInformationAsync(ct);

                if (detail.Server.CurrentPlayerCount != info.OnlinePlayers)
                    detail.Server.CurrentPlayerCount = info.OnlinePlayers;
                if (detail.Server.MaxPlayerCount != info.MaxPlayers)
                    detail.Server.MaxPlayerCount = info.MaxPlayers;
                if (detail.Server.Map != info.Map)
                    detail.Server.Map = info.Map;

                var players = await gameServer.GetPlayersAsync(ct);
                var playerMap = new Dictionary<string, SteamQueryPlayer>();
                foreach (var player in players)
                {
                    var key = player.Name;
                    if (!playerMap.TryAdd(key,player))
                    {
                        //name conflict?
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var playerDetail in detail.PlayerDetails.ToArray())
                        if (playerMap.TryGetValue(playerDetail.Name, out var player))
                        {
                            playerMap.Remove(playerDetail.Name);

                            playerDetail.Duration = player.DurationTimeSpan;
                            playerDetail.Score = player.Score;
                        }
                        else
                        {
                            //not found , means that player is leave
                            detail.PlayerDetails.Remove(playerDetail);
                        }

                    foreach (var player in playerMap.Values)
                        detail.PlayerDetails.Add(new ServerInfomationPlayerDetail
                        {
                            Duration = player.DurationTimeSpan,
                            Name = player.Name,
                            Score = player.Score
                        });

                    logger.LogDebugEx("");
                });

                await Task.Delay(interval, ct);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, $"Can't fetch server detail:{e.Message}, but still continue");
                // ignored
            }
    }

    private void ClearSqueezeJoinServer()
    {
        squeezeCancellationTokenSource = default;
        CurrentSqueezeJoinTaskStatus = default;
    }

    private async Task OnSqueezeJoinServerTask(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken)
    {
        logger.LogInformationEx("started.");
        logger.LogInformationEx($"option.SqueezeTargetPlayerCountDiff={option.SqueezeTargetPlayerCountDiff}");
        logger.LogInformationEx($"option.MakeGameForegroundIfSuccess={option.MakeGameForegroundIfSuccess}");
        logger.LogInformationEx($"option.TryJoinInterval={option.TryJoinInterval}");
        logger.LogInformationEx($"option.NotifyIfSuccess={option.NotifyIfSuccess}");
        logger.LogInformationEx($"serverInfo.Name={serverInfo.Name}");
        logger.LogInformationEx($"serverInfo.EndPointDescription={serverInfo.EndPointDescription}");
        logger.LogInformationEx($"serverInfo.ServerGroup={serverInfo.ServerGroup}");

        var runner = PickServiceService(squeezeJoinRunners, serverInfo);
        if (runner == null)
        {
            logger.LogErrorEx("No squeeze-join runner.");
            await dialogManager.ShowMessageDialog("无法执行挤服功能，应用并未支持此社区服务器", DialogMessageType.Error);
            goto End;
        }

        if (!await runner.CheckSqueezeJoinServer(serverInfo, option, cancellationToken))
        {
            logger.LogErrorEx("CheckSqueezeJoinServer() return false.");
            goto End;
        }

        var timeInterval = TimeSpan.FromSeconds(option.TryJoinInterval);

        var ipList = await Dns.GetHostAddressesAsync(serverInfo.Host, cancellationToken);
        if (ipList.Length == 0)
        {
            logger.LogErrorEx($"Can't lookup ip address for host name:{serverInfo.Host}");
            await dialogManager.ShowMessageDialog($"无法执行挤服功能，无法获取服务器ip地址({serverInfo.Host})", DialogMessageType.Error);
            goto End;
        }

        var ipAddr = ipList.First();

        //build status and set
        var server = Servers.FirstOrDefault(x => x.Info.EndPointDescription == serverInfo.EndPointDescription);
        if (server == null)
        {
            logger.LogErrorEx($"Can't lookup related Server object for endpoint:{serverInfo.EndPointDescription}");
            await dialogManager.ShowMessageDialog("无法执行挤服功能，程序内部问题(程序找不到serverInfo对应的Server对象).",
                DialogMessageType.Error);
            goto End;
        }

        var status = new SqueezeJoinTaskStatus(server, option);
        CurrentSqueezeJoinTaskStatus = status;

        var gameServer = new GameServer($"{ipAddr}:{serverInfo.Port}");
        var diff = option.SqueezeTargetPlayerCountDiff;
        while (!cancellationToken.IsCancellationRequested)
        {
            //get current player count
            try
            {
                var info = await gameServer.GetInformationAsync(cancellationToken);
                var currentPlayerCount = info.OnlinePlayers;

                CurrentSqueezeJoinTaskStatus.Server.CurrentPlayerCount = currentPlayerCount;

                logger.LogInformationEx(
                    $"currentPlayerCount({currentPlayerCount}) <= ({info.MaxPlayers - diff}) maxPlayerCount({info.MaxPlayers}) - diff({diff}).");
                if (currentPlayerCount <= info.MaxPlayers - diff)
                {
                    //join
                    await JoinServer(serverInfo);

                    //currently there is no way to check if user squeeze-join server successfully.
                    //because of map download request and loading.
                    //so we just notify user we had executed join-server cmd at all. ^ ^
                    /*
                    //wait
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                        continue;
                    //check
                    var players = (await gameServer.GetPlayersAsync(cancellationToken)).Select(x => x.Name).ToList();
                    if (await runner.IsUserInServer(players, cancellationToken))
                    {
                        logger.LogInformationEx("squeeze-join server successfully.");
                        break;
                    }

                    logger.LogInformationEx("squeeze-join server failed, try again.");
                    */

                    applicationNotification.NofitySqueezeJoinSuccess(serverInfo).NoWait();
                    break;
                }
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, $"squeeze-join thow exception:{e.Message}");
            }


            await Task.Delay(timeInterval, CancellationToken.None);
        }

        gameServer?.Dispose();

        End:
        ClearSqueezeJoinServer();
        logger.LogInformationEx("OnSqueezeJoinServerTask() finished.");
    }

    private void ProcessMapChangedServers(List<Server> mapChangedList)
    {
        var rules = applicationSetting.UserMapSubscribes;
        var filterServers = mapChangedList.AsParallel()
            .Select(x => (x, rules.FirstOrDefault(r => r.CheckRule(x))))
            .Where(x => x.Item2 != null)
            .ToArray();

        if (mapChangedList.Count > 0)
            logger.LogInformationEx(
                $"ProcessMapChangedServers() there are {filterServers.Length} servers match subscribe rules in same time: {string.Join(",", filterServers.Select(x => x.x.Map))}");

        foreach (var (server, rule) in filterServers)
            applicationNotification.NofityServerForSubscribeMap(server, rule, reaction =>
            {
                if (reaction == UserReaction.Comfirm)
                    JoinServer(server.Info).NoWait();
            });
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
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        applicationSetting = await persistence.Load<ApplicationSettings>();

        ResetServerListFilterOptions().NoWait();

        //UpdateSubscribeServers();
        Task.Run(OnAutoPingAllServersTask).NoWait();
        Task.Run(OnAutoUpdateCurrentExistServersTask).NoWait();
    }

    private void UpdateSubscribeServers()
    {
        if (applicationSetting is null)
        {
            logger.LogWarningEx("applicationSetting is null");
            return;
        }

        var rules = applicationSetting.UserMapSubscribes;
        var filterServers = Servers.AsParallel().Where(x => rules.Any(r => r.CheckRule(x))).ToArray();

        SubscribeServers.Clear();
        foreach (var server in filterServers)
            SubscribeServers.Add(server);

        logger.LogInformationEx("SubscribedServers updated");
    }

    private async void OnAutoUpdateCurrentExistServersTask()
    {
        var autoUpdateTimeInterval = TimeSpan.FromSeconds(applicationSetting.AutoPingTimeInterval);

        logger.LogInformationEx("OnUpdateServerStateTask() begin");

        while (true)
        {
            await UpdateCurrentExistServers();
            //await RefreshServers();

            logger.LogDebugEx("updated all servers.");
            await Task.Delay(autoUpdateTimeInterval);
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
            /*
            if (before != x.CurrentPlayerCount)
                logger.LogInformationEx(
                    $"server {x.Info.Name} playercount {before} -> {x.CurrentPlayerCount}");
                    */
        }).ToArray();

        await Task.WhenAll(updateTasks);

        var mapChangedServers = servers.Where(x =>
            oldMaps.TryGetValue(x.Info.EndPointDescription, out var oldMapName) && oldMapName != x.Map).ToList();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateSubscribeServers();
            RefreshFilterServers();
        });
        ProcessMapChangedServers(mapChangedServers);
    }

    private async void OnAutoPingAllServersTask()
    {
        var autoPingTimeInterval = TimeSpan.FromSeconds(applicationSetting.AutoRefreshTimeInterval);
        logger.LogInformationEx("OnAutoPingTask() begin");

        while (true)
        {
            await PingAllServers();
            logger.LogDebugEx("ping all servers.");
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