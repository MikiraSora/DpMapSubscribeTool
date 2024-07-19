using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Notifications;
using DpMapSubscribeTool.Services.Settings;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl;

[RegisterInjectable(typeof(IServerManager), ServiceLifetime.Singleton)]
public partial class DefaultServerManager : ObservableObject, IServerManager
{
    private readonly IApplicationNotification applicationNotification;
    private readonly ILogger<DefaultServerManager> logger;

    private readonly Dictionary<string, IServerActionExecutor> serverExecutors;
    private readonly Dictionary<string, IServerInfoSearcher> serverSearchers;
    private readonly Dictionary<string, IServerStateUpdater> serverUpdaters;
    private readonly ISettingManager settingManager;

    private ApplicationSettings applicationSetting;

    private TimeSpan autoPingTimeInterval;
    private TimeSpan autoRefreshTimeInterval;

    [ObservableProperty]
    private bool isDataReady;

    [ObservableProperty]
    private ObservableCollection<Server> servers = new();

    [ObservableProperty]
    private ObservableCollection<Server> subscribeServers = new();

    public DefaultServerManager(
        ILogger<DefaultServerManager> logger,
        IEnumerable<IServerInfoSearcher> serverSearchers,
        IEnumerable<IServerStateUpdater> serverUpdaters,
        ISettingManager settingManager,
        IApplicationNotification applicationNotification,
        IEnumerable<IServerActionExecutor> serverExecutors)
    {
        this.logger = logger;
        this.settingManager = settingManager;
        this.applicationNotification = applicationNotification;
        this.serverUpdaters = serverUpdaters.ToDictionary(x => x.ServerGroup, x => x);
        this.serverExecutors = serverExecutors.ToDictionary(x => x.ServerGroup, x => x);
        this.serverSearchers = serverSearchers.ToDictionary(x => x.ServerGroup, x => x);

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

    private void ProcessMapChangedServers(List<Server> mapChangedList)
    {
        var rules = applicationSetting.UserMapSubscribes;
        var filterServers = mapChangedList.AsParallel()
            .Select(x => (x, rules.FirstOrDefault(r => r.CheckRule(x))))
            .Where(x => x.Item2 != null)
            .ToArray();

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
        applicationSetting = await settingManager.GetSetting<ApplicationSettings>();
        autoPingTimeInterval = TimeSpan.FromSeconds(applicationSetting.AutoPingTimeInterval);
        autoRefreshTimeInterval = TimeSpan.FromSeconds(applicationSetting.AutoRefreshTimeInterval);

        UpdateSubscribeServers();
        Task.Run(OnAutoPingAllServersTask).NoWait();
        Task.Run(OnAutoUpdateCurrentExistServersTask).NoWait();
    }

    private void UpdateSubscribeServers()
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

            logger.LogInformation("updated all servers.");
            await Task.Delay(autoPingTimeInterval);
        }
    }

    private async Task UpdateCurrentExistServers()
    {
        var servers = Servers.ToArray();
        var oldMaps = servers.ToDictionary(x => x.Info.EndPointDescription, x => x.Map);

        var updateTasks = servers.Select(x =>
        {
            var updater = PickServiceService(serverUpdaters, x);
            return updater?.UpdateServer(x);
        }).ToArray();

        await Task.WhenAll(updateTasks);

        var mapChangedServers = servers.Where(x =>
            oldMaps.TryGetValue(x.Info.EndPointDescription, out var oldMapName) && oldMapName != x.Map).ToList();
        
        ProcessMapChangedServers(mapChangedServers);
    }

    private async void OnAutoPingAllServersTask()
    {
        logger.LogInformation("OnAutoPingTask() begin");

        while (true)
        {
            await PingAllServers();
            logger.LogInformation("ping all servers.");
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